using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading;


[System.Serializable]
public class DeviceInfoWatcher : MonoBehaviour
{
    public Config config;

    // input devices
    private InputDevice head_device;
    private InputDevice right_controller;
    private InputDevice left_controller;
    bool head_connected = false, right_connected = false, left_connected = false;

    void OnEnable()
    {
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        foreach(InputDevice device in allDevices)
        {
            //Debug.Log("Connected Device");
            //Debug.Log(device.name + device.characteristics);
            InputDevices_deviceConnected(device);
        }

        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;

        // Run in the background 
        //head_device_thread = new Thread(new ThreadStart(ReceiveData));
        //receiveThread.IsBackground = true;
        //receiveThread.Start();
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
        InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
    }

    private void InputDevices_deviceConnected(InputDevice device)
    {
        if ((device.characteristics & InputDeviceCharacteristics.HeadMounted) == InputDeviceCharacteristics.HeadMounted)
        {
            head_device = device;
            head_connected = true;
        } else
        {
            Debug.Log("Head not found");
        }

        if ((device.characteristics & InputDeviceCharacteristics.Left) == InputDeviceCharacteristics.Left)
        {
            left_controller = device;
            left_connected = true;
        }
        else
        {
            Debug.Log("left controller not found");
        }


        if ((device.characteristics & InputDeviceCharacteristics.Right) == InputDeviceCharacteristics.Right)
        {
            right_controller = device;
            right_connected = true;
        }
        else
        {
            Debug.Log("right controller not found");
        }
    }

    private void InputDevices_deviceDisconnected(InputDevice device)
    {
        Debug.Log("Device disconnected: " + device);
        if(device == head_device)
        {
            head_connected = false;
        }
        else if (device == right_controller)
        {
            right_connected = false;
        }
        else if (device == left_controller)
        {
            left_connected = false;
        }
    }

    public void SendPosToServer(Vector3 pos)
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        UdpClient client = new UdpClient();
        string text = pos.ToString("F2") + "," + GetTimestamp(DateTime.Now);
        byte[] data = Encoding.UTF8.GetBytes(text);
        client.Send(data, data.Length, remoteEndPoint);
        Debug.Log("Send pos to "+ config.remote_ip_address + ":" + text);
    }

    private static string GetTimestamp(DateTime value)
    {
        return value.ToString("yyyyMMddHHmmssffff");
    }

    public void Update()
    {
        GetLeftHandInfo();
        GetRightHandInfo();
        GetHeadsetInfo();
    }

    private LeftHandController GetLeftHandInfo()
    {
        // get left controller position
        Vector3 left_controller_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (left_controller.TryGetFeatureValue(CommonUsages.devicePosition, out left_controller_pos))
            Debug.Log("Left controller pos: " + left_controller_pos.ToString("F2"));

        // get left controller rotation
        Quaternion left_controller_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (left_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out left_controller_rotation))
            Debug.Log("Left controller rot: " + left_controller_rotation.ToString("F2"));

        return new LeftHandController(left_controller_pos, left_controller_rotation);
    }

    private RightHandController GetRightHandInfo()
    {
        // get right controller position
        Vector3 right_controller_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (right_controller.TryGetFeatureValue(CommonUsages.devicePosition, out right_controller_pos))
            Debug.Log("Right controller pos: " + right_controller_pos.ToString("F2"));

        // get right controller rotation
        Quaternion right_controller_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (right_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out right_controller_rotation))
            Debug.Log("Right controller rot: " + right_controller_rotation.ToString("F2"));

        return new RightHandController(right_controller_pos, right_controller_rotation);
    }

    private Headset GetHeadsetInfo() 
    {
        if (!head_connected || !right_connected || !left_connected)
        {
            Debug.Log("Err: Not all devices connected");
            return new Headset();
        }

        // get headset position
        Vector3 headset_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (head_device.TryGetFeatureValue(CommonUsages.centerEyePosition, out headset_pos))
            Debug.Log("Headset pos: " + headset_pos.ToString("F2"));

        // get headset rotation
        Quaternion head_device_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (head_device.TryGetFeatureValue(CommonUsages.deviceRotation, out head_device_rotation))
            Debug.Log(head_device.name + " rot: " + head_device_rotation.ToString("F2"));

        return new Headset(headset_pos, head_device_rotation);
    }
}