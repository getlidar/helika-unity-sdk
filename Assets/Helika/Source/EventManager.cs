using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Kochava;

namespace Helika
{
    public class EventManager : HelikaSingletonScriptableObject<EventManager>
    {
        // Version data that is updated via a script. Do not change.
        private const string SdkName = "Unity";
        private const string SdkVersion = "0.1.3";
        private const string SdkClass = "EventManager";

        private string _helikaApiKey;
        private string _kochavaApiKey;
        protected string _baseUrl;
        protected string _gameId;
        protected string _sessionID;
        protected bool _isInitialized = false;

        protected string _playerID;

        protected string _deviceId;

        protected bool _enabled = false;

        public async void Init(string apiKey, string gameId, HelikaEnvironment env, bool enabled = false)
        {
            if (_isInitialized)
            {
                return;
            }

            string[] apiKeys = apiKey.Split('.');
            if (apiKeys.Length != 2)
            {
                throw new ArgumentException("Invalid API Key");
            }

            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentException("Missing Game ID");
            }

            _helikaApiKey = apiKeys[0];
            _kochavaApiKey = apiKeys[1];
            _gameId = gameId;
            _baseUrl = ConvertUrl(env);
            _sessionID = Guid.NewGuid().ToString();

            // If Localhost is set, force disable sending events
            _enabled = env != HelikaEnvironment.Localhost ? enabled : false;

            KochavaTracker.Instance.RegisterEditorAppGuid(_kochavaApiKey);
            KochavaTracker.Instance.RegisterAndroidAppGuid(_kochavaApiKey);
            KochavaTracker.Instance.RegisterIosAppGuid(_kochavaApiKey);
            KochavaTracker.Instance.Start();

            await CreateSession();

            _isInitialized = true;

            // Send an event to store the Kochava device id
            KochavaTracker.Instance.GetDeviceId(async (deviceId) =>
            {
                this._deviceId = deviceId;

                JObject deviceIdEvent = new JObject(
                    new JProperty("game_id", "HELIKA_SDK"),
                    new JProperty("event_type", "GET_DEVICE_ID"),
                    new JProperty("event", new JObject(
                        new JProperty("kochava_device_id", deviceId)
                    ))
                );

                // Asynchronous send event
                await SendEvent(new JObject[] { deviceIdEvent });
            });
        }

        public async Task<string> SendEvent(JObject[] helikaEvents)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            // Add helika-specific data to the events
            JArray jarrayObj = new JArray();
            foreach (JObject helikaEvent in helikaEvents)
            {
                // Add game_id only if the event doesn't already have it
                AddIfNull(helikaEvent, "game_id", _gameId);

                // Convert to ISO 8601 format string using "o" specifier
                AddOrReplace(helikaEvent, "created_at", DateTime.UtcNow.ToString("o"));

                if (!helikaEvent.ContainsKey("event_type") || string.IsNullOrWhiteSpace(helikaEvent.GetValue("event_type").ToString()))
                {
                    throw new ArgumentException("Invalid Event: Missing 'event_type' field");
                }

                if (!helikaEvent.ContainsKey("event"))
                {
                    helikaEvent.Add(new JProperty("event", new JObject()));
                }

                if (helikaEvent.GetValue("event").GetType() != typeof(Newtonsoft.Json.Linq.JObject))
                {
                    throw new ArgumentException("Invalid Event: 'event' field must be of type [Newtonsoft.Json.Linq.JObject]");
                }

                JObject internalEvent = (JObject)helikaEvent.GetValue("event");
                AddOrReplace(internalEvent, "sessionID", _sessionID);

                if (!string.IsNullOrWhiteSpace(_playerID))
                {
                    AddOrReplace(internalEvent, "player_id", _playerID);
                }
                jarrayObj.Add(helikaEvent);
            }

            JObject newEvent = new JObject(
                new JProperty("id", _sessionID),
                new JProperty("events", jarrayObj)
            );

            if (!_enabled)
            {
                var message = "Event sent: " + newEvent.ToString();
                Debug.Log(message);
                return message;
            }
            return await PostAsync("/game/game-event", newEvent.ToString());
        }

        public void SetEnableEvents(bool enabled)
        {
            _enabled = enabled;
        }

        public string GetPlayerID()
        {
            return _playerID;
        }

        public void SetPlayerID(string playerID)
        {
            _playerID = playerID;
        }

        private async Task<string> CreateSession()
        {
            JObject createSessionEvent = new JObject(
                new JProperty("game_id", "HELIKA_SDK"),
                new JProperty("event_type", "SESSION_CREATED"),
                new JProperty("event", new JObject(
                    new JProperty("sdk_name", SdkName),
                    new JProperty("sdk_version", SdkVersion),
                    new JProperty("sdk_class", SdkClass)
                ))
            );
            return await SendEvent(new JObject[] { createSessionEvent });
        }

        private async Task<string> PostAsync(string url, string data)
        {
            using (UnityWebRequest request = new UnityWebRequest(_baseUrl + url, "POST"))
            {
                // Set the request method and content type
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-api-key", _helikaApiKey);

                // Convert the data to bytes and attach it to the request
                byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                // Send the request
                await request.SendWebRequest();

                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    // Display the error
                    Debug.LogError("Error: " + request.error + ", data: " + request.downloadHandler.text);
                }
                return request.downloadHandler.text;
            }
        }

        private static void AddIfNull(JObject helikaEvent, string key, string newValue)
        {
            if (!helikaEvent.ContainsKey(key))
            {
                helikaEvent.Add(key, newValue);
            }
        }


        private static void AddOrReplace(JObject helikaEvent, string key, string newValue)
        {
            JToken gameIdObj;
            if (helikaEvent.TryGetValue(key, out gameIdObj))
            {
                gameIdObj.Replace(newValue);
            }
            else
            {
                helikaEvent.Add(key, newValue);
            }
        }

        private static string ConvertUrl(HelikaEnvironment baseUrl)
        {
            switch (baseUrl)
            {
                case HelikaEnvironment.Production:
                    return "https://api.helika.io/v1";
                case HelikaEnvironment.Develop:
                    return "https://api-stage.helika.io/v1";
                case HelikaEnvironment.Localhost:
                default:
                    return "http://localhost:8181/v1";
            }
        }
    }
}
