using System;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

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
        protected string _anonymous_id = null;
        protected JObject _appDetails = new JObject(
            new JProperty("platform_id", null),
            new JProperty("client_app_version", null),
            new JProperty("server_app_version", null),
            new JProperty("store_id", null),
            new JProperty("source_id", null)
        );
        protected JObject _userDetails = new JObject(
            new JProperty("user_id", null),
            new JProperty("email", null),
            new JProperty("wallet", null)
        );

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
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("Missing Helika API Key");
            }

            _helikaApiKey = apiKey;
            _gameId = gameId;
            _baseUrl = ConvertUrl(env);
            _sessionID = Guid.NewGuid().ToString();
            _anonymous_id = GenerateAnonymousId(_sessionID, true);

            if (_userDetails.SelectToken("user_id") == null)
            {
                _userDetails["user_id"] = _anonymous_id;
            }

            // If Localhost is set, force print events
            _telemetry = env != HelikaEnvironment.Localhost ? telemetryLevel : TelemetryLevel.None;

            // If PrintEventsToConsole is set to true, we only print the event to console and we don't send it
            _printEventsToConsole = printEventsToConsole;

            // TelemetryOnly means we shouldn't initialize Kochava
            if (_telemetry > TelemetryLevel.TelemetryOnly)
            {
                _piiTracking = true;
            }
            CreateSession();

            _isInitialized = true;
        }

        public JObject GetUserDetails()
        {
            return _userDetails;
        }

        public void SetUserDetails(JObject userDetails, bool createNewAnonId = false)
        {
            if (userDetails["user_id"] == null || userDetails["user_id"].Type == JTokenType.Null)
            {
                _anonymous_id = GenerateAnonymousId(Guid.NewGuid().ToString(), createNewAnonId);
                userDetails = new JObject(
                    new JProperty("user_id", _anonymous_id),
                    new JProperty("email", null),
                    new JProperty("wallet", null)
                );
            }
            _userDetails = userDetails;
        }

        public JObject GetAppDetails()
        {
            return _appDetails;
        }

        public void SetAppDetails(JObject appDetails)
        {
            _appDetails = appDetails;
        }

        public bool GetPIITracking()
        {
            return _piiTracking;
        }

        public void SetPIITracking(bool piiTracking, bool sendPIITrackingEvent = false)
        {
            _piiTracking = piiTracking;

            if (_isInitialized && _piiTracking && sendPIITrackingEvent)
            {
                JObject createSessionEvent = GetEventTemplate("session_created", "session_data_updated");
                JObject innerEvent = (JObject)createSessionEvent["event"];

                AddIfNull(innerEvent, "type", "Session Data Refresh");
                AppendHelikaData(innerEvent);
                AppendUserDetails(innerEvent);
                AppendAppDetails(innerEvent);
                AppendPIITracking(innerEvent);

                JObject evt = new JObject(
                    new JProperty("id", Guid.NewGuid().ToString()),
                    new JProperty("events", new JArray() { createSessionEvent })
                );

                // Asynchronous send event
                PostAsync("/game/game-event", evt.ToString());
            }
        }

        public void SendEvent(JObject eventProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            JObject serializedEvt = new JObject(
                new JProperty("id", Guid.NewGuid().ToString()),
                new JProperty("events", new JArray() { AppendAttributesToJObject(eventProps, false) })
            );
            PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public void SendEvents(JArray eventsProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            // Add helika-specific data to the events
            JArray events = new JArray { };
            foreach (JObject eventProp in eventsProps)
            {
                events.Add(AppendAttributesToJObject(eventProp, false));
            }

            JObject serializedEvt = new JObject(
                new JProperty("id", Guid.NewGuid().ToString()),
                new JProperty("events", events)
            );
            PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public void SendUserEvent(JObject eventProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            JObject serializedEvt = new JObject(
                new JProperty("id", Guid.NewGuid().ToString()),
                new JProperty("events", new JArray() { AppendAttributesToJObject(eventProps, true) })
            );
            PostAsync("/game/game-event", serializedEvt.ToString());
        }

        public void SendUserEvents(JArray eventsProps)
        {
            if (!_isInitialized)
            {
                throw new Exception("Event Manager is not yet initialized");
            }

            // Add helika-specific data to the events
            JArray jarrayObj = new JArray();
            foreach (JObject eventProp in eventsProps)
            {
                jarrayObj.Add(AppendAttributesToJObject(eventProp, true));
            }

            JObject newEvent = new JObject(
                new JProperty("id", Guid.NewGuid().ToString()),
                new JProperty("events", jarrayObj)
            );
            PostAsync("/game/game-event", newEvent.ToString());
        }

        public void SetPrintToConsole(bool printToConsole)
        {
            _printEventsToConsole = printToConsole;
        }

        private void CreateSession()
        {
            JObject createSessionEvent = GetEventTemplate("session_created", "session_created");
            JObject internalEvent = (JObject)createSessionEvent["event"];
            AppendHelikaData(internalEvent);
            AppendUserDetails(internalEvent);
            AppendAppDetails(internalEvent);
            if (_piiTracking)
            {
                AppendPIITracking(internalEvent);
            }

            JObject evt = new JObject(
                new JProperty("id", Guid.NewGuid().ToString()),
                new JProperty("events", new JArray() { createSessionEvent })
            );

            // Asynchronous send event
            PostAsync("/game/game-event", evt.ToString());
        }

        private JObject AppendAttributesToJObject(JObject obj, bool isUserEvent)
        {
            // Forcefully overwrite the game id in the event if any
            AddOrReplace(obj, "game_id", _gameId);

            // Convert to ISO 8601 format string using "o" specifier
            AddOrReplace(obj, "created_at", DateTime.UtcNow.ToString("o"));

            if (obj.SelectToken("event_type") == null || string.IsNullOrWhiteSpace(obj.GetValue("event_type").ToString()))
            {
                throw new ArgumentException("Invalid Event: Missing 'event_type' field");
            }

            if (obj.SelectToken("event") == null)
            {
                obj.Add(new JProperty("event", new JObject()));
            }

            if (obj.GetValue("event").GetType() != typeof(JObject))
            {
                throw new ArgumentException("Invalid Event: 'event' field must be of type [Newtonsoft.Json.Linq.JObject]");
            }

            if (obj.SelectToken("event.event_sub_type") == null || string.IsNullOrWhiteSpace(obj.SelectToken("event.event_sub_type").ToString()))
            {
                throw new ArgumentException("Invalid Event: Missing 'event_sub_type' field");
            }

            JObject internalEvent = (JObject)obj["event"];
            AddOrReplace(internalEvent, "session_id", _sessionID);

            // if it's a user id, use the user_id from the userDetails, otherwise, use the anonymous id
            AddOrReplace(internalEvent, "user_id", isUserEvent ? _userDetails["user_id"].ToString() : _anonymous_id);

            AppendHelikaData(internalEvent);
            AppendAppDetails(internalEvent);

            // Only append User Details if the it's a user event
            if (isUserEvent)
            {
                AppendUserDetails(internalEvent);
            }

            return obj;
        }

        // JObject Helpers
        private JObject GetEventTemplate(string event_type, string event_sub_type)
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
                    )
                )
            );
        }

        private void AppendPIITracking(JObject gameEvent)
        {
            JObject piiData = new JObject(
                new JProperty("os", SystemInfo.operatingSystem),
                new JProperty("os_family", GetOperatingSystemFamily(SystemInfo.operatingSystemFamily)),
                new JProperty("device_model", SystemInfo.deviceModel),
                new JProperty("device_name", SystemInfo.deviceName),
                new JProperty("device_type", GetDeviceType(SystemInfo.deviceType)),
                new JProperty("device_unity_unique_identifier", SystemInfo.deviceUniqueIdentifier),
                new JProperty("device_processor_type", SystemInfo.processorType)
            );

            AddIfNull(gameEvent, "helika_data", new JObject());
            AddOrReplace((JObject)gameEvent["helika_data"], "additional_user_info", piiData);
        }

        private void AppendHelikaData(JObject gameEvent)
        {
            JObject helikaData = new JObject(
                new JProperty("anon_id", _anonymous_id),
                new JProperty("taxonomy_ver", "v2"),
                new JProperty("sdk_name", SdkName),
                new JProperty("sdk_version", SdkVersion),
                new JProperty("sdk_class", SdkClass),
                new JProperty("sdk_platform", Application.platform.ToString()),
                new JProperty("event_source", "client"),
                new JProperty("pii_tracking", _piiTracking)
            );
            AddIfNull(gameEvent, "helika_data", new JObject());
            MergeJObjects((JObject)gameEvent["helika_data"], helikaData);
        }

        private void AppendUserDetails(JObject gameEvent)
        {
            AddIfNull(gameEvent, "user_details", new JObject());
            MergeJObjects((JObject)gameEvent["user_details"], _userDetails);
        }

        private void AppendAppDetails(JObject gameEvent)
        {
            AddIfNull(gameEvent, "app_details", new JObject());
            MergeJObjects((JObject)gameEvent["app_details"], _appDetails);
        }

        private void PostAsync(string url, string data)
        {
            if (_printEventsToConsole)
            {
                var message = "[Helika] Event Sent: " + (_telemetry > TelemetryLevel.None ? "Sent" : "Print Only") + "\nEvent:\n" + data;
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

        private static void AddIfNull(JObject helikaEvent, string key, JToken newValue)
        {
            if (!helikaEvent.ContainsKey(key))
            {
                helikaEvent.Add(key, newValue);
            }
        }


        private static void AddOrReplace(JObject helikaEvent, string key, JToken newValue)
        {
            JToken gameIdObj = helikaEvent.SelectToken(key);
            if (gameIdObj != null)
            {
                gameIdObj.Replace(newValue);
            }
            else
            {
                helikaEvent.Add(key, newValue);
            }
        }

        private static void MergeJObjects(JObject obj1, JObject obj2)
        {
            obj1.Merge(obj2, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union // Can also use MergeArrayHandling.Replace or MergeArrayHandling.Concat
            });
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
