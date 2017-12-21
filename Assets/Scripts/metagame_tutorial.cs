//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using DeltaDNA;

namespace metagameTutorial
{
    public class metagame_tutorial : MonoBehaviour
    {
        public const string CLIENT_VERSION = "0.0.01";
        metagame_sender MetaGameSender; 


        // Use this for initialization
        void Start()
        {
            ///////////////////////////////////////////////////////////////////
            // Start the deltaDNA SDK as normal and use it to capture gameplay 
            // events to a deltaDNA game instance specifically for this game

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

            // Record MetaGame startup events
            MetaGameSender.StartMetaGame();
            /////////////////////////////////////////////////////////////////////////
        }



    }
}