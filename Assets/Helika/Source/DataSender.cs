// using System;
// using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Helika
{
    public class EventPacket
    {
        public string Url { get; set; }
        public string Data { get; set; }
        public int RetryCount { get; set; }
        public float NextRetryTime { get; set; }

        public EventPacket(string url, string data)
        {
            Url = url;
            Data = data;
            RetryCount = 0;
            NextRetryTime = 0;
        }
    }

    public class DataSender : MonoBehaviour
    {
        private string _helikaApiKey = null;
        private Queue<EventPacket> retryQueue = new Queue<EventPacket>();
        private float retryInterval = 2.0f; // Retry every 2 seconds



        void Update()
        {
            ProcessRetryQueue();
        }

        public void SetHelikaApiKey(string helikaApiKey)
        {
            _helikaApiKey = helikaApiKey;
        }


        public void SendData(string url, string data)
        {
            EventPacket packet = new EventPacket(url, data);
            TrySendData(packet);
        }

        private void ProcessRetryQueue()
        {
            if (retryQueue.Count == 0 || _helikaApiKey == null)
                return;

            EventPacket packet = retryQueue.Peek();
            if (Time.time >= packet.NextRetryTime)
            {
                retryQueue.Dequeue();
                TrySendData(packet);
            }
        }

        private void TrySendData(EventPacket packet)
        {
            if (_helikaApiKey == null)
            {
                Debug.LogError("API Key is not initialized. Not sending any events until API key is initialized.");
            }

            UnityWebRequest request = new UnityWebRequest(packet.Url, "POST");

            // Set the request method and content type
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", _helikaApiKey);

            // Convert the data to bytes and attach it to the request
            byte[] bodyRaw = Encoding.UTF8.GetBytes(packet.Data);
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
                        Debug.LogError("API Key is invalid. Clearing API key");
                        _helikaApiKey = null;
                    }
                    packet.RetryCount++;
                    packet.NextRetryTime = Time.time + retryInterval;
                    retryQueue.Enqueue(packet);
                }

                // Clean up resources
                request.Dispose();
            };
        }
    }
}
