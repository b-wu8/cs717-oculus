/* 
 * Declares the classes used in identifying data 
 * about a player using an Oculus device.
 */

using System;
using UnityEngine;
using System.Collections.Generic;

/*
 * Types to classify what kind of message is being sent.
 */
static class Constants
{
    // startup message indicates that a player is trying to connect to room
    // the player name is mapped to the included IP address
    public const int SYN = 1;
    // controller input message
    public const int INPUT = 2;
    // player formally quits
    public const int FIN = 3;
    public const int HEARTBEAT = 4;
    public const int DATA = 5;
    public const int LEAVE = 6;
}

public class Avatar {
    private string avatar_name, player_name;
    private Color color;
    public string timestamp;
    public Headset headset_controller;
    public LeftHandController left_controller;
    public RightHandController right_controller;
    public Vector3 offset;
    public GameObject head, left_hand, right_hand;
    public bool is_created, to_be_destroyed;
    private List<Color> colors = new List<Color>(new Color[] {
        Color.magenta, Color.cyan, Color.yellow, Color.green, Color.grey, Color.blue, Color.red});
    public static int color_idx = 0;
    public Avatar(string player_name) 
    {
        Debug.Log("Created new avatar in Avatar(string player_name)");
        this.offset = new Vector3(0f, 1.5f, 0f);  // Everyone is 1.5 units tall
        this.color = colors[color_idx++];
        this.player_name = player_name;
        this.is_created = this.to_be_destroyed = false;
    }
    
    public Avatar(Avatar other)
    {
        Debug.Log("Created new avatar in Avatar(Avatar other)");
        this.avatar_name = other.avatar_name;
        this.player_name = other.player_name;
        this.is_created = false;
        this.headset_controller = (Headset) Headset.deep_copy(other.headset_controller);
        this.left_controller = (LeftHandController) LeftHandController.deep_copy(other.left_controller);
        this.right_controller = (RightHandController) RightHandController.deep_copy(other.right_controller);
    }

    public void update(string server_message)
    {
        string[] infos = server_message.Split(' ');
        this.avatar_name = infos[0];
        this.headset_controller = new Headset(StringToVec3(infos[1], infos[2], infos[3]), StringToQuat(infos[4], infos[5], infos[6], infos[7]));
        this.left_controller = new LeftHandController(StringToVec3(infos[8], infos[9], infos[10]), StringToQuat(infos[11], infos[12], infos[13], infos[14]));
        this.right_controller = new RightHandController(StringToVec3(infos[15], infos[16], infos[17]), StringToQuat(infos[18], infos[19], infos[20], infos[21]));
        this.offset = StringToVec3(infos[22], infos[23], infos[24]);
        this.timestamp = infos[25];
    }
    
    public static Vector3 StringToVec3(string str_x, string str_y, string str_z){
        return new Vector3(float.Parse(str_x), float.Parse(str_y), float.Parse(str_z));
    }

    public static Vector2 StringToVec2(string str_x, string str_y){
        return new Vector2(float.Parse(str_x), float.Parse(str_y));
    }

    public static Quaternion StringToQuat(string str_x, string str_y, string str_z, string str_w){
        return new Quaternion(float.Parse(str_x), float.Parse(str_y), float.Parse(str_z), float.Parse(str_w));
    }

    // REMOVEME: Avatar string never gets sent back
    public string to_string()
    {
        string data = System.String.Empty;
        data += this.headset_controller.to_string() + " ";
        data += this.left_controller.to_string() + " ";
        data += this.right_controller.to_string() + " ";
        data += this.timestamp;
        return data;
    }

    public void create() {
        if (is_created) {
            Debug.Log("Avatar already created");
            return;
        }

        head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        head.transform.position = headset_controller.position + offset;
        head.transform.rotation = headset_controller.rotation;
        head.GetComponent<Renderer>().material.SetColor("_Color", color);
        if (player_name == avatar_name)
            head.SetActive(false);

        left_hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        left_hand.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        left_hand.transform.position = left_controller.position + offset;
        left_hand.transform.rotation = left_controller.rotation;
        left_hand.GetComponent<Renderer>().material.SetColor("_Color", color);

        right_hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        right_hand.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        right_hand.transform.position = right_controller.position + offset;
        right_hand.transform.rotation = right_controller.rotation;
        right_hand.GetComponent<Renderer>().material.SetColor("_Color", color);

        is_created = true;
    }

    public void destroy() {
        if (!is_created) {
            Debug.Log("No avatar to destroy");
            return;
        }

        GameObject.Destroy(head);
        GameObject.Destroy(left_hand);
        GameObject.Destroy(right_hand);
        Debug.Log("Destroyed avatar for " + avatar_name);
    }

    public void render() {
        if (!is_created) {
            create();
            return;
        }

        head.transform.position = headset_controller.position + offset;
        head.transform.rotation = headset_controller.rotation;

        left_hand.transform.position = left_controller.position + offset;
        left_hand.transform.rotation = left_controller.rotation;

        right_hand.transform.position = right_controller.position + offset;
        right_hand.transform.rotation = right_controller.rotation;
    }
}

