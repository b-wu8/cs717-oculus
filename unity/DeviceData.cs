/* 
 * Declares the classes used in identifying data 
 * about a player using an Oculus device.
 */
using System;
using UnityEngine;

public struct Headset
{
    public Vector3 postion;
    public Quaternion rotation;
    public Headset(Vector3 pos, Quaternion rot)
    {
        postion = pos;
        rotation = rot;
    }
}

public struct LeftHandController
{
    public Vector3 postion;
    public Quaternion rotation;
    public LeftHandController(Vector3 pos, Quaternion rot)
    {
        postion = pos;
        rotation = rot;
    }
}

public struct RightHandController
{
    public Vector3 postion;
    public Quaternion rotation;
    public RightHandController(Vector3 pos, Quaternion rot)
    {
        postion = pos;
        rotation = rot;
    }
}

public struct LeftJoystick
{
    public Vector2 position;
    public LeftJoystick(Vector2 pos)
    {
        position = pos;
    }
}

public struct RightJoystick
{
    public Vector2 position;
    public RightJoystick(Vector2 pos)
    {
        position = pos;
    }
}

public struct Timestamp
{
    public DateTime time;
    public Timestamp(DateTime t)
    {
        time = t;
    }
}