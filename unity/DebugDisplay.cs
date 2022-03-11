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
    public string stack = "";
    public Text display;
     
    private void Update() {
        Debug.Log("time:" + Time.time);
        Debug.Log(gameObject.name);
    }
    void OnEnable() {
        Application.logMessageReceivedThreaded += HandleLog;
    }
    void OnDisable(){
        Application.logMessageReceivedThreaded  -= HandleLog;
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

            debugLogs.Add("Stack : ", stackTrace);
        }

        string displayText = "";
        foreach (KeyValuePair<string, string> log in debugLogs) {
            if(log.Value == "") 
                displayText += log.Key + "\n";
            else
                displayText += log.Key + ": " + log.Value + "\n";
        }
        display.text = displayText;
    }
}
