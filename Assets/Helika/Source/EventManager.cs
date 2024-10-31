using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Security.Cryptography;
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
        private const string SdkVersion = "0.3.0";
        private const string SdkClass = "EventManager";

        private string _helikaApiKey;
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
        protected bool _piiTracking = false;
        protected string _anonymous_id = "";
        protected Dictionary<string, object> _appDetails = new Dictionary<string, object>{
            { "platform_id", null },
            { "client_app_version", null },
            { "server_app_version", null },
            { "store_id", null },
            { "source_id", null }
        };
        protected Dictionary<string, object> _userDetails = new Dictionary<string, object>{
            { "user_id", null },
            { "email", null },
            { "wallet", null }
        };

        public void Init(string apiKey, string gameId, HelikaEnvironment env, TelemetryLevel telemetryLevel = TelemetryLevel.All, bool printEventsToConsole = false)
        {
            if (_isInitialized)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentException("Missing Game ID");
            }

            _helikaApiKey = apiKey;
            _gameId = gameId;
            _baseUrl = ConvertUrl(env);
            _sessionID = Guid.NewGuid().ToString();
            _anonymous_id = GenerateAnonymousId(_sessionID, true);

            _userDetails["user_id"] = _anonymous_id;

            _isInitialized = true;

            // If Localhost is set, force print events
            _telemetry = env != HelikaEnvironment.Localhost ? telemetryLevel : TelemetryLevel.None;

            // If PrintEventsToConsole is set to true, we only print the event to console and we don't send it
            _printEventsToConsole = printEventsToConsole;

            if (_telemetry > TelemetryLevel.None)
            {

                // TelemetryOnly means we shouldn't initialize Kochava
                if (_telemetry > TelemetryLevel.TelemetryOnly)
                {
                    _piiTracking = true;
                }
                CreateSession();
            }
        }

        public Dictionary<string, object> GetUserDetails()
        {
            return _userDetails;
        }

        public void SetUserDetails(Dictionary<string, object> userDetails, bool createNewAnonId = false)
        {
            if (userDetails["user_id"] == null)
            {
                _userDetails = new Dictionary<string, object>{
                    { "user_id", GenerateAnonymousId(Guid.NewGuid().ToString(), createNewAnonId) },
                    { "email", null },
                    { "wallet", null }
                };
            }
            _userDetails = userDetails;
        }


        public Dictionary<string, object> GetAppDetails()
        {
            return _userDetails;
        }

        public void SetAppDetails(Dictionary<string, object> appDetails)
        {
            _appDetails = appDetails;
        }

        public bool GetPIITracking()
        {
            return _piiTracking;
        }

        public void SetPIITracking(bool piiTracking)
        {
            _piiTracking = piiTracking;

            // todo: fire an event for "Session Data Refresh" containing the pii tracking
        }

        // Todo: Update SendEvents()
        public void SendEvent(string eventName, Dictionary<string, object> eventProps)
        {
            // if (!_isInitialized)
            // {
            //     throw new Exception("Event Manager is not yet initialized");
            // }

            // Dictionary<string, object> finalEvent = new Dictionary<string, object>();
            // finalEvent["id"] = Guid.NewGuid().ToString();
            // finalEvent["events"] = new Dictionary<string, object>[] { AppendAttributesToDictionary(eventName, eventProps) };

            // JObject serializedEvt = JObject.FromObject(finalEvent);
            // PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public void SendEvents(string eventName, Dictionary<string, object>[] eventsProps)
        {
            // if (!_isInitialized)
            // {
            //     throw new Exception("Event Manager is not yet initialized");
            // }

            // // Add helika-specific data to the events
            // List<Dictionary<string, object>> events = new List<Dictionary<string, object>> { };
            // foreach (Dictionary<string, object> eventProps in eventsProps)
            // {
            //     events.Add(AppendAttributesToDictionary(eventName, eventProps));
            // }

            // Dictionary<string, object> finalEvent = new Dictionary<string, object>();
            // finalEvent["id"] = Guid.NewGuid().ToString();
            // finalEvent["events"] = events.ToArray();

            // JObject serializedEvt = JObject.FromObject(finalEvent);
            // PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public void SendUserEvent(JObject eventProps)
        {
            // if (!_isInitialized)
            // {
            //     throw new Exception("Event Manager is not yet initialized");
            // }

            // JObject newEvent = new JObject(
            //     new JProperty("id", Guid.NewGuid().ToString()),
            //     new JProperty("events", new JArray() { AppendAttributesToJObject(eventProps) })
            // );
            // PostAsync("/game/game-event", newEvent.ToString());
        }

        public void SendUserEvents(JObject[] eventsProps)
        {
            // if (!_isInitialized)
            // {
            //     throw new Exception("Event Manager is not yet initialized");
            // }

            // // Add helika-specific data to the events
            // JArray jarrayObj = new JArray();
            // foreach (JObject eventProp in eventsProps)
            // {
            //     jarrayObj.Add(AppendAttributesToJObject(eventProp));
            // }

            // JObject newEvent = new JObject(
            //     new JProperty("id", Guid.NewGuid().ToString()),
            //     new JProperty("events", jarrayObj)
            // );
            // PostAsync("/game/game-event", newEvent.ToString());
        }

        public void SetPrintToConsole(bool printToConsole)
        {
            _printEventsToConsole = printToConsole;
        }

        // private JObject AppendAttributesToJObject(JObject obj)
        // {
        //     // Add game_id only if the event doesn't already have it
        //     AddIfNull(obj, "game_id", _gameId);

        //     // Convert to ISO 8601 format string using "o" specifier
        //     AddOrReplace(obj, "created_at", DateTime.UtcNow.ToString("o"));

        //     if (!obj.ContainsKey("event_type") || string.IsNullOrWhiteSpace(obj.GetValue("event_type").ToString()))
        //     {
        //         throw new ArgumentException("Invalid Event: Missing 'event_type' field");
        //     }

        //     if (!obj.ContainsKey("event"))
        //     {
        //         obj.Add(new JProperty("event", new JObject()));
        //     }

        //     if (obj.GetValue("event").GetType() != typeof(Newtonsoft.Json.Linq.JObject))
        //     {
        //         throw new ArgumentException("Invalid Event: 'event' field must be of type [Newtonsoft.Json.Linq.JObject]");
        //     }

        //     JObject internalEvent = (JObject)obj.GetValue("event");
        //     AddOrReplace(internalEvent, "session_id", _sessionID);

        //     if (!string.IsNullOrWhiteSpace(_playerID))
        //     {
        //         AddOrReplace(internalEvent, "player_id", _playerID);
        //     }

        //     return obj;
        // }

        // private Dictionary<string, object> AppendAttributesToDictionary(string eventName, Dictionary<string, object> eventProps)
        // {
        //     Dictionary<string, object> helikaEvent = new Dictionary<string, object>()
        //     {
        //         // Add game_id only if the event doesn't already have it
        //         {"game_id", _gameId},
        //         // Convert to ISO 8601 format string using "o" specifier
        //         {"created_at", DateTime.UtcNow.ToString("o")},
        //         // Set event_type
        //         {"event_type", eventName},
        //     };

        //     eventProps["session_id"] = _sessionID;
        //     if (!string.IsNullOrWhiteSpace(_playerID))
        //     {
        //         eventProps["player_id"] = _playerID;
        //     }

        //     helikaEvent["event"] = eventProps;

        //     return helikaEvent;
        // }

        private void CreateSession()
        {
            Dictionary<string, object> createSessionEvent = GetEventDictTemplate("session_created", "session_created");

            AppendHelikaData((Dictionary<string, object>)createSessionEvent["event"]);
            AppendUserDetails((Dictionary<string, object>)createSessionEvent["event"]);
            AppendAppDetails((Dictionary<string, object>)createSessionEvent["event"]);
            if (_piiTracking)
            {
                AppendPIITracking((Dictionary<string, object>)createSessionEvent["event"]);
            }

            var evt = new Dictionary<string, object>() {
                {"id", Guid.NewGuid().ToString()},
                {"events", new List<Dictionary<string, object>> {createSessionEvent }}
            };

            // Asynchronous send event
            JObject serializedEvt = JObject.FromObject(evt);
            PostAsync("/game/game-event", serializedEvt.ToString());
        }

        // Dictionary Helpers
        private Dictionary<string, object> GetEventDictTemplate(string event_type, string event_sub_type)
        {
            return new Dictionary<string, object>() {
                // Add game_id only if the event doesn't already have it
                {"game_id", _gameId},
                // Convert to ISO 8601 format string using "o" specifier
                {"created_at", DateTime.UtcNow.ToString("o")},
                {"event_type", event_type},
                {"event", new Dictionary<string, object>() {
                    { "user_id", _userDetails["user_id"].ToString() },
                    { "session_id", _sessionID },
                    { "event_sub_type", event_sub_type },
                    { "event_detail", new Dictionary<string, object>(){} }
                }},
            };
        }

        private void AppendPIITracking(Dictionary<string, object> gameEvent)
        {
            Dictionary<string, object> piiData = new Dictionary<string, object>{
                {"os", SystemInfo.operatingSystem},
                {"os_family", GetOperatingSystemFamily(SystemInfo.operatingSystemFamily)},
                {"device_model", SystemInfo.deviceModel},
                {"device_name", SystemInfo.deviceName},
                {"device_type", GetDeviceType(SystemInfo.deviceType)},
                {"device_unity_unique_identifier", SystemInfo.deviceUniqueIdentifier},
                {"device_processor_type", SystemInfo.processorType}
            };

            if (!gameEvent.ContainsKey("helika_data"))
            {
                gameEvent.Add("helika_data", new Dictionary<string, object> { });
            }

            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("additional_user_info", piiData);
        }

        private void AppendHelikaData(Dictionary<string, object> gameEvent)
        {
            if (!gameEvent.ContainsKey("helika_data"))
            {
                gameEvent.Add("helika_data", new Dictionary<string, object> { });
            }

            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("anon_id", _anonymous_id);
            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("taxonomy_ver", "v2");
            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("sdk_name", SdkName);
            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("sdk_version", SdkVersion);
            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("sdk_class", SdkClass);
            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("sdk_platform", Application.platform.ToString());
            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("event_source", "client");
            ((Dictionary<string, object>)gameEvent["helika_data"]).Add("pii_tracking", _piiTracking);
        }

        private void AppendUserDetails(Dictionary<string, object> gameEvent)
        {
            if (!gameEvent.ContainsKey("user_details"))
            {
                gameEvent.Add("user_details", new Dictionary<string, object> { });
            }

            MergeDictBIntoA((Dictionary<string, object>)gameEvent["user_details"], _userDetails);
        }

        private void AppendAppDetails(Dictionary<string, object> gameEvent)
        {
            if (!gameEvent.ContainsKey("app_details"))
            {
                gameEvent.Add("app_details", new Dictionary<string, object> { });
            }

            MergeDictBIntoA((Dictionary<string, object>)gameEvent["app_details"], _appDetails);
        }

        // JObject Helpers
        private JObject GetEventJObjectTemplate(string event_type, string event_sub_type)
        {
            return new JObject(
                new JProperty("created_at", DateTime.UtcNow.ToString("o")),
                new JProperty("game_id", _gameId),
                new JProperty("event_type", event_type),
                new JProperty("event", new JObject(
                    new JProperty("user_id", _userDetails["user_id"].ToString()),
                    new JProperty("session_id", _sessionID),
                    new JProperty("event_sub_type", event_sub_type),
                    new JProperty("event_detail", new JObject())
                ))
            );
        }

        private void AppendPIITracking(JObject gameEvent)
        {
            // Todo: 
        }

        private void AppendHelikaData(JObject gameEvent)
        {
            // Todo: 
        }

        private void AppendUserDetails(JObject gameEvent)
        {
            // Todo: 
        }

        private void AppendAppDetails(JObject gameEvent)
        {
            // Todo: 
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

        private string GenerateAnonymousId(string seed, bool createNewAnonId = false)
        {
            if (createNewAnonId)
            {
                return "anon_" + ComputeSha256Hash(seed);
            }
            return _anonymous_id;
        }

        private Dictionary<string, object> MergeDictBIntoA(Dictionary<string, object> dictA, Dictionary<string, object> dictB)
        {
            foreach (var kvp in dictB)
            {
                dictA[kvp.Key] = kvp.Value;
            }
            return dictA;
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

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); // Convert byte to hex
                }
                return builder.ToString();
            }
        }
    }
}
