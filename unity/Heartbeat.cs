using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Heartbeat : MonoBehaviour
{
    public Config config;
    public int heartbeat_ms = 3000;  // 3 sec
    private UdpClient client;
    private Thread thread;  
    private IPEndPoint server_endpoint;   

    void Start()
    {
        client = new UdpClient();
        server_endpoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);

        thread = new Thread(new ThreadStart(SendKeepAlive));
        thread.IsBackground = true;
        thread.Start();
    }

    void OnApplicationQuit()
    {
        byte[] data = Encoding.UTF8.GetBytes(Constants.FIN + " " + config.player_name + " " + config.lobby);
        client.Send(data, data.Length, server_endpoint);
        if (thread != null)
            thread.Abort();
    }

    private void SendKeepAlive() {
        while (true) {
            System.Threading.Thread.Sleep(heartbeat_ms);
            byte[] data = Encoding.UTF8.GetBytes(Constants.HEARTBEAT + " " + config.player_name + " " + config.lobby);
            client.Send(data, data.Length, server_endpoint);
        }
    }
}
