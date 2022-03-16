using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.XR;
using Google.Protobuf;

public class OculusClient : MonoBehaviour
{
    public Config config;
    public PlayerView view;
    public DeviceInfoWatcher device_watcher;
    private string last_packet;
    private int discrete_timeout_ms;
    private Dictionary<string, bool> current_discrete_state;
    public UdpClient receive_client, heartbeat_client, continuous_input_client, discrete_input_client;
    private Thread receive_thread, heartbeat_thread, continuous_input_thread, discrete_input_thread;
    private DateTime ping_start_time;
    public IPEndPoint server_endpoint;
    private TouchScreenKeyboard overlayKeyboard;
    public static string inputText = "";

    public void Start()
    {
        receive_client = new UdpClient();
        ADS.Request init_request = new ADS.Request { Type = ADS.Request.Types.Type.Syn, Player = new ADS.Player { PlayerName = config.player_name }, LobbyName = config.lobby };
        byte[] temp = init_request.ToByteArray();
        server_endpoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        receive_client.Send(temp, temp.Length, server_endpoint);
        receive_client.Send(temp, temp.Length, server_endpoint);
        receive_thread = new Thread(new ThreadStart(ReceiveData));
        receive_thread.IsBackground = true;
        receive_thread.Start();

        heartbeat_client = new UdpClient();
        heartbeat_thread = new Thread(new ThreadStart(Heartbeat));
        heartbeat_thread.IsBackground = true;
        heartbeat_thread.Start();

        continuous_input_client = new UdpClient();
        continuous_input_thread = new Thread(new ThreadStart(SendContinuousData));
        continuous_input_thread.IsBackground = true;
        continuous_input_thread.Start();

        discrete_timeout_ms = 10;
        current_discrete_state = new Dictionary<string, bool>();
        current_discrete_state.Add("LeftPrimaryButton", false);
        current_discrete_state.Add("RightPrimaryButton", false);
        current_discrete_state.Add("LeftSecondaryButton", false);
        current_discrete_state.Add("RightSecondaryButton", false);
        discrete_input_client = new UdpClient();
        discrete_input_thread = new Thread(new ThreadStart(SendDiscreteData));
        discrete_input_thread.IsBackground = true;
        discrete_input_thread.Start();

        overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        if (overlayKeyboard != null)
            inputText = overlayKeyboard.text;
    }

    void OnGUI()
    {
        Rect rectObj = new Rect(40, 10, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "Last Packet: \n" + last_packet, style);
    }

    void OnApplicationQuit()
    {   
        receive_thread.Abort();

        continuous_input_thread.Abort();
        discrete_input_thread.Abort();

        byte[] data = new ADS.Request{ Type = ADS.Request.Types.Type.Fin, Player = new ADS.Player { PlayerName = config.player_name }, LobbyName = config.lobby}.ToByteArray();
        heartbeat_client.Send(data, data.Length, server_endpoint);
        heartbeat_thread.Abort();
    }

    private void HandleDataMessage(ADS.Response response) {
        view.num_players = response.Players.Count;

        view.sphere_loc = new Vector3(
            response.Sphere.Position.X,
            response.Sphere.Position.Y,
            response.Sphere.Position.Z);

        view.plane_loc = new Vector3(
            response.Plane.Position.X,
            response.Plane.Position.Y,
            response.Plane.Position.Z);

        for (int i = 0; i < response.Players.Count; i++) 
        {
            string avatar_name = response.Players[i].PlayerName;
            if (view.avatars.ContainsKey(avatar_name)) {
                view.avatars[avatar_name].update(response.Players[i]);
            } else {
                view.avatars.Add(avatar_name, new Avatar(config.player_name));
                view.avatars[avatar_name].update(response.Players[i]);
            } 
        }
    }

    private void HandleLeaveMessage(ADS.Response response) {
        int num_players = response.Players.Count;

        for(int i = 0; i < num_players; i++)
        {
            string player_name = response.Players[i].PlayerName;
            if (view.avatars.ContainsKey(player_name))
            {
                view.avatars[player_name].to_be_destroyed = true;
                Debug.Log("Marked " + player_name + " for destruction");
            }
        }
    }

    private void HandleHeartbeatMessage() {
        DateTime ping_end_time = DateTime.Now;
        TimeSpan ping_time = ping_end_time.Subtract(ping_start_time);
        Debug.Log("Ping time: " + ping_time.Milliseconds + " ms");
    }

