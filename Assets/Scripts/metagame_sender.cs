﻿using System; 
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using DeltaDNA;

namespace metagameTutorial
{
    public class metagame_sender : MonoBehaviour
    {
        // This is the EnvironmentKey and REST API URL for the "Publisher MetaGame" created on the deltaDNA platform
        private const string ENVIRONMENT_KEY                = "49994906769133941928339970015138";
        private const string COLLECT_REST_API_URL           = "https://collect12660pblsh.deltadna.net/collect/api";
        private const string ENGAGE_REST_API_URL            = "https://engage12660pblsh.deltadna.net";


        public bool IsUploading { get; private set; }


        // Events will be stored inside this List and uploaded in batches. 
        List<string> events = new List<string>();


        // Call this when the game is launched, just after the deltaDNA SDK has been started.
        // It will record a gameStarted and clientDevice event and upload them to the "Publisher MetaGame"
        public void StartMetaGame()
        {   
            ////////////////////////////////////////////////////////
            // Create a gameStarted event for the Publisher MetaGame
            GameEvent metaGameStarted = new GameEvent("gameStarted")
                .AddParam("clientVersion", metagameTutorial.metagame_tutorial.CLIENT_VERSION)
                .AddParam("userLocale", ClientInfo.Locale);

            RecordMetaGameEvent(metaGameStarted);

            /////////////////////////////////////////////////////////
            // Create a clientDevice event for the Publisher MetaGame
            GameEvent metaclientDevice = new GameEvent("clientDevice")
                .AddParam("deviceName", ClientInfo.DeviceName)
                .AddParam("deviceType", ClientInfo.DeviceType)
                    .AddParam("hardwareVersion", ClientInfo.DeviceModel)
                    .AddParam("operatingSystem", ClientInfo.OperatingSystem)
                    .AddParam("operatingSystemVersion", ClientInfo.OperatingSystemVersion)
                    .AddParam("timezoneOffset", ClientInfo.TimezoneOffset)
                    .AddParam("userLanguage", ClientInfo.LanguageCode);

            if (ClientInfo.Manufacturer != null)
            {
                metaclientDevice.AddParam("manufacturer", ClientInfo.Manufacturer);
            };

            RecordMetaGameEvent(metaclientDevice);
            
            ////////////////////////////////////////////////////////////////
            // Upload gameStarted and clientDevice Publisher MetaGame events
            StartCoroutine(UploadMetaGameEvents());
        }        


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // The Following Methods are modified versions of event recording and upload methods found in the deltaDNA SDK, 
        // In Assets\DeltaDNA\DDNA.cs
        // They don't use local cached storage as they are sending to a different API endpoint than the DDNA SDK Instance.        
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////


        // Build Events and store them as JSON strings in a LIST
        private void RecordMetaGameEvent<T>(T gameEvent) where T : GameEvent<T>
        {
            if (!DDNA.Instance.isActiveAndEnabled)
            {
                throw new Exception("You must first start the SDK via the StartSDK method");
            }

            // COMMON PARAMETERS ADDED TO ALL EVENTS, 
            // Note: The gameID, gameName and gameGenre parameters for capturing which game is being played for the Publisher Metagame view.
            gameEvent.AddParam("platform", DDNA.Instance.Platform);
            gameEvent.AddParam("sdkVersion", Settings.SDK_VERSION);
            gameEvent.AddParam("gameID", metagame_tutorial.GAME_ID);
            gameEvent.AddParam("gameName", metagame_tutorial.GAME_NAME);
            gameEvent.AddParam("gameGenre", metagame_tutorial.GAME_GENRE);

            var eventSchema = gameEvent.AsDictionary();
            eventSchema["userID"]       = DDNA.Instance.UserID;
            eventSchema["sessionID"]    = DDNA.Instance.SessionID;
            eventSchema["eventUUID"]    = Guid.NewGuid().ToString();

            string currentTimestmp = GetCurrentTimestamp();
            if (currentTimestmp != null)
            {
                eventSchema["eventTimestamp"] = GetCurrentTimestamp();
            }

            try
            {
               events.Add(DeltaDNA.MiniJSON.Json.Serialize(eventSchema));
                DeltaDNA.Logger.LogDebug(string.Format("Sending MetaGame '{0}' event", gameEvent.Name));
            }
            catch (Exception ex)
            {
                DeltaDNA.Logger.LogWarning("Unable to generate JSON for '" + gameEvent.Name + "' event. " + ex.Message);
            }
        }


