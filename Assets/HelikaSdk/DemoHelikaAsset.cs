using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using Helika;
using Newtonsoft.Json.Linq;

namespace Rawrshak
{
    public class DemoHelikaAsset : MonoBehaviour
    {
        public string ApiKey;
        public string GameId;
        public string BaseUrl = Helika.HelikaBaseURL.Localhost;
        public bool SendingEvents = false;
        private Helika.EventManager eventManager;
        public string gamerId;

        void Start()
        {
            eventManager = Helika.EventManager.Instance;
            eventManager.Init(ApiKey, GameId, BaseUrl, SendingEvents);
            eventManager.SetGamerID(gamerId);
        }


        async void Update()
        {
            if (Input.GetKeyDown("space"))
            {
                JObject startEvent = new JObject(
                    new JProperty("game_id", "Test Project"),
                    new JProperty("event_type", "Start Event"),
                    new JProperty("event", new JObject(
                        new JProperty("user_id", 10),
                        new JProperty("name", "John Doe"),
                        new JProperty("email", "john@doe.com")
                    ))
                );
                JObject endEvent = new JObject(
                    new JProperty("game_id", "Test Project"),
                    new JProperty("event_type", "End Event"),
                    new JProperty("event", new JObject(
                        new JProperty("user_id", 10)
                    ))
                );
                JObject[] testEvents = new JObject[] { startEvent, endEvent };
                await eventManager.SendEvent(testEvents);
            }
        }
    }
}