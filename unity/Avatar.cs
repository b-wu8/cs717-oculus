/* 
 * Declares the classes used in identifying data 
 * about a player using an Oculus device.
 */

using System;
using UnityEngine;
using System.Collections.Generic;

public static class AvatarColors {
    public static IReadOnlyList<Color> COLORS = new List<Color>(new Color[] {
        new Color(0, 255, 255),  // Cyan
        new Color(0, 128, 255),  // LightBlue
        new Color(0, 0, 255),  // Blue
        new Color(128,0,255),  // Purple
        new Color(225, 0, 255),  // Magenta
        new Color(255, 0, 128),  // Pink
        new Color(255, 0, 0),  // Red
        new Color(255, 128, 0),  // Orange
        new Color(255, 255, 0),  // Yellow
        new Color(128, 225, 0),  // Lime
        new Color(0, 255, 0),  // Green
        new Color(0, 255, 128)  // Teal
    });
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

    public Avatar(string player_name, int color_idx) 
    {
        Debug.Log("Created new avatar in Avatar(string player_name)");
        this.offset = new Vector3(0f, 1.5f, 0f);  // TODO: Remove this (offset is set by server)
        this.color = AvatarColors.COLORS[color_idx % AvatarColors.COLORS.Count];
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
        int h_len = 2;
        this.avatar_name = infos[1];
        this.headset_controller = new Headset(
            StringToVec3(infos[h_len + 0], infos[h_len + 1], infos[h_len + 2]), 
            StringToQuat(infos[h_len + 3], infos[h_len + 4], infos[h_len + 5], infos[h_len + 6]));
        this.left_controller = new LeftHandController(
            StringToVec3(infos[h_len + 7], infos[h_len + 8], infos[h_len + 9]), 
            StringToQuat(infos[h_len + 10], infos[h_len + 11], infos[h_len + 12], infos[h_len + 13]));
        this.right_controller = new RightHandController(
            StringToVec3(infos[h_len + 14], infos[h_len + 15], infos[h_len + 16]), 
            StringToQuat(infos[h_len + 17], infos[h_len + 18], infos[h_len + 19], infos[h_len + 20]));
        this.offset = StringToVec3(infos[h_len + 21], infos[h_len + 22], infos[h_len + 23]);
        this.timestamp = infos[h_len + 24];
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
