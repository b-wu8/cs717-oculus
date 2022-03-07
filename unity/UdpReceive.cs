//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;


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

[StructLayout(LayoutKind.Sequential, Pack=1)]
public unsafe struct RainPacket {
    public int flag;
    public double x;
    public double y;
    public double z;
    public fixed byte mess[256];
};

public class UdpReceive : MonoBehaviour
{

    // receiving Thread
    Thread receiveThread;

    // udpclient object
    UdpClient client;

    GameObject sphere;

    // public
    // public string IP = "127.0.0.1"; default local
    public int port; // define > init

    // infos
    public string lastReceivedUDPPacket = "";
    public string allReceivedUDPPackets = ""; // clean up this from time to time!
    public float x, y, z;


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
    // start from unity3d
    public void Start()
    {
        init();
    }

    public void Update()
    {
        sphere.transform.position = new Vector3(x, y, z);
    }


    // OnGUI
    void OnGUI()
    {
        Rect rectObj = new Rect(40, 10, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "# UDPReceive\n127.0.0.1 " + port + " #\n"
                    + "shell> nc -u 127.0.0.1 : " + port + " \n"
                    + "\nLast Packet: \n" + lastReceivedUDPPacket
                    + "\n\nAll Messages: \n" + allReceivedUDPPackets
                , style);
    }

    // init
    private void init()
    {
        // Endpunkt definieren, von dem die Nachrichten gesendet werden.
        Debug.Log("UDPSend.init()");

        // define port
        port = 8051;
        //port = 20003;

        // status
        Debug.Log("Sending to 127.0.0.1 : " + port);
        Debug.Log("Test-Sending to this Port: nc -u 127.0.0.1  " + port + "");

        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = new Vector3(1f, 1.5f, 1f);
        var sphere_renderer = sphere.GetComponent<Renderer>();
        sphere_renderer.material.SetColor("_Color", Color.red);

        // ----------------------------
        // Abhören
        // ----------------------------
        // Lokalen Endpunkt definieren (wo Nachrichten empfangen werden).
        // Einen neuen Thread für den Empfang eingehender Nachrichten erstellen.
        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

    }

    // receive thread
    private void ReceiveData()
    {

        client = new UdpClient(port);

        byte[] temp = Encoding.UTF8.GetBytes("Starting...");
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("128.220.221.21"), 4577);
        client.Send(temp, temp.Length, remoteEndPoint);
        client.Send(temp, temp.Length, remoteEndPoint);

        while (true)
        {
            try
            {
                // Bytes empfangen.
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                string[] pieces = text.Split(' ');
                Debug.Log(">> " + text);
                lastReceivedUDPPacket = text;
                allReceivedUDPPackets = allReceivedUDPPackets + text;
                x = float.Parse(pieces[0], CultureInfo.InvariantCulture.NumberFormat);
                y = float.Parse(pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                z = float.Parse(pieces[2], CultureInfo.InvariantCulture.NumberFormat);

                //x = -1f;
                //y = 2f;
                //z = -4f;
                // sphere.transform.position = new Vector3(-1f, 2f, -4f);

                /*
                RainPacket packet = new RainPacket();
                int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(RainPacket));
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(data, 0, ptr, size);
                packet = (RainPacket)Marshal.PtrToStructure(ptr, packet.GetType());
                //RainPacket packet = (RainPacket)data;
                //sphere.transform.position = new Vector3((float) packet.x, (float) packet.y, (float) packet.z);
                */
                //x = (float)packet.x;
                //y = (float)packet.y;
                //z = (float)packet.z;


                /*
                // Bytes mit der UTF8-Kodierung in das Textformat kodieren.
                string text = Encoding.UTF8.GetString((byte[])packet.mess);
                // Den abgerufenen Text anzeigen.
                Debug.Log(">> " + text);
                // latest UDPpacket
                lastReceivedUDPPacket = text;
                // ....
                allReceivedUDPPackets = allReceivedUDPPackets + text;
                */
            }
            catch (Exception err)
            {
                Debug.Log(err.ToString());
            }
        }
    }

    // getLatestUDPPacket
    // cleans up the rest
    public string getLatestUDPPacket()
    {
        allReceivedUDPPackets = "";
        return lastReceivedUDPPacket;
    }
}