    private void ReceiveData()
    {
        while (true)
        {
            try 
            {
                IPEndPoint from_endpoint = new IPEndPoint(IPAddress.Any, 0);

                byte[] received_data_bytes = receive_client.Receive(ref from_endpoint);

                ADS.Response response = new ADS.Response();

                response = ADS.Response.Parser.ParseFrom(received_data_bytes);

                string message = Encoding.UTF8.GetString(received_data_bytes);
                last_packet = message;

                if (ADS.Response.Types.Type.Data.Equals(response.GetType()))
                    HandleDataMessage(response);
                else if (ADS.Response.Types.Type.Lobby.Equals(response.GetType()))
                    HandleLeaveMessage(response);
                else if (ADS.Response.Types.Type.Heartbeat.Equals(response.GetType()))
                    HandleHeartbeatMessage();
                else 
                    Debug.Log("Error - malformed message: " + message);

            } 
            catch (Exception e)
            {
                Debug.Log("Exception in ReceiveData(): " + e);
            }
        }
    }

    private void Heartbeat() {
        while (true) {
            System.Threading.Thread.Sleep(config.heartbeat_sleep_ms);
            try 
            {
                ping_start_time = DateTime.Now;

                //protobuf serialization
                Vector3 head_pos = device_watcher.GetHeadPosition();
                Quaternion head_rot = device_watcher.GetHeadRotation();
                Vector3 right_controller_pos = device_watcher.GetRightControllerPosition();
                Quaternion right_controller_rot = device_watcher.GetRightControllerRotation();
                Vector3 left_controller_pos = device_watcher.GetLeftControllerPosition();
                Quaternion left_controller_rot = device_watcher.GetLeftControllerRotation();
                Vector2 right_joystick = device_watcher.GetRightJoystickVec2();
                Vector2 left_joystick = device_watcher.GetLeftJoystickVec2();
                ADS.Request request = new ADS.Request
                {
                    LobbyName = config.lobby,
                    Type = ADS.Request.Types.Type.Heartbeat,
                    Player = new ADS.Player
                    {
                        PlayerName = config.player_name,
                        Headset = new ADS.DeviceInfo {
                            Position = new ADS.Position
                            {
                                X = head_pos.x,
                                Y = head_pos.y,
                                Z = head_pos.z
                            },
                            Rotation = new ADS.Rotation
                            {
                                X = head_rot.x,
                                Y = head_rot.y,
                                Z = head_rot.z,
                                W = head_rot.w
                            }
                        },
                        RightController = new ADS.DeviceInfo
                        {
                            Position = new ADS.Position
                            {
                                X = left_controller_pos.x,
                                Y = left_controller_pos.y,
                                Z = left_controller_pos.z
                            },
                            Rotation = new ADS.Rotation
                            {
                                X = left_controller_rot.x,
                                Y = left_controller_rot.y,
                                Z = left_controller_rot.z,
                                W = left_controller_rot.w
                            }
                        },
                        RightJoystick = new ADS.Vector2
                        {
                            X = right_joystick.x,
                            Y = right_joystick.y
                        },
                        LeftJoystick = new ADS.Vector2
                        {
                            X = left_joystick.x,
                            Y = left_joystick.y
                        }
                    }
                };
                byte[] data = request.ToByteArray();
                heartbeat_client.Send(data, data.Length, server_endpoint);
            }
            catch (Exception e)
            {
                Debug.Log("Exception in Heartbeat(): " + e);
            }
        }
    }

