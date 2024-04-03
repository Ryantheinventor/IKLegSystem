using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Simple script to set a target position where the camera is pointing when the user clicks
/// </summary>
public class CameraTargetSystem : MonoBehaviour
{

    public LayerMask walkableLayer;

    public static Vector3 targetPos = Vector3.zero;


    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0)) 
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f,0.5f));
            if (Physics.Raycast(ray, out RaycastHit hit, 1000, walkableLayer)) 
            {
                targetPos = hit.point;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(targetPos, new Vector3(0.2f, 0.2f, 0.2f));
    }
}
