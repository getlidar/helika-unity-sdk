using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using Helika;
using Newtonsoft.Json.Linq;

namespace Helika
{
    public class HelikaAssetScript : MonoBehaviour
    {
        public string apiKey;
        public string gameId;
        public HelikaEnvironment helikaEnv = HelikaEnvironment.Localhost;
        public bool sendingEvents = false;
        private EventManager eventManager;
        public string gamerId;

        void Start()
        {
            eventManager = EventManager.Instance;
            eventManager.Init(apiKey, gameId, helikaEnv, sendingEvents);
            eventManager.SetGamerID(gamerId);
        }


        async void Update()
        {
            if (Input.GetKeyDown("space"))
            {
                // This is an example of how to send an event
                JObject startEvent = new JObject(
                    new JProperty("event_type", "Start Event"),
                    new JProperty("event", new JObject(
                        new JProperty("user_id", "Start Event"),
                        new JProperty("name", "John Doe"),
                        new JProperty("email", "john@doe.com")
                    ))
                );
                JObject middleEvent = new JObject(
                    new JProperty("event_type", "No Event")
                );
                JObject endEvent = new JObject(
                    new JProperty("game_id", "Override Project"),
                    new JProperty("event_type", "End Event"),
                    new JProperty("event", new JObject(
                        new JProperty("user_id", 10)
                    ))
                );
                JObject[] testEvents = new JObject[] { startEvent, middleEvent, endEvent };
                await eventManager.SendEvent(testEvents);
            }
        }
    }
}