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

namespace Helika
{
    public class EventManager : HelikaSingletonScriptableObject<EventManager>
    {
        // Version data that is updated via a script. Do not change.
        private const string SdkName = "Unity";
        private const string SdkVersion = "0.1.6.kote-min";
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

        public bool iosAttAuthorizationAutoRequest = true;
        public double iosAttAuthorizationWaitTime = 30;

        public void Init(string apiKey, string gameId, HelikaEnvironment env, bool enabled = false)
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

            // If Localhost is set, force disable sending events
            _enabled = env != HelikaEnvironment.Localhost ? enabled : false;

            CreateSession();
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
            Debug.Log("Creating Session");
            Dictionary<string, object> createSessionEvent = new Dictionary<string, object>()
            {
                // Add game_id only if the event doesn't already have it
                {"game_id", _gameId},
                // Convert to ISO 8601 format string using "o" specifier
                {"created_at", DateTime.UtcNow.ToString("o")},
                // Set event_type
                {"event_type", "session_created"},
                {"event", new Dictionary<string, object>
                    {
                        { "session_id", _sessionID },
                        { "player_id", _playerID },
                        { "sdk_name", SdkName },
                        { "sdk_version", SdkVersion },
                        { "sdk_class", SdkClass },
                        { "sdk_platform",  Application.platform.ToString() },
                        { "kochava_app_guid", _kochavaApiKey },
                        { "kochava_initialized", !string.IsNullOrEmpty(_kochavaApiKey) },
                        { "kochava_device_id", _deviceId },
                        { "event_sub_type", "session_created" },
                        { "os", SystemInfo.operatingSystem },
                        { "os_family", GetOperatingSystemFamily(SystemInfo.operatingSystemFamily) },
                        { "device_model", SystemInfo.deviceModel },
                        { "device_name", SystemInfo.deviceName },
                        { "device_type", GetDeviceType(SystemInfo.deviceType) },
                        { "device_unity_unique_identifier", SystemInfo.deviceUniqueIdentifier },
                        { "device_processor_type", SystemInfo.processorType },
                    }
                }
            };

            Dictionary<string, object> evt = new Dictionary<string, object>()
            {
                {"id", _sessionID},
                {"events", new Dictionary<string, object>[] { createSessionEvent }},
            };

            // Asynchronous send event
            JObject serializedEvt = JObject.FromObject(evt);
            PostAsync("/game/game-event", serializedEvt.ToString());
        }

        private void PostAsync(string url, string data)
        {
            Debug.Log("Event sent: " + data);
            if (!_enabled)
            {
                var message = "Event sent: " + data;
                Debug.Log(message);
                return;
            }

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
