/*
 
    -----------------------
    UDP-Receive (send to)
    -----------------------
    // [url]http://msdn.microsoft.com/de-de/library/bb979228.aspx#ID0E3BAC[/url]
   
   
    // > receive
    // 127.0.0.1 : 8051
   
    // send
    // nc -u 127.0.0.1 8051
 
*/
using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;

/*
[StructLayout(LayoutKind.Sequential, Pack=1)]
public unsafe struct RainPacket {
    public int flag;
    public double x;
    public double y;
    public double z;
    public fixed byte mess[256];
};
*/

public struct PlayerInfo 
{
    public string name;
    public Headset headset;
    public LeftHandController left_hand;
    public RightHandController right_hand;
    
    public PlayerInfo(string name, Headset headset, LeftHandController left_hand, RightHandController right_hand)
    {
        this.name = name;
        this.headset = headset;
        this.left_hand = (LeftHandController) left_hand.Clone();
        this.right_hand = (RightHandController) right_hand.Clone();
    }
}

public class UdpReceive : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    GameObject sphere;
    public Config config;
    public string lastReceivedUDPPacket = "";
    public string allReceivedUDPPackets = ""; // clean up this from time to time!
    // public float x, y, z;
    List<PlayerInfo> all_player_info = new List<PlayerInfo>();

    // start from shell
    private static void Main()
    {
        UdpReceive receiveObj = new UdpReceive();
        receiveObj.init();

        string text = "";
        do
        {
            text = Console.ReadLine();
        }
        while (!text.Equals("exit"));
    }
    
    /*
     * Start from Unity Engine.
     */
    public void Start()
    {
        init();
    }

    /*
     * Update object position at every time tick.
     */
    public void Update()
    {
        sphere.transform.position = new Vector3(x, y, z);
    }

    void OnGUI()
    {
        Rect rectObj = new Rect(40, 10, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "# UDPReceive\n127.0.0.1 " + config.local_port + " #\n"
                    + "shell> nc -u 127.0.0.1 : " + config.local_port + " \n"
                    + "\nLast Packet: \n" + lastReceivedUDPPacket
                    + "\n\nAll Messages: \n" + allReceivedUDPPackets
                , style);
    }

    // init
    private void init()
    {
        Debug.Log("UDPSend.init()");

        // status
        Debug.Log("Sending to "+ config.remote_ip_address  +": " + config.remote_port);
        //Debug.Log("Test-Sending to this Port: nc -u 127.0.0.1  " + port + "");

        // Create red sphere object
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = new Vector3(1f, 1.5f, 1f);
        var sphere_renderer = sphere.GetComponent<Renderer>();
        sphere_renderer.material.SetColor("_Color", Color.red);

        // Run ReceiveData() in the background 
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    /*
     * Receive data from background thread.
     * Updates class variables that are used when Update() is called.  
     */
    private void ReceiveData()
    {

        client = new UdpClient(config.local_port); // port to listen on

        byte[] temp = Encoding.UTF8.GetBytes("OCULUS melody test");
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        client.Send(temp, temp.Length, remoteEndPoint); // first packet to override weird 0.0.0.0 IP address packet 
        client.Send(temp, temp.Length, remoteEndPoint);

        while (true)
        {
            try
            { 
                IPEndPoint remoteIP = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
                byte[] data = client.Receive(ref remoteIP); // blocks until data is received from somewhere 
                string text = Encoding.UTF8.GetString(data); // convert struct to string

                string[] lines = text.Split("/n"); // TODO: better encoding
                for (int i = 1; i < lines.Length; i++) // first line has lobby and # players
                {
                    string[] players = text[i].Split(' ');
                    if (all_player_info.Exists(x => x.name.Equals(players[0])))
                    {
                        continue; // player already added
                    } 
                    else
                    {
                        PlayerInfo new_player = new PlayerInfo(players[0], players[1], players[2], players[3]);
                    }
                } 

                Debug.Log(">> " + text);
                lastReceivedUDPPacket = text;
                allReceivedUDPPackets = allReceivedUDPPackets + text;

                /* Update object position asynchronously via Update thread*/
                /*
                x = float.Parse(pieces[0], CultureInfo.InvariantCulture.NumberFormat);
                y = float.Parse(pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                z = float.Parse(pieces[2], CultureInfo.InvariantCulture.NumberFormat);
                Debug.Log("pos rcvd:" + "X("+x+") Y("+y+") Z("+z+")");
                */
            }
            catch (Exception err)
            {
                Debug.Log(err.ToString());
            }
        }
    }


    /*
     * Get the most recent UDP packet.
     * clears previously received packets.
     */ 
    public string getLatestUDPPacket()
    {
        allReceivedUDPPackets = "";
        return lastReceivedUDPPacket;
    }
}
