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

        public async Task Init(string apiKey, string gameId, HelikaEnvironment env, bool enabled = false)
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

            _isInitialized = true;

            // If Localhost is set, force disable sending events
            _enabled = env != HelikaEnvironment.Localhost ? enabled : false;

            if (KochavaTracker.Instance != null)
            {
                KochavaTracker.Instance.RegisterEditorAppGuid(_kochavaApiKey);
#if UNITY_ANDROID
                KochavaTracker.Instance.RegisterAndroidAppGuid(_kochavaApiKey);
#endif

#if UNITY_IOS
                KochavaTracker.Instance.RegisterIosAppGuid(_kochavaApiKey);
                KochavaTracker.Instance.SetIosAttAuthorizationAutoRequest(true);
                KochavaTracker.Instance.SetIosAttAuthorizationWaitTime(30);
#endif

                KochavaTracker.Instance.Start();

                // Send an event to store the Kochava device id
                KochavaTracker.Instance.GetDeviceId((deviceId) =>
                {
                    this._deviceId = deviceId;

#pragma warning disable CS4014
                    // Fire and forget generate a 'Create Session'
                    CreateSession();
#pragma warning restore CS4014
                });
            }
            else
            {
                // In case the KochavaTracker fails to initialized
                await CreateSession();
            }
        }

        public async Task<string> SendEvent(string eventName, Dictionary<string, object> eventProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            Dictionary<string, object> helikaEvent = new Dictionary<string, object>();

            // Add game_id only if the event doesn't already have it
            helikaEvent["game_id"] = _gameId;

            // Convert to ISO 8601 format string using "o" specifier
            helikaEvent["created_at"] = DateTime.UtcNow.ToString("o");

            // Set event_type if it doesn't exist
            helikaEvent["event_type"] = eventName;

            eventProps["sessionID"] = _sessionID;
            if (!string.IsNullOrWhiteSpace(_playerID))
            {
                eventProps["player_id"] = _playerID;
            }

            helikaEvent["event"] = eventProps;


            Dictionary<string, object> finalEvent = new Dictionary<string, object>();
            finalEvent["id"] = _sessionID;
            finalEvent["events"] = new Dictionary<string, object>[] { helikaEvent };

            JObject serializedEvt = JObject.FromObject(finalEvent);
            return await PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public async Task<string> SendEvents(string eventName, Dictionary<string, object>[] eventsProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }
            // Add helika-specific data to the events
            List<Dictionary<string, object>> events = new List<Dictionary<string, object>> { };
            foreach (Dictionary<string, object> eventProps in eventsProps)
            {
                Dictionary<string, object> helikaEvent = new Dictionary<string, object>()
                {
                    // Add game_id only if the event doesn't already have it
                    {"game_id", _gameId},
                    // Convert to ISO 8601 format string using "o" specifier
                    {"created_at", DateTime.UtcNow.ToString("o")},
                    // Set event_type
                    {"event_type", eventName},
                };

                eventProps["sessionID"] = _sessionID;
                if (!string.IsNullOrWhiteSpace(_playerID))
                {
                    eventProps["player_id"] = _playerID;
                }

                helikaEvent["event"] = eventProps;

                events.Add(helikaEvent);
            }


            Dictionary<string, object> finalEvent = new Dictionary<string, object>();
            finalEvent["id"] = _sessionID;
            finalEvent["events"] = events.ToArray();

            JObject serializedEvt = JObject.FromObject(finalEvent);
            return await PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public async Task<string> SendEvent(JObject eventProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            // Add game_id only if the event doesn't already have it
            AddIfNull(eventProps, "game_id", _gameId);

            // Convert to ISO 8601 format string using "o" specifier
            AddOrReplace(eventProps, "created_at", DateTime.UtcNow.ToString("o"));

            if (!eventProps.ContainsKey("event_type") || string.IsNullOrWhiteSpace(eventProps.GetValue("event_type").ToString()))
            {
                throw new ArgumentException("Invalid Event: Missing 'event_type' field");
            }

            if (!eventProps.ContainsKey("event"))
            {
                eventProps.Add(new JProperty("event", new JObject()));
            }

            if (eventProps.GetValue("event").GetType() != typeof(Newtonsoft.Json.Linq.JObject))
            {
                throw new ArgumentException("Invalid Event: 'event' field must be of type [Newtonsoft.Json.Linq.JObject]");
            }

            JObject internalEvent = (JObject)eventProps.GetValue("event");
            AddOrReplace(internalEvent, "sessionID", _sessionID);
            if (!string.IsNullOrWhiteSpace(_playerID))
            {
                AddOrReplace(internalEvent, "player_id", _playerID);
            }

            JObject newEvent = new JObject(
                new JProperty("id", _sessionID),
                new JProperty("events", new JArray() { eventProps })
            );
            return await PostAsync("/game/game-event", newEvent.ToString());
        }

        public async Task<string> SendEvents(JObject[] eventsProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            // Add helika-specific data to the events
            JArray jarrayObj = new JArray();
            foreach (JObject eventProp in eventsProps)
            {
                // Add game_id only if the event doesn't already have it
                AddIfNull(eventProp, "game_id", _gameId);

                // Convert to ISO 8601 format string using "o" specifier
                AddOrReplace(eventProp, "created_at", DateTime.UtcNow.ToString("o"));

                if (!eventProp.ContainsKey("event_type") || string.IsNullOrWhiteSpace(eventProp.GetValue("event_type").ToString()))
                {
                    throw new ArgumentException("Invalid Event: Missing 'event_type' field");
                }

                if (!eventProp.ContainsKey("event"))
                {
                    eventProp.Add(new JProperty("event", new JObject()));
                }

                if (eventProp.GetValue("event").GetType() != typeof(Newtonsoft.Json.Linq.JObject))
                {
                    throw new ArgumentException("Invalid Event: 'event' field must be of type [Newtonsoft.Json.Linq.JObject]");
                }

                JObject internalEvent = (JObject)eventProp.GetValue("event");
                AddOrReplace(internalEvent, "sessionID", _sessionID);

                if (!string.IsNullOrWhiteSpace(_playerID))
                {
                    AddOrReplace(internalEvent, "player_id", _playerID);
                }
                jarrayObj.Add(eventProp);
            }

            JObject newEvent = new JObject(
                new JProperty("id", _sessionID),
                new JProperty("events", jarrayObj)
            );
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
                new JProperty("game_id", _gameId),
                new JProperty("event_type", "SESSION_CREATED"),
                new JProperty("created_at", DateTime.UtcNow.ToString("o")),
                new JProperty("event", new JObject(
                    new JProperty("sessionID", _sessionID),
                    new JProperty("player_id", _playerID),
                    new JProperty("sdk_name", SdkName),
                    new JProperty("sdk_version", SdkVersion),
                    new JProperty("sdk_class", SdkClass),
                    new JProperty("kochava_device_id", _deviceId)
                ))
            );

            JObject evt = new JObject(
                new JProperty("id", _sessionID),
                new JProperty("events", new JArray() { createSessionEvent })
            );

            // Asynchronous send event
            return await PostAsync("/game/game-event", evt.ToString());
        }

        private async Task<string> PostAsync(string url, string data)
        {
            if (!_enabled)
            {
                var message = "Event sent: " + data;
                Debug.Log(message);
                return message;
            }

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