        // Upload events from JSON string list
        private IEnumerator UploadMetaGameEvents()
        {
            this.IsUploading = true;
            try
            {   
            
                if (events != null && events.Count > 0)
                {
                    // Try to upload metagame events
                    DeltaDNA.Logger.LogDebug("Starting MetaGame event upload.");

                    Action<bool, int> postCb = (succeeded, statusCode) =>
                    {
                        if (succeeded)
                        {
                            DeltaDNA.Logger.LogDebug("MetaGameEvent upload successful.");
                            this.events.Clear();
                        }
                        else if (statusCode == 400)
                        {
                            DeltaDNA.Logger.LogDebug("Collect rejected events, possible corruption.");
                            this.events.Clear();
                        }
                        else
                        {
                            DeltaDNA.Logger.LogWarning("Event upload failed - try again later.");
                        }
                    };

                    yield return StartCoroutine(PostMetaGameEvents(events.ToArray(), postCb));
                    
                }
            }
            finally
            {
                this.IsUploading = true;
            }
        }


        
        // Post Events to Publisher MetaGame
        private IEnumerator PostMetaGameEvents(string[] events, Action<bool, int> resultCallback)
        {
            string bulkEvent = "{\"eventList\":[" + String.Join(",", events) + "]}";
            string url = COLLECT_REST_API_URL + @"\" + ENVIRONMENT_KEY; 


            int attempts = 0;
            bool succeeded = false;
            int status = 0;

            Action<int, string, string> completionHandler = (statusCode, data, error) => {
                if (statusCode > 0 && statusCode < 400)
                {
                    succeeded = true;
                }
                else
                {
                    DeltaDNA.Logger.LogDebug("Error posting events: " + error + " " + data);
                }
                status = statusCode;
            };

            HttpRequest request = new HttpRequest(url);
            request.HTTPMethod = HttpRequest.HTTPMethodType.POST;
            request.HTTPBody = bulkEvent;
            request.setHeader("Content-Type", "application/json");

            do
            {
                yield return StartCoroutine(DeltaDNA.Network.SendRequest(request, completionHandler));

                if (succeeded || ++attempts < DDNA.Instance.Settings.HttpRequestMaxRetries) break;

                yield return new WaitForSeconds(DDNA.Instance.Settings.HttpRequestRetryDelaySeconds);
            } while (attempts < DDNA.Instance.Settings.HttpRequestMaxRetries);

            resultCallback(succeeded, status);
        }

        

        // Format EventTimestamp
        private static string GetCurrentTimestamp()
        {
            DateTime? dt = DateTime.UtcNow;
            if (dt.HasValue)
            {
                String ts = dt.Value.ToString(Settings.EVENT_TIMESTAMP_FORMAT, CultureInfo.InvariantCulture);
                // Fix for millisecond timestamp format bug seen on Android.
                if (ts.EndsWith(".1000"))
                {
                    ts = ts.Replace(".1000", ".999");
                }
                return ts;
            }
            return null;    // Collect will insert a timestamp for us.
        }


        ////////////////////////////////////////////////////////////////////
        // Publisher MetaGame Engage Campaign Check  
        ////////////////////////////////////////////////////////////////////
        public void MetaGameRequestEngagement(Engagement engagement, Action<Engagement> onCompleted, Action<Exception> onError)
        {
            DeltaDNA.Logger.LogDebug("MetaGame Campaign Check Click");
            if (String.IsNullOrEmpty(ENGAGE_REST_API_URL))
            {
                throw new Exception("Engage URL not configured.");
            }

            try
            {
                var dict = engagement.AsDictionary();

                var request = new EngageRequest(dict["decisionPoint"] as string);
                request.Flavour = dict["flavour"] as string;
                request.Parameters = dict["parameters"] as Dictionary<string, object>;

                EngageResponse handler = (string response, int statusCode, string error) => {
                    engagement.Raw = response;
                    engagement.StatusCode = statusCode;
                    engagement.Error = error;

                    onCompleted(engagement);
                };

                StartCoroutine(MetaGameEngageRequest(this, request, handler));

            }
            catch (Exception ex)
            {
                DeltaDNA.Logger.LogWarning("Engagement request failed: " + ex.Message);
            }
        }

        internal static IEnumerator MetaGameEngageRequest(MonoBehaviour caller, EngageRequest request, EngageResponse response)
        {
            string requestJSON = request.ToJSON();
            string url = String.Format("{0}/{1}", ENGAGE_REST_API_URL, ENVIRONMENT_KEY);

            HttpRequest httpRequest = new HttpRequest(url);
            httpRequest.HTTPMethod = HttpRequest.HTTPMethodType.POST;
            httpRequest.HTTPBody = requestJSON;
            httpRequest.TimeoutSeconds = DDNA.Instance.Settings.HttpRequestEngageTimeoutSeconds;
            httpRequest.setHeader("Content-Type", "application/json");

            System.Action<int, string, string> httpHandler = (statusCode, data, error) => {

                string engagementKey = "DDSDK_ENGAGEMENT_" + request.DecisionPoint + "_" + request.Flavour;
                if (error == null && statusCode >= 200 && statusCode < 300)
                {
                    try
                    {
                        PlayerPrefs.SetString(engagementKey, data);
                    }
                    catch (Exception exception)
                    {
                        DeltaDNA.Logger.LogWarning("Unable to cache engagement: " + exception.Message);
                    }
                }
                else
                {
                    DeltaDNA.Logger.LogDebug("Engagement failed with " + statusCode + " " + error);
                    if (PlayerPrefs.HasKey(engagementKey))
                    {
                        DeltaDNA.Logger.LogDebug("Using cached response");
                        data = "{\"isCachedResponse\":true," + PlayerPrefs.GetString(engagementKey).Substring(1);
                    }
                    else
                    {
                        data = "{}";
                    }
                }

                response(data, statusCode, error);
            };

            yield return caller.StartCoroutine(DeltaDNA.Network.SendRequest(httpRequest, httpHandler));
        }

    }
}