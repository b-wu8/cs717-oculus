/**
 * Where we define the configuration for the project
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Config : MonoBehaviour
{
    public string remote_ip_address = "NOT_SPECIFIED";
    public int remote_port;
    public int local_port;
    public string player_name;
    public string lobby = "DEFAULT";
    public int sleep_ms = 50;
    // public int send_thread_sleep_time = 100;
    // public int receive_thread_sleep_time = 100;
}