/*
public class PlayerInfo 
{
    public string name;
    public Headset headset;
    public LeftHandController left_hand;
    public RightHandController right_hand;
    public string timestamp;
    
    //string constructor
    public PlayerInfo(string name, Headset headset, LeftHandController left_hand, RightHandController right_hand)
    {
        this.name = name;
        this.headset = (Headset) Headset.deep_copy(headset);
        this.left_hand = (LeftHandController) LeftHandController.deep_copy(left_hand);
        this.right_hand = (RightHandController) RightHandController.deep_copy(right_hand);
    }

    public static Vector3 StringToVec3(string str_x, string str_y, string str_z){
        return new Vector3(float.Parse(str_x), float.Parse(str_y), float.Parse(str_z));
    }

    public static Vector2 StringToVec2(string str_x, string str_y){
        return new Vector2(float.Parse(str_x), float.Parse(str_y));
    }

    public static Quaternion StringToQuat(string str_x, string str_y, string str_z, string str_w){
        return new Quaternion(float.Parse(str_x), float.Parse(str_y), float.Parse(str_z), float.Parse(str_w));
    }

    public PlayerInfo(string data)
    {
        string[] infos = data.Split(' ');
        this.name = infos[0];
        this.headset = new Headset(StringToVec3(infos[1], infos[2], infos[3]), StringToQuat(infos[4], infos[5], infos[6], infos[7]));
        this.left_hand = new LeftHandController(StringToVec3(infos[8], infos[9], infos[10]), StringToQuat(infos[11], infos[12], infos[13], infos[14]));
        this.right_hand = new RightHandController(StringToVec3(infos[15], infos[16], infos[17]), StringToQuat(infos[18], infos[19], infos[20], infos[21]));
        this.timestamp = infos[22];
        // TODO: parse joystick
    }

    public string to_string()
    {
        string data = System.String.Empty;
        data += this.headset.to_string() + " ";
        data += this.left_hand.to_string() + " ";
        data += this.right_hand.to_string() + " ";
        data += this.timestamp;

        return data;
    }
}
*/

/*
 * Devices whose data is trackable.
 * Includes the headset and controllers.
 */
public class TrackableDevice 
{
    public Vector3 position;
    public Quaternion rotation;

    public TrackableDevice(Vector3 pos, Quaternion rot)
    {
        this.position.x = pos.x;
        this.position.y = pos.y;
        this.position.z = pos.z;
        this.rotation.x = rot.x;
        this.rotation.y = rot.y;
        this.rotation.z = rot.z;
        this.rotation.w = rot.w;
    }

    public TrackableDevice()
    {
        this.position.x = 0;
        this.position.y = 0;
        this.position.z = 0;
        this.rotation.x = 0;
        this.rotation.y = 0;
        this.rotation.z = 0;
        this.rotation.w = 0;
    }

    /*
     * Serializes metadata in string format.
     */
    public string to_string() 
    {
        string data = System.String.Empty;
        data += this.position.x + " " + 
                this.position.y + " " + 
                this.position.z + " ";
        data += this.rotation.x + " " + 
                this.rotation.y + " " + 
                this.rotation.z + " " + 
                this.rotation.w;
        return data;
    }

    /*
     * Creates a deep copy of the class.
     */
    public static TrackableDevice deep_copy(TrackableDevice device) 
    {
        return new TrackableDevice(device.position, device.rotation);
    }
}

public class Headset : TrackableDevice
{
    public Headset(): base() {}
    public Headset(Vector3 pos, Quaternion rot) : base(pos, rot)
    {
    }
}

public class LeftHandController : TrackableDevice
{
    public LeftHandController(): base() {}

    public LeftHandController(Vector3 pos, Quaternion rot) : base(pos, rot)
    {
    }

}

public class RightHandController : TrackableDevice
{
    public RightHandController() : base() { }
    public RightHandController(Vector3 pos, Quaternion rot) : base(pos, rot)
    {
    }
}

/* 
 * Each of the joysticks on the device controllers.
 */
public class Joystick {
    public Vector2 position;
    public Joystick()
    {
        this.position.x = 0;
        this.position.y = 0;
    }
    public Joystick(Vector2 pos)
    {
        this.position.x = pos.x;
        this.position.y = pos.y;
    }

    /*
     * Serialize joystick movement metadata as a string.
     */
    public string to_string() 
    {
        string data = System.String.Empty;
        data += this.position.x + " " + 
                this.position.y;
        return data;
    }

    /*
     * Creates a deep copy of the class.
     */
    public Joystick deep_copy(Joystick device) 
    {
        return new Joystick(device.position);
    }
}

public class LeftJoystick : Joystick
{
    public LeftJoystick() : base() {}
    public LeftJoystick(Vector2 pos) : base(pos)
    {
    }
}

public class RightJoystick : Joystick
{
    public RightJoystick() : base() {}
    public RightJoystick(Vector2 pos) : base(pos)
    {
    }
}

public class Timestamp
{
    public DateTime time;
    public Timestamp(DateTime t)
    {
        time = t;
    }
}

