using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using DeltaDNA;

namespace metagameTutorial
{
    public class metagame_tutorial : MonoBehaviour
    {
        public const string CLIENT_VERSION = "0.0.01";
        public UnityEngine.UI.Text txtLevel;
        public UnityEngine.UI.Text txtCoins;

        public const string GAME_ID = "1";
        public const string GAME_NAME = "Simple Game";
        public const string GAME_GENRE = "Shootem Up";

        private int userLevel =  1;
        private int userCoins =  0;       

        // Use this for initialization
        void Start()
        {
            UpdatePlayerHUD(); 

            ///////////////////////////////////////////////////////////////////
            // Start the deltaDNA SDK as normal and use it to capture gameplay 
            // events to a deltaDNA game instance specifically for this game

            // For demo purposes... clear all data and treat each run as if it were a fresh new install.
            //DDNA.Instance.ClearPersistentData(); 

            // Enter additional configuration here
            DDNA.Instance.ClientVersion = CLIENT_VERSION;
            DDNA.Instance.SetLoggingLevel(DeltaDNA.Logger.Level.DEBUG);
            DDNA.Instance.Settings.DebugMode = true;

            // Launch the SDK
            // Record normal event to the deltaDNA demo game dev environment.
            DDNA.Instance.StartSDK(
                "56919948607282167963952652014071",
                "https://collect2674dltcr.deltadna.net/collect/api",
                "https://engage2674dltcr.deltadna.net");
            //////////////////////////////////////////////////////////////////




            ////////////////////////////////////////////////////////////////////////
            // Send additional events to the Publisher MetaGame using the REST API
            // At the start of the game and other key points such as IAP purchase and gameEnd
            metagame_sender MetaGameSender = gameObject.GetComponent<metagame_sender>();
            MetaGameSender.StartMetaGame();
            /////////////////////////////////////////////////////////////////////////
        }
        
        

        ///////////////////////////////////////////////////////////////////
        // Handle Button Clicks to simulate campaign check decision points.
        public void Button_PrimaryGameCampaignCheck_Clicked()
        {
            PrimaryGameCampaignCheck("gameStarted");
        }
        public void Button_MetaGameCampaignCheck_Clicked()
        {
            MetaGameCampaignCheck("gameStarted");
        }
        public void Button_LevelUp_Clicked()
        {
            this.userLevel++;

            // Send levelUp event to the primary game environment
            GameEvent levelUp = new GameEvent("levelUp")
                .AddParam("userLevel", this.userLevel)
                .AddParam("levelUpName", string.Format("Level {0}",this.userLevel));
            DDNA.Instance.RecordEvent(levelUp);
            DDNA.Instance.Upload();

            UpdatePlayerHUD();
        }

        public void Button_Reset_Clicked()
        {
            ResetPlayer();
        }
        ///////////////////////////////////////////////////////////////////

        private void UpdatePlayerHUD()
        {
            this.txtLevel.text = string.Format("Level : {0}", userLevel);
            this.txtCoins.text = string.Format("Coins : {0}", userCoins);
        }
        private void ResetPlayer()
        {
            // For demo purposes... clear all data so we can see how campaigns react for a new player.
            if(DDNA.Instance.isActiveAndEnabled)
            {
                DDNA.Instance.StopSDK();
            }
            DDNA.Instance.ClearPersistentData();
            this.userLevel = 1;
            this.userCoins = 0;
           
            this.Start();
        }

        ///////////////////////////////////////////////////////////////////////////////
        // Primary Game Campaign Check
        private void PrimaryGameCampaignCheck(string decisionPoint)
        {
            Debug.Log("Primary Campaign Check");
            var engagement = new Engagement(decisionPoint)
                 .AddParam("userLevel", this.userLevel);

            // Make request to Engage In-Game campaign system to see 
            // if there are any active campaigns for this player at this time
            DDNA.Instance.RequestEngagement(engagement, (response) => {

                ReactToCampaignResponse(response);
            }, (exception) => {
                Debug.Log("Engage reported an error: " + exception.Message);
            });
        }
        ///////////////////////////////////////////////////////////////////////////////
        // MetaGame Campaign Check
        private void MetaGameCampaignCheck(string decisionPoint)
        {
            Debug.Log("MetaGame Campaign Check");
            var engagement = new Engagement(decisionPoint)
                .AddParam("userLevel", this.userLevel)
                .AddParam("gameID", GAME_ID)
                .AddParam("gameName", GAME_NAME)
                .AddParam("gameGenre", GAME_GENRE);
            metagame_sender MetaGameSender = gameObject.GetComponent<metagame_sender>();
            // Make request to Engage In-Game campaign system to see 
            // if there are any active campaigns for this player at this time
            MetaGameSender.MetaGameRequestEngagement(engagement, (response) => {

                ReactToCampaignResponse(response);
            }, (exception) => {
                Debug.Log("Engage reported an error: " + exception.Message);
            });
        }
        ///////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////        
        private void ReactToCampaignResponse(Engagement response)
        {

            ImageMessage imageMessage = ImageMessage.Create(response);

            // Handle response
            // Check we got an engagement with a valid image message.
            if (imageMessage != null)
            {
                Debug.Log("Engage returned a valid image message.");

                // This example will show the image as soon as the background
                // and button images have been downloaded.
                imageMessage.OnDidReceiveResources += () => {
                    Debug.Log("Image Message loaded resources.");
                    imageMessage.Show();
                };

                // Add a handler for the 'dismiss' action.
                imageMessage.OnDismiss += (ImageMessage.EventArgs obj) => {
                    Debug.Log("Image Message dismissed by " + obj.ID);
                };

                // Add a handler for the 'action' action.
                imageMessage.OnAction += (ImageMessage.EventArgs obj) => {
                    Debug.Log("Image Message actioned by " + obj.ID + " with command " + obj.ActionValue);

                    // Look for specific values in the Button Action and act accordingly
                    switch (obj.ActionValue.ToUpper())
                    {
                        case "GIFT":
                            AwardGiftFromEngage(response);
                            break;
                        default:
                            break;
                    }
                };

                // Download the image message resources.
                imageMessage.FetchResources();
            }
            else
            {
                Debug.Log("Engage didn't return an image message.");
            }
        }


        ///////////////////////////////////////////////////////////////////////////               
        // Award a gift to the player based on the response from an Engage campaign
        private void AwardGiftFromEngage(Engagement response)
        {
            object parametersObject;
            
            if (response.JSON != null && response.JSON.TryGetValue("parameters", out parametersObject))
            {                
                
                Dictionary<string, object> parameters = parametersObject as Dictionary<string, object>;
                   
                if (parameters.ContainsKey("virtualCurrencyName") && parameters.ContainsKey("virtualCurrencyAmount"))
                {
                    // Looking for virtualCurrencyName == coins and virtualCurrencyAMount > 0
                    if (    parameters["virtualCurrencyName"]   != null &&
                            parameters["virtualCurrencyAmount"] != null &&
                            parameters["virtualCurrencyName"].ToString().ToLower() == "coins" && 
                            System.Convert.ToInt32(parameters["virtualCurrencyAmount"]) > 0 ) {

                        int coins = System.Convert.ToInt32(parameters["virtualCurrencyAmount"]);
                        this.userCoins += coins; 
                        DeltaDNA.Logger.LogDebug("Gifting player " + coins + " coins");
                    }
                }

                UpdatePlayerHUD();                
            }
        }
   
    }
}