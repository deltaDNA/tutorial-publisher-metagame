using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DDNAInboxTutorial
{
    public class Console : MonoBehaviour
    {


        public UnityEngine.UI.Text lblConsole;  // displays debug info on deletaDNA SDK and SmartAds


        // UI Debug Console will be used to debug what SmartAds and Engage are doing
        public int numConsoleLines = 12;        // the number of lines that the debug console will be truncated to
        private List<string> console = new List<string>();


        // Use this when the Script is loaded
        void Awake()
        {
            // This will allow us to display Unity Debug.Log in the UI 
            Application.logMessageReceived += PrintToConsole;
        }
        // Use this for initialization
        void Start()
        {
            Debug.Log("Application Started");
        }

        // Update is called once per frame
        void Update()
        {

        }

        // Display Debug Messages in UI
        private void PrintToConsole(string logString, string stackTrace, LogType type)
        {

            console.Add(string.Format("{0}::{1}\n", System.DateTime.Now.ToString("h:mm:ss tt"), logString));
            if (console.Count > numConsoleLines)
            {
                console.RemoveRange(0, console.Count - numConsoleLines);
            }
            lblConsole.text = "";
            console.ForEach(i => lblConsole.text += i);
        }
    }
}