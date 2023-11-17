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
    public class HelikaAsset : MonoBehaviour
    {

        private Helika.EventManager eventManager;

        void Start()
        {
            eventManager = Helika.EventManager.Instance;
            eventManager.Init("4b22e2a34e2c95d9b46668a702ead7", "HelikaUnitySDK", Helika.HelikaBaseURL.Localhost);
        }


        async void Update()
        {
            if (Input.GetKeyDown("space"))
            {
                await eventManager.TestHelikaAPI();
            }
            if (Input.GetKeyDown("tab"))
            {
                JObject event1 = new JObject(new JProperty("id", "moneyEvent"));
                JObject event2 = new JObject(new JProperty("id", "moneyEvent2"), new JProperty("event", new JObject()), new JProperty("data", "event 2 data"));
                JObject[] testEvents = new JObject[] { event1, event2 };
                await eventManager.SendEvent(testEvents);
            }
        }
    }
}