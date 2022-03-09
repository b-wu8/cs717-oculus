using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

[System.Serializable]
public class PrimaryButtonEvent : UnityEvent<bool> { }

public class PrimaryButtonWatcher : MonoBehaviour
{
    public PrimaryButtonEvent primaryButtonPress; // object accessed as a variable of Unity script (BallController's watcher)
    public Config config;
    private bool lastButtonState = false;
    private List<InputDevice> devicesWithPrimaryButton;

    private void Awake()
    {
        if (primaryButtonPress == null)
        {
            primaryButtonPress = new PrimaryButtonEvent();
        }

        devicesWithPrimaryButton = new List<InputDevice>();
    }

    void OnEnable()
    {
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        foreach(InputDevice device in allDevices)
            InputDevices_deviceConnected(device);

        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
        InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
        devicesWithPrimaryButton.Clear();
    }

    private void InputDevices_deviceConnected(InputDevice device)
    {
        bool discardedValue;
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out discardedValue))
        {
            devicesWithPrimaryButton.Add(device); // Add any devices that have a primary button.
        }
        //Debug.Log("Device connected " + device.name + device.characteristics);
    }

    private void InputDevices_deviceDisconnected(InputDevice device)
    {
        if (devicesWithPrimaryButton.Contains(device))
            devicesWithPrimaryButton.Remove(device);
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

    void Update()
    {
        bool tempState = false;
        foreach (var device in devicesWithPrimaryButton)
        {
            bool primaryButtonState = false;
            tempState = device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonState) // did get a value
                        && primaryButtonState // the value we got
                        || tempState; // cumulative result from other controllers


            // get the position of the device
            Vector3 device_pos = new Vector3(0.0f, 0.0f, 0.0f);
            if(device.TryGetFeatureValue(CommonUsages.devicePosition, out device_pos))
            {
                Debug.Log(device.name + " pos: " + device_pos.ToString("F2"));
                if (device.name.Contains("Right"))
                {
                    SendPosToServer(device_pos);
                } else
                {
                    Debug.Log("Err: Did not find device");
                }
            }

            // get the position of the device
            Quaternion device_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out device_rotation))
                Debug.Log(device.name + " rot: " + device_rotation.ToString("F2"));

            // get the eye position of the device
            Vector3 left_eye_pos = new Vector3(0.0f, 0.0f, 0.0f);
            if (device.TryGetFeatureValue(CommonUsages.leftEyePosition, out left_eye_pos))
                Debug.Log(device.name + " left eye pos: " + left_eye_pos.ToString("F2"));

            // get the eye position of the device
            Vector3 right_eye_pos = new Vector3(0.0f, 0.0f, 0.0f);
            if (device.TryGetFeatureValue(CommonUsages.rightEyePosition, out right_eye_pos))
                Debug.Log(device.name + " right eye pos: " + right_eye_pos.ToString("F2"));
        }

        if(tempState != lastButtonState) // Button state changed since last frame
        {
            primaryButtonPress.Invoke(tempState);
            lastButtonState = tempState;
        }

    }
}