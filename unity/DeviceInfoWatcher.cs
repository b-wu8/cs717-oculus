/*
 * This is where all the device inputs getters(e.g. the headset and lefthand controller) are located.
 */

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
            InputDevices_deviceConnected(device);
        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
    }

    void OnDisable()
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
            // Debug.Log("Head not found");
        }
        if ((device.characteristics & InputDeviceCharacteristics.Left) == InputDeviceCharacteristics.Left)
        {
            left_controller = device;
            left_connected = true;
        }
        else
        {
            // Debug.Log("left controller not found");
        }
        if ((device.characteristics & InputDeviceCharacteristics.Right) == InputDeviceCharacteristics.Right)
        {
            right_controller = device;
            right_connected = true;
        }
        else
        {
            // Debug.Log("right controller not found");
        }
    }

    private void InputDevices_deviceDisconnected(InputDevice device)
    {
        // Debug.Log("Device disconnected: " + device);
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

    public string GetControllerData() {
        string data = System.String.Empty;
        data += GetHeadset().to_string() + " ";
        data += GetLeftHandController().to_string() + " ";
        data += GetRightHandController().to_string()+ " ";
        data += GetLeftJoystick().to_string() + " ";
        data += GetRightJoystick().to_string() + " ";
        data += DateTime.Now.ToString("yyyyMMddHHmmssffff");
        return data;
    }

    public LeftHandController GetLeftHandController()
    {
        if (!left_connected)
        {
            // Debug.Log("Err: Left hand controller not connected");
            return new LeftHandController();
        }
        // get left controller position
        Vector3 left_controller_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (left_controller.TryGetFeatureValue(CommonUsages.devicePosition, out left_controller_pos)) {
            // Debug.Log("Left controller pos: " + left_controller_pos.ToString("F2"));
        }
        // get left controller rotation
        Quaternion left_controller_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (left_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out left_controller_rotation)) {
            // Debug.Log("Left controller rot: " + left_controller_rotation.ToString("F2"));
        }
        return new LeftHandController(left_controller_pos, left_controller_rotation);
    }

    public RightHandController GetRightHandController()
    {
        if (!right_connected)
        {
            // Debug.Log("Err: Right hand controller not connected");
            return new RightHandController();
        }
        // get right controller position
        Vector3 right_controller_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (right_controller.TryGetFeatureValue(CommonUsages.devicePosition, out right_controller_pos)) {
            // Debug.Log("Right controller pos: " + right_controller_pos.ToString("F2"));
        }
        // get right controller rotation
        Quaternion right_controller_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (right_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out right_controller_rotation)) {
            // Debug.Log("Right controller rot: " + right_controller_rotation.ToString("F2"));
        }
        return new RightHandController(right_controller_pos, right_controller_rotation);
    }

    public Headset GetHeadset() 
    {
        if (!head_connected)
        {
            // Debug.Log("Err: Headset not connected");
            return new Headset();
        }
        // get headset position
        Vector3 headset_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (head_device.TryGetFeatureValue(CommonUsages.centerEyePosition, out headset_pos)) {
            // Debug.Log("Headset pos: " + headset_pos.ToString("F2"));
        }
        // get headset rotation
        Quaternion head_device_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (head_device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out head_device_rotation)) {
            // Debug.Log(head_device.name + " rot: " + head_device_rotation.ToString("F2"));
        }
        //if (head_device.TryGetFeatureValue(CommonUsages.deviceRotation, out head_device_rotation))
        //    // Debug.Log(head_device.name + " rot: " + head_device_rotation.ToString("F2"));
        return new Headset(headset_pos, head_device_rotation);
    }
    
    public RightJoystick GetRightJoystick()
    {
        if (!right_connected)
        {
            // Debug.Log("Err: Right hand controller not connected");
            return new RightJoystick();
        }
        // get right controller position
        Vector2 right_joystick_vec2 = new Vector2(0.0f, 0.0f);
        if (right_controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out right_joystick_vec2)) {
            // Debug.Log("Right controller Joystick: " + right_joystick_vec2.ToString("F2"));
        }
        return new RightJoystick(right_joystick_vec2);
    }

    public LeftJoystick GetLeftJoystick()
    {
        if (!left_connected)
        {
            // Debug.Log("Err: Left hand controller not connected");
            return new LeftJoystick();
        }
        // get left controller position
        Vector2 left_joystick_vec2 = new Vector2(0.0f, 0.0f);
        if (left_controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out left_joystick_vec2)) {
            // Debug.Log("Left controller Joystick: " + left_joystick_vec2.ToString("F2"));
        }
        return new LeftJoystick(left_joystick_vec2);
    }
}