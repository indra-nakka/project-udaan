using UnityEngine;
using Unity.Netcode; // Added Netcode

// Changed from MonoBehaviour to NetworkBehaviour
public class CameraController : NetworkBehaviour 
{
    [Header("Camera References")]
    // We made this private because the script will find it automatically now
    private Transform mainCamera; 
    
    public Transform fpvMount;
    public Transform tpvMount;

    [Header("Settings")]
    public float transitionSpeed = 15f;
    private bool isFPV = true;

    void Start()
    {
        // 1. CRUCIAL: If this isn't our drone, don't steal the camera!
        if (!IsOwner) return; 

        // 2. Automatically find the Main Camera in the scene
        mainCamera = Camera.main.transform;

        // 3. Snap to FPV instantly on start
        mainCamera.position = fpvMount.position;
        mainCamera.rotation = fpvMount.rotation;
        
        // Parent the camera to the drone so it stays with us
        mainCamera.SetParent(transform);
    }

    void Update()
    {
        // Don't run the camera logic if it's someone else's drone
        if (!IsOwner || mainCamera == null) return; 

        // Toggle view with Keyboard 'C' or Xbox 'Y' Button (JoystickButton3)
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            isFPV = !isFPV;
        }

        // Smoothly glide the camera to the active mount
        if (isFPV)
        {
            mainCamera.position = Vector3.Lerp(mainCamera.position, fpvMount.position, Time.deltaTime * transitionSpeed);
            mainCamera.rotation = Quaternion.Lerp(mainCamera.rotation, fpvMount.rotation, Time.deltaTime * transitionSpeed);
        }
        else
        {
            mainCamera.position = Vector3.Lerp(mainCamera.position, tpvMount.position, Time.deltaTime * transitionSpeed);
            mainCamera.rotation = Quaternion.Lerp(mainCamera.rotation, tpvMount.rotation, Time.deltaTime * transitionSpeed);
        }
    }
}