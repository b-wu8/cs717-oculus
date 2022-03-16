/**
 *  This is a script for printing debug log onto an Unity UI text. 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugDisplay : MonoBehaviour
{
    Dictionary<string, string> debugLogs = new Dictionary<string, string>();
    string displayText = "";
    public string stack = "";
    public Text display;
     
    void OnEnable() {
        Application.logMessageReceivedThreaded += HandleLog;
    }
    void OnDisable(){
        Application.logMessageReceivedThreaded  -= HandleLog;
    }

    public void FixedUpdate()
    {
        display.text = displayText;
    }

    void HandleLog(string logString, string stackTrace, LogType type){
        if(type == LogType.Log) {
            string[] splitString = logString.Split(char.Parse(":"));
            string debugKey = splitString[0];
            string debugValue = splitString.Length > 1 ? splitString[1] : "";

            if(debugLogs.ContainsKey(debugKey)){
                debugLogs[debugKey] = debugValue;
            } else {
                debugLogs.Add(debugKey, debugValue);
            }
        }

        if(type == LogType.Exception)
        {
            string[] splitString = logString.Split(char.Parse(":"));
            string debugKey = splitString[0];
            string debugValue = splitString.Length > 1 ? splitString[1] : "";

            if (debugLogs.ContainsKey(debugKey))
            {
                debugLogs[debugKey] = debugValue;
            }
            else
            {
                debugLogs.Add(debugKey, debugValue);
            }


            if (debugLogs.ContainsKey("Stack"))
            {
                debugLogs[debugKey] = stackTrace;
            }
            else
            {
                debugLogs.Add("Stack", stackTrace);
            }
        }

        displayText = "";

        Dictionary<string, string> debug_log_copy = new Dictionary<string, string>(debugLogs);
        foreach (KeyValuePair<string, string> log in debug_log_copy) {
            if(log.Value == "") 
                displayText += log.Key + "\n";
            else
                displayText += log.Key + ": " + log.Value + "\n";
        }
        
    }
}
