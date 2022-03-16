/**
 *  This is a script for printing debug log onto an Unity UI text. 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugDisplay : MonoBehaviour
{
    Dictionary<string, string> debug_logs = new Dictionary<string, string>();
    string display_text = "";
    public string stack = "";
    public Text display;
     
    void OnEnable() {
        Application.logMessageReceivedThreaded += HandleLog;
        display.fontSize = 18;
    }
    void OnDisable(){
        Application.logMessageReceivedThreaded  -= HandleLog;
    }

    public void FixedUpdate()
    {
        display.text = display_text;
    }

    void HandleLog(string logString, string stack_trace, LogType type){
        if(type == LogType.Log) {
            string[] splitString = logString.Split(char.Parse(":"));
            string debug_key = splitString[0];
            string debug_value = splitString.Length > 1 ? splitString[1] : "";

            if(debug_logs.ContainsKey(debug_key))
                debug_logs[debug_key] = debug_value;
            else
                debug_logs.Add(debug_key, debug_value);
        }

        if(type == LogType.Exception)
        {
            string[] splitString = logString.Split(char.Parse(":"));
            string debug_key = splitString[0];
            string debug_value = splitString.Length > 1 ? splitString[1] : "";

            if (debug_logs.ContainsKey(debug_key))
                debug_logs[debug_key] = debug_value;
            else
                debug_logs.Add(debug_key, debug_value);


            if (debug_logs.ContainsKey("Stack"))
                debug_logs[debug_key] = stack_trace;
            else
                debug_logs.Add("Stack", stack_trace);
        }

        display_text = "";
        Dictionary<string, string> debug_log_copy = new Dictionary<string, string>(debug_logs);
        foreach (KeyValuePair<string, string> log in debug_log_copy) {
            if(log.Value == "") 
                display_text += log.Key + "\n";
            else
                display_text += log.Key + ": " + log.Value + "\n";
        }
        
    }
}
