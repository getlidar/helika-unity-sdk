using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        private const string SdkVersion = "0.2.0";
        private const string SdkClass = "EventManager";

        private string _helikaApiKey;
        private string _kochavaApiKey;
        protected string _baseUrl;
        protected string _gameId;
        protected string _sessionID;
        protected bool _isInitialized = false;
        protected string _playerID;
        protected string _deviceId;
        protected bool _printEventsToConsole = false;
        protected TelemetryLevel _telemetry = TelemetryLevel.All;
        public bool iosAttAuthorizationAutoRequest = true;
        public double iosAttAuthorizationWaitTime = 30;

        public void Init(string apiKey, string gameId, HelikaEnvironment env, TelemetryLevel telemetryLevel = TelemetryLevel.All, bool printEventsToConsole = false)
        {
            if (_isInitialized)
            {
                return;
            }

            string[] apiKeys = apiKey.Split('.');
            if (apiKeys.Length < 1 || apiKeys.Length > 2)
            {
                throw new ArgumentException("Invalid API Key");
            }

            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentException("Missing Game ID");
            }

            _helikaApiKey = apiKeys[0];
            if (apiKeys.Length == 2)
            {
                _kochavaApiKey = apiKeys[1];
            }
            _gameId = gameId;
            _baseUrl = ConvertUrl(env);
            _sessionID = Guid.NewGuid().ToString();

            _isInitialized = true;

            // If Localhost is set, force print events
            _telemetry = env != HelikaEnvironment.Localhost ? telemetryLevel : TelemetryLevel.None;

            // If PrintEventsToConsole is set to true, we only print the event to console and we don't send it
            _printEventsToConsole = printEventsToConsole;

            if (_telemetry > TelemetryLevel.None)
            {
                // TelemetryOnly means we shouldn't initialize Kochava
                if (_telemetry > TelemetryLevel.TelemetryOnly &&
                    !string.IsNullOrEmpty(_kochavaApiKey) &&
                    KochavaTracker.Instance != null)
                {
                    InitializeKochava();
                }
                else
                {
                    // In case the Kochava key doesn't exist or KochavaTracker fails to initialized
                    CreateSession();
                }
            }
        }

        public void SendEvent(string eventName, Dictionary<string, object> eventProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            Dictionary<string, object> finalEvent = new Dictionary<string, object>();
            finalEvent["id"] = _sessionID;
            finalEvent["events"] = new Dictionary<string, object>[] { AppendAttributesToDictionary(eventName, eventProps) };

            JObject serializedEvt = JObject.FromObject(finalEvent);
            PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public void SendEvents(string eventName, Dictionary<string, object>[] eventsProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            // Add helika-specific data to the events
            List<Dictionary<string, object>> events = new List<Dictionary<string, object>> { };
            foreach (Dictionary<string, object> eventProps in eventsProps)
            {
                events.Add(AppendAttributesToDictionary(eventName, eventProps));
            }

            Dictionary<string, object> finalEvent = new Dictionary<string, object>();
            finalEvent["id"] = _sessionID;
            finalEvent["events"] = events.ToArray();

            JObject serializedEvt = JObject.FromObject(finalEvent);
            PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public void SendCustomEvent(JObject eventProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            JObject newEvent = new JObject(
                new JProperty("id", _sessionID),
                new JProperty("events", new JArray() { AppendAttributesToJObject(eventProps) })
            );
            PostAsync("/game/game-event", newEvent.ToString());
        }

        public void SendCustomEvents(JObject[] eventsProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            // Add helika-specific data to the events
            JArray jarrayObj = new JArray();
            foreach (JObject eventProp in eventsProps)
            {
                jarrayObj.Add(AppendAttributesToJObject(eventProp));
            }

            JObject newEvent = new JObject(
                new JProperty("id", _sessionID),
                new JProperty("events", jarrayObj)
            );
            PostAsync("/game/game-event", newEvent.ToString());
        }

        public void SetPrintToConsole(bool printToConsole)
        {
            _printEventsToConsole = printToConsole;
        }

        public string GetPlayerID()
        {
            return _playerID;
        }

        public void SetPlayerID(string playerID)
        {
            _playerID = playerID;
        }

        private JObject AppendAttributesToJObject(JObject obj)
        {
            // Add game_id only if the event doesn't already have it
            AddIfNull(obj, "game_id", _gameId);

            // Convert to ISO 8601 format string using "o" specifier
            AddOrReplace(obj, "created_at", DateTime.UtcNow.ToString("o"));

            if (!obj.ContainsKey("event_type") || string.IsNullOrWhiteSpace(obj.GetValue("event_type").ToString()))
            {
                throw new ArgumentException("Invalid Event: Missing 'event_type' field");
            }

            if (!obj.ContainsKey("event"))
            {
                obj.Add(new JProperty("event", new JObject()));
            }

            if (obj.GetValue("event").GetType() != typeof(Newtonsoft.Json.Linq.JObject))
            {
                throw new ArgumentException("Invalid Event: 'event' field must be of type [Newtonsoft.Json.Linq.JObject]");
            }

            JObject internalEvent = (JObject)obj.GetValue("event");
            AddOrReplace(internalEvent, "session_id", _sessionID);

            if (!string.IsNullOrWhiteSpace(_playerID))
            {
                AddOrReplace(internalEvent, "player_id", _playerID);
            }

            return obj;
        }

        private Dictionary<string, object> AppendAttributesToDictionary(string eventName, Dictionary<string, object> eventProps)
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

            eventProps["session_id"] = _sessionID;
            if (!string.IsNullOrWhiteSpace(_playerID))
            {
                eventProps["player_id"] = _playerID;
            }

            helikaEvent["event"] = eventProps;

            return helikaEvent;
        }

        private void CreateSession()
        {
            JObject internal_event = new JObject(
                new JProperty("session_id", _sessionID),
                new JProperty("player_id", _playerID),
                new JProperty("sdk_name", SdkName),
                new JProperty("sdk_version", SdkVersion),
                new JProperty("sdk_class", SdkClass),
                new JProperty("sdk_platform", Application.platform.ToString()),
                new JProperty("event_sub_type", "session_created"),
                new JProperty("telemetry_level", _telemetry.ToString())
            );

            // TelemetryOnly means not sending Kochava, Device, and OS information
            if (_telemetry > TelemetryLevel.TelemetryOnly)
            {
                bool isInitialized = !string.IsNullOrEmpty(_kochavaApiKey);
                AddIfNull(internal_event, "kochava_app_guid", _kochavaApiKey);
                AddIfNull(internal_event, "kochava_initialized", isInitialized.ToString());
                AddIfNull(internal_event, "kochava_device_id", _deviceId);
                AddIfNull(internal_event, "os", SystemInfo.operatingSystem);
                AddIfNull(internal_event, "os_family", GetOperatingSystemFamily(SystemInfo.operatingSystemFamily));
                AddIfNull(internal_event, "device_model", SystemInfo.deviceModel);
                AddIfNull(internal_event, "device_name", SystemInfo.deviceName);
                AddIfNull(internal_event, "device_type", GetDeviceType(SystemInfo.deviceType));
                AddIfNull(internal_event, "device_unity_unique_identifier", SystemInfo.deviceUniqueIdentifier);
                AddIfNull(internal_event, "device_processor_type", SystemInfo.processorType);
            }

            JObject createSessionEvent = new JObject(
                new JProperty("game_id", _gameId),
                new JProperty("event_type", "session_created"),
                new JProperty("created_at", DateTime.UtcNow.ToString("o")),
                new JProperty("event", internal_event)
            );

            JObject evt = new JObject(
                new JProperty("id", _sessionID),
                new JProperty("events", new JArray() { createSessionEvent })
            );

            // Asynchronous send event
            PostAsync("/game/game-event", evt.ToString());
        }

        private void PostAsync(string url, string data)
        {
            if (_printEventsToConsole)
            {
                var message = "[Helika] Event:" + data;
                Debug.Log(message);
            }

            if (_telemetry > TelemetryLevel.None)
            {
                UnityWebRequest request = new UnityWebRequest(_baseUrl + url, "POST");

                // Set the request method and content type
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-api-key", _helikaApiKey);

                // Convert the data to bytes and attach it to the request
                byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                // Send the request asynchronously
                request.SendWebRequest().completed += (asyncOperation) =>
                {
                    // Check for errors
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        // Display the error
                        Debug.LogError("Error: " + request.error + ", data: " + request.downloadHandler.text);
                        if (request.responseCode == 401)
                        {
                            Debug.LogError("API Key is invalid. Disabling Sending Messages. Please reach out to Helika Support to request a valid API key.");
                            _isInitialized = false;
                        }
                    }

                    // Clean up resources
                    request.Dispose();
                };
            }
        }

        private void InitializeKochava()
        {
            KochavaTracker.Instance.RegisterEditorAppGuid(_kochavaApiKey);
#if UNITY_ANDROID
            KochavaTracker.Instance.RegisterAndroidAppGuid(_kochavaApiKey);
#endif

#if UNITY_IOS
            KochavaTracker.Instance.RegisterIosAppGuid(_kochavaApiKey);
            KochavaTracker.Instance.SetIosAttAuthorizationAutoRequest(iosAttAuthorizationAutoRequest);
            KochavaTracker.Instance.SetIosAttAuthorizationWaitTime(iosAttAuthorizationWaitTime);
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

        private static string GetDeviceType(DeviceType type)
        {
            switch (type)
            {
                case DeviceType.Console:
                    return "Console";
                case DeviceType.Desktop:
                    return "Desktop";
                case DeviceType.Handheld:
                    return "Handheld";
                case DeviceType.Unknown:
                default:
                    return "Unknown";
            }
        }

        private static string GetOperatingSystemFamily(OperatingSystemFamily family)
        {
            switch (family)
            {
                case OperatingSystemFamily.Windows:
                    return "Windows";
                case OperatingSystemFamily.MacOSX:
                    return "MacOSX";
                case OperatingSystemFamily.Linux:
                    return "Linux";
                case OperatingSystemFamily.Other:
                default:
                    return "Other";
            }
        }
    }
}
