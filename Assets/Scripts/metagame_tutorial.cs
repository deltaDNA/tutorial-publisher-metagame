using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeltaDNA; 

public class metagame_tutorial : MonoBehaviour {

	// Use this for initialization
	void Start () {
        // Enter additional configuration here
        DDNA.Instance.ClientVersion = "1.0.0";
        DDNA.Instance.SetLoggingLevel(DeltaDNA.Logger.Level.DEBUG);
        DDNA.Instance.Settings.DebugMode = true;

        // Launch the SDK
        DDNA.Instance.StartSDK(
            "49994906769133941928339970015138",
            "https://collect12660pblsh.deltadna.net/collect/api",
            "https://engage12660pblsh.deltadna.net"
        );
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
