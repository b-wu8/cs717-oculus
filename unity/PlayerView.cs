using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerView : MonoBehaviour
{
    public Transform xr_rig_offset;
    public GameObject debug_canvas;
    public Dictionary<string, Avatar> avatars;
    public Config config;
    private GameObject sphere, plane, xtemp;
    public Vector3 sphere_loc, plane_loc;
    public int num_players;
    public bool display_debug;

    // Start is called before the first frame update
    void Start()
    {
        // init game objects
        Debug.Log("Player View: Start");
        display_debug = false;
        debug_canvas.SetActive(display_debug);

        // sphere init
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = new Vector3(0f, 0f, 0f);        // init location
        var sphere_renderer = sphere.GetComponent<Renderer>();
        sphere_renderer.material.SetColor("_Color", Color.red);     // change color

        // plane init
        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);    
        plane.transform.position = new Vector3(0f, 0f, 0f);         // init location
        var plane_renderer = plane.GetComponent<Renderer>();
        plane_renderer.material.SetColor("_Color", Color.white);    // change color

        // test_ball
        xtemp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        xtemp.transform.position = new Vector3(4f, 4f, 4f);        // init location
        var xtemp_renderer = xtemp.GetComponent<Renderer>();
        xtemp_renderer.material.SetColor("_Color", Color.green);     // change color

        avatars = new Dictionary<string, Avatar>();
    }

    private string GetTimeStamp(DateTime time)
    {
        return time.ToString("yyyyMMddHHmmssffff");
    }

    // Update is called once per frame
    public void Update()
    {
        // Render red sphere and white plane
        sphere.transform.position = sphere_loc;
        plane.transform.position = plane_loc;

        // Check if any avatars were marked for destruction
        bool destroyed_avatar = false;;    
        foreach (KeyValuePair<string, Avatar> kv_avatar in avatars) {
            if (!kv_avatar.Value.to_be_destroyed)
                kv_avatar.Value.render();
            else {
                kv_avatar.Value.destroy();
                destroyed_avatar = true;
            }
        }

        // If there were any destroyed avatars, remvoe them
        if (destroyed_avatar) {
            Dictionary<string, Avatar> temp_avatars = new Dictionary<string, Avatar>();
            foreach (KeyValuePair<string, Avatar> kv_avatar in avatars)
                if (!kv_avatar.Value.to_be_destroyed)
                    temp_avatars.Add(kv_avatar.Key, kv_avatar.Value);
            avatars = temp_avatars;
        }

        // Move the players camera rig
        if (avatars.ContainsKey(config.player_name)) {
            Avatar main_avatar = avatars[config.player_name];
            xr_rig_offset.position = main_avatar.offset;
        }

        // Display debug canvas
        if (display_debug != debug_canvas.activeSelf)
            debug_canvas.SetActive(display_debug);
    }
}
