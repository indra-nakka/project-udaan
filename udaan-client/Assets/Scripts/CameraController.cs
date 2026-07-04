using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Free-look camera. The right stick (via FlightInputRouter.Last aimX/aimY) orbits the view within a
/// cone around the drone — decoupled from flight — so you can look and fire in one direction while
/// flying another. Recenters when the stick is released (aim returns to 0). Chase (default) and FPV
/// variants; toggle with C / Y / VIEW button. Works offline (single-player) and as the owner online.
/// </summary>
public class CameraController : NetworkBehaviour
{
    [Header("Mounts")]
    public Transform fpvMount;
    public Transform tpvMount; // optional reference; chase is computed from the look direction

    [Header("Settings")]
    public float transitionSpeed = 18f;
    [Tooltip("Start in first-person. Default = chase.")]
    public bool startInFPV = false;

    [Header("Free-look cone")]
    public float maxLookYaw = 70f;
    public float maxLookPitch = 45f;

    [Header("Chase framing")]
    public float chaseDistance = 8f;
    public float chaseHeight = 2.5f;

    private Transform mainCamera;
    private FlightInputRouter _input;
    private bool isFPV;
    private Renderer[] _droneRenderers;
    private bool _bodyHidden;

    void Start()
    {
        isFPV = startInFPV;
        _input = GetComponent<FlightInputRouter>();

        bool offline = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
        if (offline) SetupCamera();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        SetupCamera();
    }

    private void SetupCamera()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("CameraController: no Camera tagged 'MainCamera' found in the scene.");
            return;
        }
        mainCamera = Camera.main.transform;
        mainCamera.SetParent(null); // we drive world position/rotation directly each frame
        _droneRenderers = GetComponentsInChildren<Renderer>(true); // to hide the body in FPV
    }

    // Hide the drone's own mesh in FPV so it doesn't block the cockpit view.
    private void SetBodyHidden(bool hidden)
    {
        if (_droneRenderers == null || _bodyHidden == hidden) return;
        _bodyHidden = hidden;
        foreach (var r in _droneRenderers) if (r != null) r.enabled = !hidden;
    }

    void Update()
    {
        if (mainCamera == null) return;
        if (IsSpawned && !IsOwner) return;

        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.JoystickButton3)) ToggleView();

        SetBodyHidden(isFPV); // don't render our own drone in first-person

        // Free-look offsets from the right stick (0 when released -> recenters to forward).
        FlightInputState aim = _input != null ? _input.Last : FlightInputState.None;
        float yawOff = aim.aimX * maxLookYaw;
        float pitchOff = -aim.aimY * maxLookPitch; // up = look up

        Quaternion lookRot = transform.rotation * Quaternion.Euler(pitchOff, yawOff, 0f);
        Vector3 lookDir = lookRot * Vector3.forward;

        Vector3 desiredPos;
        Quaternion desiredRot;
        if (isFPV)
        {
            desiredPos = fpvMount != null ? fpvMount.position : transform.position;
            desiredRot = lookRot;
        }
        else
        {
            desiredPos = transform.position - lookDir * chaseDistance + Vector3.up * chaseHeight;
            desiredRot = Quaternion.LookRotation(lookDir, Vector3.up);
        }

        mainCamera.position = Vector3.Lerp(mainCamera.position, desiredPos, Time.deltaTime * transitionSpeed);
        mainCamera.rotation = Quaternion.Lerp(mainCamera.rotation, desiredRot, Time.deltaTime * transitionSpeed);
    }

    public void ToggleView() => isFPV = !isFPV;
}
