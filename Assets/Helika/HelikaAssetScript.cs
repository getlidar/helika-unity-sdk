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
        public string playerId;

        async Task Start()
        {
            eventManager = EventManager.Instance;
            await eventManager.Init(apiKey, gameId, helikaEnv, sendingEvents);
            eventManager.SetPlayerID(playerId);
        }


        // async void Update()
        // {
        //     if (Input.GetKeyDown("space"))
        //     {
        //         // This is an example of sending a single event
        //         Dictionary<string, object> dictionary = new Dictionary<string, object>
        //         {
        //             { "key1", 1 },
        //             { "key2", "value2" },
        //             { "key3", "value3" }
        //         };
        //         await eventManager.SendEvent("single_event", dictionary);

        //         // This is an example of sending multiples of a single event
        //         Dictionary<string, object> evt1 = new Dictionary<string, object>
        //         {
        //             { "key1", "Event 1" },
        //         };
        //         Dictionary<string, object> evt2 = new Dictionary<string, object>
        //         {
        //             { "key1", "Event 1" },
        //         };
        //         await eventManager.SendEvents("multiple_events", new Dictionary<string, object>[] { evt1, evt2 });

        //         // This is an example of how to send multiple unique events 
        //         JObject startEvent = new JObject(
        //             new JProperty("event_type", "start_event"),
        //             new JProperty("event", new JObject(
        //                 new JProperty("user_id", 10),
        //                 new JProperty("name", "John Doe"),
        //                 new JProperty("email", "john@doe.com")
        //             ))
        //         );
        //         // This event has no 'event' data. We automatically generate it.
        //         JObject middleEvent = new JObject(
        //             new JProperty("event_type", "no_event")
        //         );
        //         // This event overwrites the `game_id` field
        //         JObject endEvent = new JObject(
        //             new JProperty("game_id", "Override Project"),
        //             new JProperty("event_type", "end_event"),
        //             new JProperty("event", new JObject(
        //                 new JProperty("user_id", 10)
        //             ))
        //         );
        //         JObject[] uniqueEvents = new JObject[] { startEvent, middleEvent, endEvent };
        //         await eventManager.SendEvents(uniqueEvents);
        //     }
        // }
    }
}