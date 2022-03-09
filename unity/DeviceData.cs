/* 
 * Declares the classes used in identifying data 
 * about a player using an Oculus device.
 */
using System;
using UnityEngine;

public struct Headset : ICloneable
{
    public Vector3 postion;
    public Quaternion rotation;
    public Headset(Vector3 pos, Quaternion rot)
    {
        postion = pos;
        rotation = rot;
    }
}

public struct LeftHandController : ICloneable
{
    public Vector3 postion;
    public Quaternion rotation;
    public LeftHandController(Vector3 pos, Quaternion rot)
    {
        postion = pos;
        rotation = rot;
    }
}

public struct RightHandController : ICloneable
{
    public Vector3 postion;
    public Quaternion rotation;
    public RightHandController(Vector3 pos, Quaternion rot)
    {
        postion = pos;
        rotation = rot;
    }
}

public struct LeftJoystick : ICloneable
{
    public Vector2 position;
    public LeftJoystick(Vector2 pos)
    {
        position = pos;
    }
}

public struct RightJoystick : ICloneable
{
    public Vector2 position;
    public RightJoystick(Vector2 pos)
    {
        position = pos;
    }
}

public struct Timestamp : ICloneable
{
    public DateTime time;
    public Timestamp(DateTime t)
    {
        time = t;
    }
}