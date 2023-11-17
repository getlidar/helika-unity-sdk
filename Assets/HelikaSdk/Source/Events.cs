using System;
using System.Collections;
using System.Collections.Generic;
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
        private const string SdkVersion = "0.1.0";

        private string _apiKey;
        protected string _baseUrl;
        protected string _gameId;
        protected string _sessionID;
        protected bool _isInitialized = false;


        public void Init(string apiKey, string gameId, string baseUrl)
        {
            if (_isInitialized)
            {
                return;
            }

            if (!HelikaBaseURL.validate(baseUrl))
            {
                throw new ArgumentException("Invalid Base URL");
            }

            _apiKey = apiKey;
            _gameId = gameId;
            _baseUrl = baseUrl;
            _sessionID = Guid.NewGuid().ToString();

            KochavaTracker.Instance.RegisterEditorAppGuid("kohelika-test-molp8ydo");
            KochavaTracker.Instance.RegisterAndroidAppGuid("kohelika-test-molp8ydo");
            KochavaTracker.Instance.RegisterIosAppGuid("kohelika-test-molp8ydo");
            KochavaTracker.Instance.Start();
        }

        public async Task<string> SendEvent(JObject[] helikaEvents)
        {
            JArray jarrayObj = new JArray();
            foreach (JObject helikaEvent in helikaEvents)
            {
                if (helikaEvent["event"] == null)
                {
                    helikaEvent["event"] = new JObject();
                }
                ((JObject)helikaEvent["event"]).Add("sessionID", _sessionID);

                helikaEvent.Add("created_at", DateTime.UtcNow.ToString());
                jarrayObj.Add(helikaEvent);
            }

            JObject newEvent = new JObject(
                new JProperty("id", _sessionID),
                new JProperty("events", jarrayObj)
            );
            // newEvent.Add("events", helikaEvents);
            Debug.Log(newEvent.ToString());
            return "test";
            // return await PostAsync("/game/game-event", newEvent.ToString());
        }

        public async Task<string> TestHelikaAPI()
        {
            var jsonObject = new
            {
                message = "test"
            };
            var jsonString = JsonConvert.SerializeObject(jsonObject);
            Debug.Log(jsonString);

            string returnData = await PostAsync("/game/test-event", jsonString);
            Debug.Log(returnData);
            return returnData;
            // return JsonUtility.FromJson<ReturnData>(returnData);
        }

        protected async Task<string> PostAsync(string url, string data)
        {
            // Create a UnityWebRequest object
            UnityWebRequest request = new UnityWebRequest(_baseUrl.ToString() + url, "POST");

            // Set the request method and content type
            // request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", _apiKey);

            // Convert the data to bytes and attach it to the request
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            // Send the request asynchronously
            await request.SendWebRequest();

            // Check for errors
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Display the response text
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            else
            {
                // Display the error
                Debug.LogError("Error: " + request.error + ", data: " + request.downloadHandler.text);
            }

            return request.downloadHandler.text;
        }
    }
}