    private void SendContinuousData()
    {
        while (true)
        {
            System.Threading.Thread.Sleep(config.controller_sleep_ms);
            try {
                //protobuf serialization
                Vector3 head_pos = device_watcher.GetHeadPosition();
                Quaternion head_rot = device_watcher.GetHeadRotation();
                Vector3 right_controller_pos = device_watcher.GetRightControllerPosition();
                Quaternion right_controller_rot = device_watcher.GetRightControllerRotation();
                Vector3 left_controller_pos = device_watcher.GetLeftControllerPosition();
                Quaternion left_controller_rot = device_watcher.GetLeftControllerRotation();
                Vector2 right_joystick = device_watcher.GetRightJoystickVec2();
                Vector2 left_joystick = device_watcher.GetLeftJoystickVec2();
                ADS.Request request = new ADS.Request
                {
                    LobbyName = config.lobby,
                    Type = ADS.Request.Types.Type.Heartbeat,
                    Player = new ADS.Player
                    {
                        PlayerName = config.player_name,
                        Headset = new ADS.DeviceInfo
                        {
                            Position = new ADS.Position
                            {
                                X = head_pos.x,
                                Y = head_pos.y,
                                Z = head_pos.z
                            },
                            Rotation = new ADS.Rotation
                            {
                                X = head_rot.x,
                                Y = head_rot.y,
                                Z = head_rot.z,
                                W = head_rot.w
                            }
                        },
                        RightController = new ADS.DeviceInfo
                        {
                            Position = new ADS.Position
                            {
                                X = left_controller_pos.x,
                                Y = left_controller_pos.y,
                                Z = left_controller_pos.z
                            },
                            Rotation = new ADS.Rotation
                            {
                                X = left_controller_rot.x,
                                Y = left_controller_rot.y,
                                Z = left_controller_rot.z,
                                W = left_controller_rot.w
                            }
                        },
                        RightJoystick = new ADS.Vector2
                        {
                            X = right_joystick.x,
                            Y = right_joystick.y
                        },
                        LeftJoystick = new ADS.Vector2
                        {
                            X = left_joystick.x,
                            Y = left_joystick.y
                        }
                    }
                };
                byte[] data = request.ToByteArray();
                continuous_input_client.Send(data, data.Length, server_endpoint);
            }
            catch (Exception e)
            {
                Debug.Log("Exception in SendControllerData(): " + e);
            }
        }
    }

    private void SendDiscreteData()
    {
        while (true)
        {
            System.Threading.Thread.Sleep(discrete_timeout_ms);
            try {
                //if left controller primary button pushed, send to send right away
                bool new_state = device_watcher.GetLeftControllerPrimaryButtonPushed();
                if (current_discrete_state["LeftPrimaryButton"] != new_state)
                {
                    current_discrete_state["LeftPrimaryButton"] = new_state;
                    ADS.Request request = new ADS.Request { Type = ADS.Request.Types.Type.Discrete };
                    if (new_state) // if button pushed
                    {
                        request.Message = "left hand primary button pushed :)";
                    }
                    else
                    {
                        request.Message = "left hand primary released";
                    }
                    byte[] bytes = request.ToByteArray();
                    discrete_input_client.Send(bytes, bytes.Length, server_endpoint);
                }

                //if left controller secondary button pushed, send to send right away
                new_state = device_watcher.GetLeftControllerSecondaryButtonPushed();
                if (current_discrete_state["LeftSecondaryButton"] != new_state)
                {
                    // trigger event
                    current_discrete_state["LeftSecondaryButton"] = new_state;
                    ADS.Request request = new ADS.Request { Type = ADS.Request.Types.Type.Discrete };
                    if (new_state) // if button pushed
                    {
                        request.Message = "left hand secondary button pushed :)";
                    }
                    else
                    {
                        request.Message = "left hand secondary button released :)";
                    }
                    byte[] bytes = request.ToByteArray();
                    discrete_input_client.Send(bytes, bytes.Length, server_endpoint);
                }

                new_state = device_watcher.GetRightControllerPrimaryButtonPushed();
                if (current_discrete_state["RightPrimaryButton"] != new_state)
                {
                    // trigger event
                    current_discrete_state["RightPrimaryButton"] = new_state;
                    ADS.Request request = new ADS.Request { Type = ADS.Request.Types.Type.Discrete };
                    if (new_state) // if button pushed
                    {
                        request.Message = "right hand primary button pushed :)";
                    }
                    else
                    {
                        request.Message = "right hand primary button released :)";
                    }
                    byte[] bytes = request.ToByteArray();
                    discrete_input_client.Send(bytes, bytes.Length, server_endpoint);
                }

                new_state = device_watcher.GetRightControllerSecondaryButtonPushed();
                if (current_discrete_state["RightSecondaryButton"] != new_state)
                {
                    // trigger event
                    current_discrete_state["RightSecondaryButton"] = new_state;
                    ADS.Request request = new ADS.Request { Type = ADS.Request.Types.Type.Discrete };
                    if (new_state) // if button pushed
                    {
                        request.Message = "right hand secondary button pushed :)";
                    }
                    else
                    {
                        request.Message = "right hand secondary button released :)";
                    }
                    byte[] bytes = request.ToByteArray();
                    discrete_input_client.Send(bytes, bytes.Length, server_endpoint);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Exception in SendControllerData(): " + e);
            }
        }
    }
}
