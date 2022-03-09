using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class MoveBallButton : MonoBehaviour
{
    public Config config;
    private IPEndPoint endpoint;
    private byte[] data;
    public ServerEventHandler se_handler;

    // Start is called before the first frame update
    void Start()
    {
        data = Encoding.UTF8.GetBytes("OCULUS " + config.player_name + " " + config.lobby);
        endpoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
    }

    void OnGUI()
    {
        Rect rectObj = new Rect(200, 380, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "", style);
        if (GUI.Button(new Rect(100, 375, 80, 20), "Move Ball"))
        {
            se_handler.client.Send(data, data.Length, endpoint);
        }
    }
}
