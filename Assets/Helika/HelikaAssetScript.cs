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
        public TelemetryLevel telemetry = TelemetryLevel.None;
        public bool printEventsToConsole = true;
        private EventManager eventManager;
        public string playerId;

        void Start()
        {
            eventManager = EventManager.Instance;
            eventManager.SetUserDetails(
                new JObject(
                    new JProperty("user_id", playerId),
                    new JProperty("email", "test@gmail.com"),
                    new JProperty("wallet", "0x8540507642419A0A8Af94Ba127F175dA090B58B0")
                )
            );
            eventManager.SetAppDetails(
                new JObject(
                    new JProperty("platform_id", "Android"),
                    new JProperty("client_app_version", "0.0.1"),
                    new JProperty("server_app_version", null),
                    new JProperty("store_id", "EpicGames"),
                    new JProperty("source_id", null)
                )
            );
            eventManager.Init(apiKey, gameId, helikaEnv, telemetry, printEventsToConsole);
        }

        // void Update()
        // {
        //     if (Input.GetKeyDown("space"))
        //     {
        //         // This is an example of sending a single user event
        //         JObject playerKilledEvent = new JObject(
        //             new JProperty("event_type", "player_event"),
        //             new JProperty("event", new JObject(
        //                     new JProperty("event_sub_type", "player_killed"),
        //                     new JProperty("user_id", 10),
        //                     new JProperty("damage_amount", 40),
        //                     new JProperty("bullets_fired", 15),
        //                     new JProperty("map", "arctic")
        //                 )
        //             )
        //         );
        //         eventManager.SendUserEvent(playerKilledEvent);

        //         // This is an example of sending multiple events at once
        //         JObject evt1 = new JObject(
        //             new JProperty("event_type", "bomb_event"),
        //             new JProperty("event", new JObject(
        //                     new JProperty("event_sub_type", "bomb_planted"),
        //                     new JProperty("map", "arctic"),
        //                     new JProperty("team", "counter-terrorists")
        //                 )
        //             )
        //         );

        //         JObject evt2 = new JObject(
        //             new JProperty("event_type", "bomb_event"),
        //             new JProperty("event", new JObject(
        //                     new JProperty("event_sub_type", "bomb_diffused"),
        //                     new JProperty("map", "arctic"),
        //                     new JProperty("team", "counter-terrorists"),
        //                     new JProperty("duration", 10210.121)
        //                 )
        //             )
        //         );
        //         eventManager.SendUserEvents(new JArray() { evt1, evt2 });

        //         // This is an example of a non-user event. For Non-user events, we don't automatically append user information
        //         JObject winEvent = new JObject(
        //             new JProperty("event_type", "game_finished"),
        //             new JProperty("event", new JObject(
        //                     new JProperty("event_sub_type", "win_results"),
        //                     new JProperty("winner", "counter-terrorists"),
        //                     new JProperty("map", "arctic")
        //                 )
        //             )
        //         );
        //         eventManager.SendEvent(winEvent);
        //     }

        //     if (Input.GetKeyDown("u"))
        //     {
        //         eventManager.SetUserDetails(
        //             new JObject(
        //                 new JProperty("user_id", null)
        //             )
        //         );
        //         // Clear and Reset User Details
        //         JObject logoutEvent = new JObject(
        //             new JProperty("event_type", "logout"),
        //             new JProperty("event", new JObject(
        //                     new JProperty("event_sub_type", "user_logged_out")
        //                 )
        //             )
        //         );
        //         eventManager.SendUserEvent(logoutEvent);


        //         eventManager.SetUserDetails(
        //             new JObject(
        //                 new JProperty("user_id", "new_player_id")
        //             )
        //         );
        //         // Clear and Reset User Details
        //         JObject loginEvent = new JObject(
        //             new JProperty("event_type", "login"),
        //             new JProperty("event", new JObject(
        //                     new JProperty("event_sub_type", "user_logged_in")
        //                 )
        //             )
        //         );
        //         eventManager.SendUserEvent(loginEvent);
        //     }

        //     if (Input.GetKeyDown("a"))
        //     {
        //         var appDetails = eventManager.GetAppDetails();
        //         appDetails["source_id"] = "google_ads";
        //         appDetails["client_app_version"] = "0.0.3";
        //         eventManager.SetAppDetails(appDetails);

        //         // Clear and Reset User Details
        //         JObject upgradeEvent = new JObject(
        //             new JProperty("event_type", "upgrade"),
        //             new JProperty("event", new JObject(
        //                     new JProperty("event_sub_type", "upgrade_finished")
        //                 )
        //             )
        //         );
        //         eventManager.SendUserEvent(upgradeEvent);
        //     }

        //     if (Input.GetKeyDown("p"))
        //     {
        //         var piiTracking = eventManager.GetPIITracking();
        //         eventManager.SetPIITracking(!piiTracking, true);
        //     }
        // }
    }
}