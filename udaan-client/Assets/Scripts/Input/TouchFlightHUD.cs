using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-contained on-screen touch controls for the M1 flight toy. Builds its own Canvas at runtime
/// (no prefab wiring required) and reads raw Input.touches by screen region, so it needs no
/// EventSystem/GraphicRaycaster. Also drives the camera-view and scheme toggle buttons.
///
/// Two schemes (toggle in-game, per active-sprint "prototype 2-3 touch schemes"):
///   TwinStick   : left virtual stick = strafe/altitude, right virtual stick = yaw/pitch (absolute).
///   MoveAndDrag : left virtual stick = strafe/altitude, right side = drag-to-aim (relative rate).
/// Throttle is a persistent center-detent slider (fwd/zero/reverse) with a sticky neutral.
/// When a controller is connected the HUD can either hide or mirror the physical inputs.
///
/// All regions are normalized (0..1 screen, y up) and inspector-exposed for on-device tuning.
/// </summary>
public class TouchFlightHUD : MonoBehaviour, IFlightInput
{
    public enum Scheme { TwinStick, MoveAndDrag }
    public enum ControllerHudMode { Mirror, Hide }

    [Header("Scheme")]
    public Scheme scheme = Scheme.TwinStick;

    [Header("Controller (when a gamepad is connected)")]
    [Tooltip("Mirror = show fixed-home sticks reflecting the physical controller. Hide = hide the touch UI.")]
    public ControllerHudMode controllerHudMode = ControllerHudMode.Mirror;
    [Tooltip("Fixed home position (normalized) of the left stick in Mirror mode.")]
    public Vector2 mirrorLeftHome = new Vector2(0.16f, 0.26f);
    [Tooltip("Fixed home position (normalized) of the right stick in Mirror mode.")]
    public Vector2 mirrorRightHome = new Vector2(0.84f, 0.26f);

    [Header("Screen Regions (normalized: x, y, width, height; y up)")]
    public Rect leftStickRegion  = new Rect(0.00f, 0.00f, 0.45f, 0.72f);
    public Rect rightAimRegion   = new Rect(0.52f, 0.00f, 0.34f, 0.72f);
    public Rect throttleRegion   = new Rect(0.90f, 0.10f, 0.075f, 0.55f);
    public Rect schemeButton     = new Rect(0.02f, 0.91f, 0.095f, 0.06f);
    public Rect camButton        = new Rect(0.885f, 0.92f, 0.095f, 0.06f);
    public Rect restartButton    = new Rect(0.885f, 0.845f, 0.095f, 0.055f);
    public Rect aimModeButton    = new Rect(0.02f, 0.84f, 0.095f, 0.06f);
    public Rect switchTargetButton  = new Rect(0.02f, 0.77f, 0.095f, 0.055f); // cycle locked target
    public Rect primaryFireButton   = new Rect(0.805f, 0.05f, 0.10f, 0.12f);  // bullets (held)
    public Rect secondaryFireButton = new Rect(0.805f, 0.19f, 0.10f, 0.095f); // rockets (held)

    [Header("Feel")]
    [Tooltip("LEFT stick is the primary control (aim: yaw/pitch); RIGHT handles strafe/altitude. Uncheck to flip.")]
    public bool leftStickAims = true;
    [Tooltip("Throttle slider snaps back to center (zero) when released. Off = stays where you leave it (cruise).")]
    public bool throttleSelfCenters = false;
    [Tooltip("Virtual-stick travel as a fraction of the screen's shorter side.")]
    public float stickRadiusFraction = 0.13f;
    [Tooltip("Pixels of drag = full aim deflection (MoveAndDrag scheme). Lower = more sensitive.")]
    public float dragFullScalePixels = 220f;
    [Tooltip("Only show the touch UI on touch devices (still testable with mouse in the Editor).")]
    public bool hideOnDesktop = false;
    [Tooltip("Show the -o- flight crosshair at screen center.")]
    public bool showCrosshair = true;
    [Tooltip("Throttle snaps to exactly 0 within this fraction of center — a sticky neutral for coasting into turns.")]
    public float throttleCenterDeadzone = 0.12f;
    [Tooltip("A hard horizontal flick of the right stick also cycles the locked target (to compare vs the button).")]
    public bool flickToSwitchTarget = true;

    [Header("Weapon reticle")]
    [Tooltip("Distance (m) at which the corner-bracket reticle is 1x; closer = larger, farther = smaller.")]
    public float crosshairRefDistance = 40f;
    public float crosshairMinScale = 0.55f;
    public float crosshairMaxScale = 2.2f;

    [Header("Artificial horizon")]
    public bool showHorizon = true;
    public float horizonPixelsPerDegree = 6f;

    [Header("Mini-radar")]
    public bool showRadar = true;
    public float radarRange = 200f;
    public float radarPixelRadius = 70f;
    [Tooltip("Draw faint map/terrain footprints on the radar. Toggle live with the terrain key.")]
    public bool showRadarTerrain = false;
    public KeyCode radarTerrainToggle = KeyCode.T;

    [Header("Optional overrides")]
    public Font uiFont; // leave null to use the built-in legacy runtime font

    // ---- runtime state ----
    private FlightInputState _state;
    private float _throttle;             // -1..1, center = 0 (up = forward, down = reverse)
    private CameraController _camera;

    // finger ownership (-1 = none; -2 = editor mouse)
    private int _leftFinger = -1, _rightFinger = -1, _throttleFinger = -1;
    private int _primaryFinger = -1, _secondaryFinger = -1; // held fire buttons
    private Vector2 _leftOrigin, _rightPrev;

    // visuals
    private Canvas _canvas;
    private RectTransform _leftBase, _leftKnob, _rightBase, _rightKnob, _throttleTrack, _throttleKnob;
    private RectTransform _schemeRt, _camRt, _restartRt, _aimModeRt, _primaryRt, _secondaryRt, _switchRt;
    private RectTransform _crosshair;      // corner-bracket weapon reticle that snaps onto the lock
    private RectTransform _offscreenArrow; // edge arrow pointing at an off-screen lock
    private RectTransform _flightPip;      // where the drone is actually heading (green dot)
    private RectTransform _horizon;        // rolling/pitching artificial-horizon bar
    private RectTransform _radarRoot;      // mini-radar
    private readonly System.Collections.Generic.List<Image> _radarBlips = new System.Collections.Generic.List<Image>();
    private readonly System.Collections.Generic.List<Transform> _radarTargets = new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<Image> _outpostBlips = new System.Collections.Generic.List<Image>();
    private readonly System.Collections.Generic.List<Image> _terrainBlips = new System.Collections.Generic.List<Image>();
    private System.Collections.Generic.List<Vector3> _terrainPoints;   // cached map footprints (world XZ)
    private float _nextRadarScan;
    // objective marker (e.g. the Core during Defend): world-tracked tag + edge arrow
    private RectTransform _objMarker, _objArrow;
    private Image _objIcon, _objArrowImg;
    private Text _objLabel;
    private Transform _objTarget;
    private TargetHealth _objHealth;
    private string _objName = "";
    private float _prevAimX, _nextFlick;
    private Text _schemeLabel, _aimModeLabel, _infoText, _statText, _ammoText;
    private Rigidbody _droneRb;
    private TargetHealth _ownHealth;
    private DroneWeapon _ownWeapon;
    private RectTransform _hpBarFill, _damageFlash, _hitmarker;
    private float _flashUntil, _hitmarkerUntil, _hpBarWidth = 320f;
    private string _lastAmmo, _lastStat; // avoid per-frame Text.text reassignment (canvas rebuild) when unchanged
    private Sprite _circle, _square, _triangle;
    private RaceManager _race;
    private FlightInputRouter _router;
    private TargetingSystem _targeting;
    private Camera _lockCam;

    // controller detection (cached; GetJoystickNames allocates)
    private bool _controllerConnected;
    private float _nextControllerCheck;

    private const int NoFinger = -1;
    private const int MouseFinger = -2;

    void Start()
    {
        _camera = GetComponent<CameraController>();
        _router = GetComponent<FlightInputRouter>();
        _targeting = GetComponent<TargetingSystem>();
        _droneRb = GetComponent<Rigidbody>();
        _ownHealth = GetComponent<TargetHealth>();
        _ownWeapon = GetComponent<DroneWeapon>();
        if (hideOnDesktop && !Input.touchSupported) { enabled = false; return; }
        BuildUI();
    }

    // -------------------------------------------------------------------------------------------
    // Input reading
    // -------------------------------------------------------------------------------------------
    void Update()
    {
        // Reset momentary axes; throttle persists.
        float strafe = 0f, altitude = 0f, yaw = 0f, pitch = 0f, aimX = 0f, aimY = 0f;
        bool brake = false, firePrimary = false, fireSecondary = false;

        float radius = Mathf.Min(Screen.width, Screen.height) * stickRadiusFraction;

        // Editor / desktop: synthesize a single "finger" from the mouse so schemes are testable.
        ProcessPointers();

        bool controller = IsControllerConnected();
        bool hide = controller && controllerHudMode == ControllerHudMode.Hide;
        bool mirror = controller && controllerHudMode == ControllerHudMode.Mirror;

        if (_canvas != null && _canvas.enabled == hide) _canvas.enabled = !hide;
        if (hide)
        {
            // Controller owns input; touch UI hidden. Emit neutral touch state (controller feeds via router).
            _state = FlightInputState.None;
            return;
        }

        // ---- Left virtual stick (touch, dynamic) ----
        Vector2 leftVec = Vector2.zero;
        if (_leftFinger != NoFinger)
        {
            Vector2 p = PointerPos(_leftFinger);
            leftVec = Vector2.ClampMagnitude((p - _leftOrigin) / radius, 1f);
            if (!mirror) PlaceStick(_leftBase, _leftKnob, _leftOrigin, leftVec, radius, true);
        }
        else if (!mirror) PlaceStick(_leftBase, _leftKnob, Vector2.zero, Vector2.zero, radius, false);

        // ---- Right virtual stick / drag (touch, dynamic) ----
        Vector2 rightVec = Vector2.zero;
        if (_rightFinger != NoFinger)
        {
            Vector2 p = PointerPos(_rightFinger);
            if (scheme == Scheme.TwinStick)
            {
                rightVec = Vector2.ClampMagnitude((p - _rightPrev) / radius, 1f); // _rightPrev = origin here
                if (!mirror) PlaceStick(_rightBase, _rightKnob, _rightPrev, rightVec, radius, true);
            }
            else // MoveAndDrag: relative rate from per-frame delta
            {
                Vector2 delta = p - _rightPrev;
                rightVec = new Vector2(Mathf.Clamp(delta.x / dragFullScalePixels, -1f, 1f),
                                       Mathf.Clamp(delta.y / dragFullScalePixels, -1f, 1f));
                _rightPrev = p;
                if (!mirror) PlaceStick(_rightBase, _rightKnob, p, Vector2.zero, radius, false);
            }
        }
        else if (!mirror) PlaceStick(_rightBase, _rightKnob, Vector2.zero, Vector2.zero, radius, false);

        // Stick roles: the flight-aim stick (default LEFT) always steers (yaw/pitch). The other stick
        // is strafe/altitude in NoseAim, or the free-aim reticle in FreeAim.
        bool freeAim = _router != null && _router.Aim == FlightInputRouter.AimMode.FreeAim;
        Vector2 flightAim = leftStickAims ? leftVec : rightVec;
        Vector2 secondary = leftStickAims ? rightVec : leftVec;
        yaw = flightAim.x; pitch = flightAim.y;
        if (freeAim) { aimX = secondary.x; aimY = secondary.y; }
        else { strafe = secondary.x; altitude = secondary.y; }

        // ---- Throttle slider: -1..1, center = zero (up = forward, down = reverse) ----
        if (_throttleFinger != NoFinger)
        {
            Rect r = PixelRect(throttleRegion);
            float y = PointerPos(_throttleFinger).y;
            _throttle = Mathf.Clamp(Mathf.Lerp(-1f, 1f, Mathf.InverseLerp(r.yMin, r.yMax, y)), -1f, 1f);
            // Sticky neutral: snap to exactly 0 near center so it's easy to coast into turns.
            if (Mathf.Abs(_throttle) < throttleCenterDeadzone) _throttle = 0f;
        }

        // Fire (held) from the on-screen buttons.
        firePrimary = _primaryFinger != NoFinger;
        fireSecondary = _secondaryFinger != NoFinger;

        // Keyboard fallbacks for Editor testing.
        if (Input.GetKeyDown(KeyCode.T)) ToggleScheme();
        if (Input.GetKeyDown(KeyCode.V)) CycleAimMode(); // gamepad X now = dash
        if (Input.GetKey(KeyCode.LeftShift)) brake = true;

        // Switch locked target: RS-click / Tab (touch = SWITCH button).
        if (Input.GetKeyDown(KeyCode.JoystickButton9) || Input.GetKeyDown(KeyCode.Tab)) SwitchTarget(1);

        // Optional: hard right-stick flick also cycles the target (to compare feel vs the button).
        if (flickToSwitchTarget && Time.time >= _nextFlick)
        {
            if (aimX > 0.8f && _prevAimX <= 0.5f) { SwitchTarget(1); _nextFlick = Time.time + 0.35f; }
            else if (aimX < -0.8f && _prevAimX >= -0.5f) { SwitchTarget(-1); _nextFlick = Time.time + 0.35f; }
        }
        _prevAimX = aimX;

        // ---- Render ----
        float throttleDisplay = _throttle;
        if (mirror)
        {
            // Reflect the live merged input (the connected controller) on fixed-home sticks.
            FlightInputState m = _router != null ? _router.Last : FlightInputState.None;
            Vector2 mFlight = new Vector2(m.yaw, m.pitch);
            Vector2 mSecondary = freeAim ? new Vector2(m.aimX, m.aimY) : new Vector2(m.strafe, m.altitude);
            PlaceStick(_leftBase, _leftKnob, PixelPoint(mirrorLeftHome), leftStickAims ? mFlight : mSecondary, radius, true);
            PlaceStick(_rightBase, _rightKnob, PixelPoint(mirrorRightHome), leftStickAims ? mSecondary : mFlight, radius, true);
            throttleDisplay = m.thrust;
        }

        UpdateAimReticle();
        UpdateInstruments();

        LayoutStatic();
        UpdateThrottleVisual(throttleDisplay);

        _state = new FlightInputState
        {
            thrust = _throttle,
            strafe = strafe,
            altitude = altitude,
            yaw = yaw,
            pitch = pitch,
            aimX = aimX,
            aimY = aimY,
            brake = brake,
            boost = false,
            firePrimary = firePrimary,
            fireSecondary = fireSecondary,
        };
    }

    private void CycleAimMode()
    {
        if (_router != null) _router.CycleAim();
        if (_aimModeLabel != null) _aimModeLabel.text = AimModeLabel();
    }

    private bool IsControllerConnected()
    {
        if (Time.unscaledTime >= _nextControllerCheck)
        {
            _nextControllerCheck = Time.unscaledTime + 0.5f;
            _controllerConnected = false;
            foreach (var n in Input.GetJoystickNames())
                if (!string.IsNullOrEmpty(n)) { _controllerConnected = true; break; }
        }
        return _controllerConnected;
    }

    private Vector2 PixelPoint(Vector2 norm) => new Vector2(norm.x * Screen.width, norm.y * Screen.height);

    public FlightInputState Read() => _state;

    /// <summary>Push race/status text into the top-center readout.</summary>
    public void SetInfoText(string text) { if (_infoText != null) _infoText.text = text; }

    /// <summary>Track a world objective (e.g. the Core): shows a labelled tag on it + an edge arrow when off-screen. Pass null to clear.</summary>
    public void SetObjectiveMarker(Transform t, string label)
    {
        _objTarget = t;
        _objName = label;
        _objHealth = t != null ? t.GetComponent<TargetHealth>() : null;
        if (_objMarker != null) _objMarker.gameObject.SetActive(t != null);
        if (_objArrow != null) _objArrow.gameObject.SetActive(false);
    }

    public void ToggleScheme()
    {
        scheme = scheme == Scheme.TwinStick ? Scheme.MoveAndDrag : Scheme.TwinStick;
        if (_schemeLabel != null) _schemeLabel.text = scheme == Scheme.TwinStick ? "AIM: STICK" : "AIM: DRAG";
        // Release the right finger so the new scheme starts clean.
        _rightFinger = NoFinger;
    }

    private void RestartRace()
    {
        if (_race == null) _race = FindFirstObjectByType<RaceManager>();
        if (_race != null) _race.RestartRace();
    }

    // -------------------------------------------------------------------------------------------
    // Pointer processing: assign fingers to controls on the frame they press down.
    // -------------------------------------------------------------------------------------------
    private void ProcessPointers()
    {
        // Real touches.
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began) AssignFinger(t.fingerId, t.position);
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) ReleaseFinger(t.fingerId);
        }

        // Editor / desktop mouse as one pseudo-finger.
        if (!Input.touchSupported || Application.isEditor)
        {
            if (Input.GetMouseButtonDown(0)) AssignFinger(MouseFinger, Input.mousePosition);
            else if (Input.GetMouseButtonUp(0)) ReleaseFinger(MouseFinger);
        }
    }

    private void AssignFinger(int id, Vector2 pos)
    {
        // Priority: buttons > throttle > sticks (so a tap on a button never grabs a stick).
        if (PixelRect(schemeButton).Contains(pos)) { ToggleScheme(); return; }
        if (PixelRect(camButton).Contains(pos))    { if (_camera != null) _camera.ToggleView(); return; }
        if (PixelRect(restartButton).Contains(pos)) { RestartRace(); return; }
        if (PixelRect(aimModeButton).Contains(pos)) { CycleAimMode(); return; }
        if (PixelRect(primaryFireButton).Contains(pos)) { _primaryFinger = id; return; }
        if (PixelRect(secondaryFireButton).Contains(pos)) { _secondaryFinger = id; return; }
        if (PixelRect(switchTargetButton).Contains(pos)) { SwitchTarget(1); return; }
        if (PixelRect(throttleRegion).Contains(pos)) { _throttleFinger = id; return; }

        if (_leftFinger == NoFinger && PixelRect(leftStickRegion).Contains(pos))
        {
            _leftFinger = id; _leftOrigin = pos; return;
        }
        if (_rightFinger == NoFinger && PixelRect(rightAimRegion).Contains(pos))
        {
            _rightFinger = id; _rightPrev = pos; return; // origin for stick, prev for drag
        }
    }

    private void ReleaseFinger(int id)
    {
        if (_leftFinger == id) _leftFinger = NoFinger;
        if (_rightFinger == id) _rightFinger = NoFinger;
        if (_throttleFinger == id)
        {
            _throttleFinger = NoFinger;
            if (throttleSelfCenters) _throttle = 0f; // spring back to neutral
        }
        if (_primaryFinger == id) _primaryFinger = NoFinger;
        if (_secondaryFinger == id) _secondaryFinger = NoFinger;
    }

    private Vector2 PointerPos(int id)
    {
        if (id == MouseFinger) return Input.mousePosition;
        for (int i = 0; i < Input.touchCount; i++)
            if (Input.GetTouch(i).fingerId == id) return Input.GetTouch(i).position;
        return Vector2.zero;
    }

    private Rect PixelRect(Rect norm) =>
        new Rect(norm.x * Screen.width, norm.y * Screen.height, norm.width * Screen.width, norm.height * Screen.height);

    // -------------------------------------------------------------------------------------------
    // UI construction / layout
    // -------------------------------------------------------------------------------------------
    private void BuildUI()
    {
        _circle = MakeCircleSprite(128);
        _square = MakeSquareSprite();
        _triangle = MakeTriangleSprite(64);

        var canvasGo = new GameObject("TouchFlightHUD_Canvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        Color baseCol = new Color(1f, 1f, 1f, 0.12f);
        Color knobCol = new Color(1f, 1f, 1f, 0.35f);

        _leftBase  = NewImage("LeftBase", _circle, baseCol);
        _leftKnob  = NewImage("LeftKnob", _circle, knobCol);
        _rightBase = NewImage("RightBase", _circle, baseCol);
        _rightKnob = NewImage("RightKnob", _circle, knobCol);
        _throttleTrack = NewImage("ThrottleTrack", _square, new Color(1f, 1f, 1f, 0.10f));
        _throttleKnob  = NewImage("ThrottleKnob", _square, new Color(0.3f, 0.9f, 1f, 0.55f));

        _schemeRt = NewButton("Scheme", scheme == Scheme.TwinStick ? "AIM: STICK" : "AIM: DRAG",
                              new Color(1f, 1f, 1f, 0.20f), out _schemeLabel);
        _camRt    = NewButton("Cam", "VIEW", new Color(1f, 1f, 1f, 0.20f), out _);
        _restartRt = NewButton("Restart", "RESTART", new Color(1f, 0.8f, 0.3f, 0.22f), out _);
        _aimModeRt = NewButton("AimMode", AimModeLabel(), new Color(1f, 1f, 1f, 0.20f), out _aimModeLabel);
        _primaryRt = NewButton("Primary", "FIRE", new Color(1f, 0.5f, 0.3f, 0.30f), out _);
        _secondaryRt = NewButton("Secondary", "ROCKET", new Color(1f, 0.75f, 0.2f, 0.30f), out _);
        _switchRt = NewButton("Switch", "TARGET", new Color(0.4f, 0.8f, 1f, 0.28f), out _);

        // Top-center info readout (race HUD text).
        var infoGo = new GameObject("Info");
        infoGo.transform.SetParent(_canvas.transform, false);
        _infoText = infoGo.AddComponent<Text>();
        _infoText.font = ResolveFont();
        _infoText.alignment = TextAnchor.UpperCenter;
        _infoText.fontSize = 28;
        _infoText.color = Color.white;
        var infoRt = _infoText.rectTransform;
        infoRt.anchorMin = new Vector2(0.5f, 1f); infoRt.anchorMax = new Vector2(0.5f, 1f);
        infoRt.pivot = new Vector2(0.5f, 1f); infoRt.sizeDelta = new Vector2(800, 120);
        infoRt.anchoredPosition = new Vector2(0, -12);

        BuildCrosshair();

        _offscreenArrow = NewImage("OffscreenArrow", _triangle, new Color(1f, 0.3f, 0.3f, 0.9f));
        _offscreenArrow.anchorMin = _offscreenArrow.anchorMax = new Vector2(0.5f, 0.5f);
        _offscreenArrow.pivot = new Vector2(0.5f, 0.5f);
        _offscreenArrow.sizeDelta = new Vector2(46f, 46f);
        _offscreenArrow.gameObject.SetActive(false);

        // Flight-vector marker: green upward chevron (∧) showing where the drone is actually heading.
        var pipGo = new GameObject("FlightPip", typeof(RectTransform));
        pipGo.transform.SetParent(_canvas.transform, false);
        _flightPip = pipGo.GetComponent<RectTransform>();
        _flightPip.anchorMin = _flightPip.anchorMax = new Vector2(0.5f, 0.5f);
        _flightPip.pivot = new Vector2(0.5f, 0.5f);
        _flightPip.sizeDelta = new Vector2(28f, 20f);
        _flightPip.anchoredPosition = Vector2.zero;
        Color g = new Color(0.3f, 1f, 0.5f, 0.9f);
        AddRotatedDash(_flightPip, "PipL", g, new Vector2(-5f, 0f), new Vector2(15f, 3f), -135f);
        AddRotatedDash(_flightPip, "PipR", g, new Vector2(5f, 0f), new Vector2(15f, 3f), -45f);
        _flightPip.gameObject.SetActive(false);

        // Compact speed / altitude readout (top-left).
        var statGo = new GameObject("Stats");
        statGo.transform.SetParent(_canvas.transform, false);
        _statText = statGo.AddComponent<Text>();
        _statText.font = ResolveFont();
        _statText.alignment = TextAnchor.UpperLeft;
        _statText.fontSize = 18;
        _statText.color = new Color(1f, 1f, 1f, 0.85f);
        var statRt = _statText.rectTransform;
        statRt.anchorMin = statRt.anchorMax = new Vector2(0f, 1f);
        statRt.pivot = new Vector2(0f, 1f);
        statRt.sizeDelta = new Vector2(240f, 60f);
        statRt.anchoredPosition = new Vector2(12f, -12f);

        BuildArtificialHorizon();
        BuildRadar();
        BuildObjectiveMarker();
        BuildCombatHUD();
        LayoutStatic();
    }

    // A world-tracked objective tag (diamond icon + label) plus an edge arrow for when it's off-screen.
    private void BuildObjectiveMarker()
    {
        var mGo = new GameObject("ObjectiveMarker", typeof(RectTransform));
        mGo.transform.SetParent(_canvas.transform, false);
        _objMarker = mGo.GetComponent<RectTransform>();
        _objMarker.anchorMin = _objMarker.anchorMax = new Vector2(0.5f, 0.5f);
        _objMarker.pivot = new Vector2(0.5f, 0.5f);
        _objMarker.sizeDelta = new Vector2(120f, 60f);

        Color cyan = new Color(0.4f, 0.85f, 1f, 1f);
        _objIcon = AddChildImage(_objMarker, "ObjIcon", _square, cyan, new Vector2(20f, 20f), new Vector2(0f, 0f));
        _objIcon.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f); // diamond

        var lGo = new GameObject("ObjLabel");
        lGo.transform.SetParent(_objMarker, false);
        _objLabel = lGo.AddComponent<Text>();
        _objLabel.font = ResolveFont();
        _objLabel.alignment = TextAnchor.LowerCenter;
        _objLabel.fontSize = 20;
        _objLabel.color = cyan;
        _objLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        _objLabel.verticalOverflow = VerticalWrapMode.Overflow;
        var lrt = _objLabel.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0f);
        lrt.sizeDelta = new Vector2(160f, 24f);
        lrt.anchoredPosition = new Vector2(0f, 16f);

        var aGo = new GameObject("ObjectiveArrow", typeof(RectTransform));
        aGo.transform.SetParent(_canvas.transform, false);
        _objArrow = aGo.GetComponent<RectTransform>();
        _objArrow.anchorMin = _objArrow.anchorMax = new Vector2(0.5f, 0.5f);
        _objArrow.pivot = new Vector2(0.5f, 0.5f);
        _objArrow.sizeDelta = new Vector2(34f, 34f);
        _objArrowImg = aGo.AddComponent<Image>();
        _objArrowImg.sprite = _triangle; _objArrowImg.color = cyan; _objArrowImg.raycastTarget = false;

        _objMarker.gameObject.SetActive(false);
        _objArrow.gameObject.SetActive(false);
    }

    // Project the objective to screen each frame: on-screen = tag on it, off-screen = arrow at the edge.
    private void UpdateObjectiveMarker()
    {
        if (_objMarker == null) return;
        if (_objTarget == null)
        {
            if (_objMarker.gameObject.activeSelf) _objMarker.gameObject.SetActive(false);
            if (_objArrow != null && _objArrow.gameObject.activeSelf) _objArrow.gameObject.SetActive(false);
            return;
        }
        if (_lockCam == null) _lockCam = Camera.main;
        if (_lockCam == null) return;

        Vector3 sp = _lockCam.WorldToScreenPoint(_objTarget.position + Vector3.up * 6f);
        Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
        bool behind = sp.z < 0f;
        Vector2 scr = new Vector2(sp.x, sp.y);
        if (behind) scr = center - (scr - center); // mirror when the point is behind the camera
        bool onScreen = !behind && sp.x >= 0f && sp.x <= Screen.width && sp.y >= 0f && sp.y <= Screen.height;

        string pct = _objHealth != null ? $"  {Mathf.CeilToInt(_objHealth.HealthFraction * 100f)}%" : "";
        if (_objLabel != null) _objLabel.text = _objName + pct;

        if (onScreen)
        {
            _objMarker.gameObject.SetActive(true);
            _objArrow.gameObject.SetActive(false);
            _objMarker.anchoredPosition = new Vector2(sp.x - center.x, sp.y - center.y);
        }
        else
        {
            _objMarker.gameObject.SetActive(false);
            _objArrow.gameObject.SetActive(true);
            Vector2 dir = scr - center;
            if (dir.sqrMagnitude < 1f) dir = Vector2.up;
            dir.Normalize();
            float mx = Screen.width * 0.5f - 60f, my = Screen.height * 0.5f - 60f;
            float scale = Mathf.Min(mx / Mathf.Max(Mathf.Abs(dir.x), 0.0001f), my / Mathf.Max(Mathf.Abs(dir.y), 0.0001f));
            _objArrow.anchoredPosition = dir * scale;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _objArrow.localEulerAngles = new Vector3(0f, 0f, ang - 90f); // _triangle points +Y
        }
    }

    void OnEnable() { TargetHealth.OnAnyDamaged += HandleDamaged; MissionDirector.OnMissionEnd += HandleMissionEnd; }
    void OnDisable() { TargetHealth.OnAnyDamaged -= HandleDamaged; MissionDirector.OnMissionEnd -= HandleMissionEnd; }

    // Hide the whole gameplay HUD when the results screen takes over.
    private void HandleMissionEnd(bool victory) { if (_canvas != null) _canvas.gameObject.SetActive(false); }

    private void HandleDamaged(TargetHealth victim, int attackerTeam, float amount, bool fromPlayer)
    {
        if (_ownHealth == null) _ownHealth = GetComponent<TargetHealth>();
        if (_ownHealth != null && victim == _ownHealth) { _flashUntil = Time.time + 0.25f; CameraController.Shake(0.35f); } // I got hit
        if (fromPlayer && victim != _ownHealth) _hitmarkerUntil = Time.time + 0.12f; // my own hit connected
    }

    private void BuildCombatHUD()
    {
        // Full-screen red damage flash.
        _damageFlash = NewImage("DamageFlash", _square, new Color(1f, 0f, 0f, 0f));
        _damageFlash.anchorMin = Vector2.zero; _damageFlash.anchorMax = Vector2.one;
        _damageFlash.offsetMin = Vector2.zero; _damageFlash.offsetMax = Vector2.zero;
        _damageFlash.SetAsFirstSibling(); // behind other HUD

        // Player HP bar (bottom-left).
        var bg = NewImage("HpBarBg", _square, new Color(0f, 0f, 0f, 0.5f));
        bg.anchorMin = bg.anchorMax = Vector2.zero; bg.pivot = Vector2.zero;
        bg.sizeDelta = new Vector2(_hpBarWidth, 20f); bg.anchoredPosition = new Vector2(14f, 14f);

        var fillGo = new GameObject("HpBarFill");
        fillGo.transform.SetParent(bg, false);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.sprite = _square; fillImg.color = new Color(0.3f, 1f, 0.4f, 0.95f); fillImg.raycastTarget = false;
        _hpBarFill = fillImg.rectTransform;
        _hpBarFill.anchorMin = new Vector2(0f, 0f); _hpBarFill.anchorMax = new Vector2(0f, 1f);
        _hpBarFill.pivot = new Vector2(0f, 0.5f);
        _hpBarFill.sizeDelta = new Vector2(_hpBarWidth, 0f);
        _hpBarFill.anchoredPosition = Vector2.zero;

        // Ammo text (bottom-right, near the fire buttons).
        var ammoGo = new GameObject("Ammo"); ammoGo.transform.SetParent(_canvas.transform, false);
        _ammoText = ammoGo.AddComponent<Text>(); _ammoText.font = ResolveFont();
        _ammoText.alignment = TextAnchor.LowerRight; _ammoText.fontSize = 20; _ammoText.color = Color.white;
        var art = _ammoText.rectTransform;
        art.anchorMin = art.anchorMax = new Vector2(1f, 0f); art.pivot = new Vector2(1f, 0f);
        art.sizeDelta = new Vector2(220f, 60f); art.anchoredPosition = new Vector2(-14f, 14f);

        // Hitmarker (center X, shown briefly on a hit).
        var hm = new GameObject("Hitmarker", typeof(RectTransform)); hm.transform.SetParent(_canvas.transform, false);
        _hitmarker = hm.GetComponent<RectTransform>();
        _hitmarker.anchorMin = _hitmarker.anchorMax = new Vector2(0.5f, 0.5f); _hitmarker.pivot = new Vector2(0.5f, 0.5f);
        _hitmarker.sizeDelta = new Vector2(40f, 40f);
        Color hc = new Color(1f, 1f, 1f, 0.95f);
        AddRotatedDash(_hitmarker, "HM1", hc, new Vector2(-12f, 12f), new Vector2(14f, 3f), -45f);
        AddRotatedDash(_hitmarker, "HM2", hc, new Vector2(12f, 12f), new Vector2(14f, 3f), 45f);
        AddRotatedDash(_hitmarker, "HM3", hc, new Vector2(-12f, -12f), new Vector2(14f, 3f), 45f);
        AddRotatedDash(_hitmarker, "HM4", hc, new Vector2(12f, -12f), new Vector2(14f, 3f), -45f);
        _hitmarker.gameObject.SetActive(false);
    }

    private void UpdateCombatHUD()
    {
        if (_ownHealth == null) _ownHealth = GetComponent<TargetHealth>();

        if (_hpBarFill != null && _ownHealth != null)
        {
            float f = _ownHealth.HealthFraction;
            _hpBarFill.sizeDelta = new Vector2(_hpBarWidth * f, 0f);
            var img = _hpBarFill.GetComponent<Image>();
            if (img != null) img.color = Color.Lerp(new Color(1f, 0.25f, 0.25f, 0.95f), new Color(0.3f, 1f, 0.4f, 0.95f), f);
        }

        if (_damageFlash != null)
        {
            var img = _damageFlash.GetComponent<Image>();
            if (img != null) img.color = new Color(1f, 0f, 0f, Mathf.Clamp01((_flashUntil - Time.time) / 0.25f) * 0.35f);
        }

        if (_hitmarker != null)
        {
            bool show = Time.time < _hitmarkerUntil;
            if (_hitmarker.gameObject.activeSelf != show) _hitmarker.gameObject.SetActive(show);
        }

        if (_ammoText != null)
        {
            string a = (_ownWeapon != null && _ownWeapon.HasAmmoSystem)
                ? (_ownWeapon.IsReloading
                    ? $"RELOADING…\nRKT {_ownWeapon.Rockets}"
                    : $"AMMO {_ownWeapon.BulletsInMag}/{_ownWeapon.BulletMagSize}\nRKT {_ownWeapon.Rockets}")
                : "";
            if (a != _lastAmmo) { _ammoText.text = a; _lastAmmo = a; } // only touch the canvas on change
        }
    }

    private void BuildRadar()
    {
        if (!showRadar) return;
        var rGo = new GameObject("Radar", typeof(RectTransform));
        rGo.transform.SetParent(_canvas.transform, false);
        _radarRoot = rGo.GetComponent<RectTransform>();
        _radarRoot.anchorMin = _radarRoot.anchorMax = new Vector2(0f, 1f); // top-left corner
        _radarRoot.pivot = new Vector2(0.5f, 0.5f);
        float d = radarPixelRadius * 2f;
        _radarRoot.sizeDelta = new Vector2(d, d);
        _radarRoot.anchoredPosition = new Vector2(radarPixelRadius + 16f, -(92f + radarPixelRadius));

        AddChildImage(_radarRoot, "RadarBg", _circle, new Color(0f, 0f, 0f, 0.35f), new Vector2(d, d), Vector2.zero);
        AddChildDash(_radarRoot, "RadarFwd", new Color(1f, 1f, 1f, 0.5f), new Vector2(0f, radarPixelRadius - 6f), new Vector2(3f, 10f));
        // "You" marker: an upward triangle (points to your heading), not a dot.
        AddChildImage(_radarRoot, "RadarSelf", _triangle, new Color(0.3f, 1f, 0.5f, 1f), new Vector2(13f, 13f), Vector2.zero);
    }

    private Image AddChildImage(RectTransform parent, string name, Sprite sprite, Color c, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite; img.color = c; img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        return img;
    }

    private void UpdateRadar()
    {
        if (_radarRoot == null) return;

        if (Input.GetKeyDown(radarTerrainToggle)) showRadarTerrain = !showRadarTerrain;

        // Flattened heading basis is needed by the terrain/outpost passes too — compute once up front.
        Vector3 fwdB = transform.forward; fwdB.y = 0f;
        if (fwdB.sqrMagnitude < 0.0001f) fwdB = Vector3.forward;
        fwdB.Normalize();
        Vector3 rightB = Vector3.Cross(Vector3.up, fwdB);
        DrawTerrain(fwdB, rightB);
        DrawOutposts(fwdB, rightB);

        if (Time.time >= _nextRadarScan)
        {
            _nextRadarScan = Time.time + 0.1f;
            _radarTargets.Clear();
            Vector3 origin = transform.position;
            var all = TargetHealth.All; // registry, not a per-scan FindObjectsByType allocation
            for (int i = 0; i < all.Count; i++)
            {
                var t = all[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                if (t.transform.root == transform.root) continue;
                if (Vector3.Distance(origin, t.transform.position) > radarRange) continue;
                _radarTargets.Add(t.transform);
            }
        }

        // Flattened heading basis: radar up (+Y) = drone forward, radar right (+X) = drone's right.
        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        Transform lockT = _targeting != null ? _targeting.CurrentTarget : null;

        for (int i = 0; i < _radarTargets.Count; i++)
        {
            Transform t = _radarTargets[i];
            if (t == null) { GetBlip(i).gameObject.SetActive(false); continue; } // target was destroyed
            Vector3 rel = t.position - transform.position;
            Vector2 p = new Vector2(Vector3.Dot(rel, right), Vector3.Dot(rel, fwd)) / radarRange * radarPixelRadius;
            if (p.magnitude > radarPixelRadius) p = p.normalized * radarPixelRadius;

            Image b = GetBlip(i);
            b.gameObject.SetActive(true);
            b.rectTransform.anchoredPosition = p;
            bool locked = t == lockT;
            var bth = t.GetComponent<TargetHealth>();
            bool ally = bth != null && _ownHealth != null && bth.team == _ownHealth.team;
            b.color = locked ? new Color(1f, 1f, 0.3f, 1f)
                     : (ally ? new Color(0.3f, 0.7f, 1f, 0.9f) : new Color(1f, 0.3f, 0.3f, 0.9f));
            b.rectTransform.sizeDelta = locked ? new Vector2(12f, 12f) : new Vector2(8f, 8f);
        }
        for (int i = _radarTargets.Count; i < _radarBlips.Count; i++)
            _radarBlips[i].gameObject.SetActive(false);
    }

    private Image GetBlip(int i)
    {
        while (_radarBlips.Count <= i)
            _radarBlips.Add(AddChildImage(_radarRoot, "Blip", _circle, Color.red, new Vector2(8f, 8f), Vector2.zero));
        return _radarBlips[i];
    }

    // Outposts as owner-colored squares (white neutral / blue yours / red enemy).
    private void DrawOutposts(Vector3 fwd, Vector3 right)
    {
        var outposts = Outpost.All; // registry, not a per-frame FindObjectsByType allocation
        int used = 0;
        for (int i = 0; i < outposts.Count; i++)
        {
            var o = outposts[i];
            if (o == null) continue;
            Vector3 rel = o.transform.position - transform.position;
            if (rel.magnitude > radarRange * 1.2f) continue;
            Vector2 p = new Vector2(Vector3.Dot(rel, right), Vector3.Dot(rel, fwd)) / radarRange * radarPixelRadius;
            if (p.magnitude > radarPixelRadius) p = p.normalized * radarPixelRadius;

            Image b = GetOutpostBlip(used++);
            b.gameObject.SetActive(true);
            b.rectTransform.anchoredPosition = p;
            b.color = o.OwnerTeam == 1 ? new Color(0.3f, 0.62f, 1f, 1f)
                    : o.OwnerTeam == 2 ? new Color(1f, 0.3f, 0.28f, 1f)
                    : new Color(0.92f, 0.92f, 0.95f, 0.95f);
        }
        for (int i = used; i < _outpostBlips.Count; i++) _outpostBlips[i].gameObject.SetActive(false);
    }

    private Image GetOutpostBlip(int i)
    {
        while (_outpostBlips.Count <= i)
            _outpostBlips.Add(AddChildImage(_radarRoot, "OutpostBlip", _square, Color.white, new Vector2(11f, 11f), Vector2.zero));
        return _outpostBlips[i];
    }

    // Faint map footprints (towers/walls/props) — cached once, cheap to plot. Toggle with the terrain key.
    private void DrawTerrain(Vector3 fwd, Vector3 right)
    {
        if (!showRadarTerrain)
        {
            for (int i = 0; i < _terrainBlips.Count; i++) _terrainBlips[i].gameObject.SetActive(false);
            return;
        }
        if (_terrainPoints == null) CacheTerrainPoints();

        int used = 0;
        for (int i = 0; i < _terrainPoints.Count && used < 160; i++)
        {
            Vector3 rel = _terrainPoints[i] - transform.position;
            if (rel.magnitude > radarRange) continue;
            Vector2 p = new Vector2(Vector3.Dot(rel, right), Vector3.Dot(rel, fwd)) / radarRange * radarPixelRadius;
            if (p.magnitude > radarPixelRadius) continue; // clip terrain to the disc (don't ring it)

            Image b = GetTerrainBlip(used++);
            b.gameObject.SetActive(true);
            b.rectTransform.anchoredPosition = p;
        }
        for (int i = used; i < _terrainBlips.Count; i++) _terrainBlips[i].gameObject.SetActive(false);
    }

    private Image GetTerrainBlip(int i)
    {
        while (_terrainBlips.Count <= i)
            _terrainBlips.Add(AddChildImage(_radarRoot, "TerrainBlip", _square, new Color(0.7f, 0.72f, 0.75f, 0.45f), new Vector2(4f, 4f), Vector2.zero));
        return _terrainBlips[i];
    }

    // Sample the generated map's larger props once (skip tiny bits) so the terrain layer is cheap.
    private void CacheTerrainPoints()
    {
        _terrainPoints = new System.Collections.Generic.List<Vector3>();
        var park = Object.FindFirstObjectByType<ParkMapGenerator>();
        if (park == null) return;
        foreach (var r in park.GetComponentsInChildren<Renderer>())
        {
            Vector3 s = r.transform.lossyScale;
            if (Mathf.Max(s.x, s.z) < 1.5f) continue; // ignore thin posts / clutter
            _terrainPoints.Add(r.transform.position);
        }
    }

    private void BuildArtificialHorizon()
    {
        if (!showHorizon) return;

        // Fixed reference "wings" (aircraft waterline) — two dashes fixed at screen center.
        Color refC = new Color(1f, 1f, 1f, 0.7f);
        MakeCenterDash("Ref_L", refC, new Vector2(-34f, 0f));
        MakeCenterDash("Ref_R", refC, new Vector2(34f, 0f));

        // Moving horizon bar: two dashes with a center gap; rolls with bank, shifts with pitch.
        var hGo = new GameObject("Horizon", typeof(RectTransform));
        hGo.transform.SetParent(_canvas.transform, false);
        _horizon = hGo.GetComponent<RectTransform>();
        _horizon.anchorMin = _horizon.anchorMax = new Vector2(0.5f, 0.5f);
        _horizon.pivot = new Vector2(0.5f, 0.5f);
        _horizon.sizeDelta = new Vector2(240f, 20f);
        _horizon.anchoredPosition = Vector2.zero;

        Color hc = new Color(0.6f, 0.9f, 1f, 0.55f);
        AddChildDash(_horizon, "H_L", hc, new Vector2(-80f, 0f), new Vector2(90f, 3f));
        AddChildDash(_horizon, "H_R", hc, new Vector2(80f, 0f), new Vector2(90f, 3f));
    }

    private void MakeCenterDash(string name, Color c, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_canvas.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite = _square; img.color = c; img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(24f, 3f);
        rt.anchoredPosition = pos;
    }

    private void AddChildDash(RectTransform parent, string name, Color c, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = _square; img.color = c; img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    private void AddRotatedDash(RectTransform parent, string name, Color c, Vector2 pos, Vector2 size, float angleZ)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = _square; img.color = c; img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.localEulerAngles = new Vector3(0f, 0f, angleZ);
    }

    private void UpdateHorizon()
    {
        if (_horizon == null) return;
        float roll = Mathf.DeltaAngle(0f, transform.eulerAngles.z);
        float pitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
        _horizon.localEulerAngles = new Vector3(0f, 0f, roll);
        float y = Mathf.Clamp(-pitch * horizonPixelsPerDegree, -Screen.height * 0.42f, Screen.height * 0.42f);
        _horizon.anchoredPosition = new Vector2(0f, y);
    }

    /// <summary>Flight-vector pip + speed/altitude. (Mini-radar + horizon come in the instruments pass.)</summary>
    private void UpdateInstruments()
    {
        if (_lockCam == null) _lockCam = Camera.main;

        if (_flightPip != null && _lockCam != null)
        {
            Vector3 dir = (_droneRb != null && _droneRb.linearVelocity.sqrMagnitude > 4f)
                ? _droneRb.linearVelocity.normalized : transform.forward;
            Vector3 sp = _lockCam.WorldToScreenPoint(transform.position + dir * 500f);
            bool on = sp.z > 0f && sp.x >= 0f && sp.x <= Screen.width && sp.y >= 0f && sp.y <= Screen.height;
            if (_flightPip.gameObject.activeSelf != on) _flightPip.gameObject.SetActive(on);
            if (on) _flightPip.anchoredPosition = new Vector2(sp.x - Screen.width * 0.5f, sp.y - Screen.height * 0.5f);
        }

        if (_statText != null)
        {
            float spd = _droneRb != null ? _droneRb.linearVelocity.magnitude : 0f;
            string hp = _ownHealth != null ? $"\nHP {Mathf.Ceil(_ownHealth.HealthFraction * 100f):0}%" : "";
            string stat = $"SPD {spd:0} m/s\nALT {transform.position.y:0} m{hp}";
            if (stat != _lastStat) { _statText.text = stat; _lastStat = stat; } // integer-rounded → changes rarely
        }

        UpdateHorizon();
        UpdateRadar();
        UpdateObjectiveMarker();
        UpdateCombatHUD();
    }

    /// <summary>Crosshair snaps onto an on-screen lock; if the lock is off-screen, show an edge arrow toward it.</summary>
    private void UpdateAimReticle()
    {
        Transform tgt = _targeting != null ? _targeting.CurrentTarget : null;
        bool showCross = true, showArrow = false;
        Vector2 crossPos = Vector2.zero;
        float crossScale = 1f;

        if (tgt != null)
        {
            if (_lockCam == null) _lockCam = Camera.main;
            if (_lockCam != null)
            {
                Vector3 sp = _lockCam.WorldToScreenPoint(tgt.position);
                bool onScreen = sp.z > 0f && sp.x >= 0f && sp.x <= Screen.width && sp.y >= 0f && sp.y <= Screen.height;
                if (onScreen)
                {
                    crossPos = new Vector2(sp.x - Screen.width * 0.5f, sp.y - Screen.height * 0.5f);
                    float dist = Vector3.Distance(transform.position, tgt.position);
                    crossScale = Mathf.Clamp(crosshairRefDistance / Mathf.Max(dist, 1f), crosshairMinScale, crosshairMaxScale);
                }
                else
                {
                    Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    Vector2 s = new Vector2(sp.x, sp.y);
                    if (sp.z < 0f) s = 2f * center - s; // behind camera -> mirror to correct side
                    Vector2 d = s - center;
                    if (d.sqrMagnitude < 1f) d = Vector2.up;
                    float halfW = Screen.width * 0.5f - 40f, halfH = Screen.height * 0.5f - 40f;
                    float sx = Mathf.Abs(d.x) > 0.0001f ? halfW / Mathf.Abs(d.x) : float.PositiveInfinity;
                    float sy = Mathf.Abs(d.y) > 0.0001f ? halfH / Mathf.Abs(d.y) : float.PositiveInfinity;
                    Vector2 edge = d * Mathf.Min(sx, sy);
                    if (_offscreenArrow != null)
                    {
                        _offscreenArrow.anchoredPosition = edge;
                        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                        _offscreenArrow.localEulerAngles = new Vector3(0f, 0f, ang - 90f); // triangle points +Y
                    }
                    showArrow = true; showCross = false;
                }
            }
        }

        if (_crosshair != null)
        {
            if (_crosshair.gameObject.activeSelf != showCross) _crosshair.gameObject.SetActive(showCross);
            if (showCross)
            {
                _crosshair.anchoredPosition = Vector2.Lerp(_crosshair.anchoredPosition, crossPos, Time.deltaTime * 14f);
                _crosshair.localScale = Vector3.one * crossScale;
            }
        }
        if (_offscreenArrow != null && _offscreenArrow.gameObject.activeSelf != showArrow)
            _offscreenArrow.gameObject.SetActive(showArrow);
    }

    private void SwitchTarget(int dir)
    {
        if (_targeting != null) _targeting.CycleTarget(dir);
    }

    /// <summary>A simple -o- reticle pinned to screen center (nose reference).</summary>
    private void BuildCrosshair()
    {
        if (!showCrosshair) return;
        var chGo = new GameObject("Crosshair", typeof(RectTransform));
        chGo.transform.SetParent(_canvas.transform, false);
        _crosshair = chGo.GetComponent<RectTransform>();
        _crosshair.anchorMin = _crosshair.anchorMax = new Vector2(0.5f, 0.5f); // screen center
        _crosshair.pivot = new Vector2(0.5f, 0.5f);
        _crosshair.sizeDelta = new Vector2(60f, 60f);
        _crosshair.anchoredPosition = Vector2.zero;

        // Corner-bracket square (scales with target distance in UpdateAimReticle).
        Color c = new Color(1f, 1f, 1f, 0.85f);
        const float e = 22f, len = 12f, th = 3f;
        Vector2[] corners = { new Vector2(-e, e), new Vector2(e, e), new Vector2(-e, -e), new Vector2(e, -e) };
        foreach (var corner in corners)
        {
            float sx = corner.x > 0 ? -1f : 1f;
            float sy = corner.y > 0 ? -1f : 1f;
            MakeReticlePart("CH_H", _square, c, new Vector2(len, th), new Vector2(corner.x + sx * len * 0.5f, corner.y));
            MakeReticlePart("CH_V", _square, c, new Vector2(th, len), new Vector2(corner.x, corner.y + sy * len * 0.5f));
        }
    }

    private void MakeReticlePart(string name, Sprite sprite, Color color, Vector2 size, Vector2 offset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_crosshair, false); // under the movable crosshair container
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }

    private string AimModeLabel()
    {
        bool free = _router != null && _router.Aim == FlightInputRouter.AimMode.FreeAim;
        return free ? "AIM: FREE" : "AIM: NOSE";
    }

    private RectTransform NewImage(string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_canvas.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero; // bottom-left, so anchoredPosition == pixel pos
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    private RectTransform NewButton(string name, string label, Color color, out Text labelText)
    {
        var rt = NewImage(name, _square, color);
        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(rt, false);
        labelText = txtGo.AddComponent<Text>();
        labelText.font = ResolveFont();
        labelText.text = label;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.fontSize = 15;
        labelText.color = Color.white;
        var trt = labelText.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        return rt;
    }

    /// <summary>Position and size the fixed (non-stick) elements from their normalized rects.</summary>
    private void LayoutStatic()
    {
        SetRect(_schemeRt, schemeButton);
        SetRect(_camRt, camButton);
        SetRect(_restartRt, restartButton);
        SetRect(_aimModeRt, aimModeButton);
        SetRect(_primaryRt, primaryFireButton);
        SetRect(_secondaryRt, secondaryFireButton);
        SetRect(_switchRt, switchTargetButton);
        SetRect(_throttleTrack, throttleRegion);
    }

    private void SetRect(RectTransform rt, Rect norm)
    {
        if (rt == null) return;
        Rect p = PixelRect(norm);
        rt.sizeDelta = new Vector2(p.width, p.height);
        rt.anchoredPosition = new Vector2(p.center.x, p.center.y);
    }

    private void UpdateThrottleVisual(float display)
    {
        if (_throttleKnob == null) return;
        Rect p = PixelRect(throttleRegion);
        float knobH = p.height * 0.14f;
        _throttleKnob.sizeDelta = new Vector2(p.width, knobH);
        float t01 = (display + 1f) * 0.5f; // -1..1 -> 0..1
        float y = Mathf.Lerp(p.yMin + knobH * 0.5f, p.yMax - knobH * 0.5f, t01);
        _throttleKnob.anchoredPosition = new Vector2(p.center.x, y);
        // Tint: green forward, red reverse, neutral at center.
        var img = _throttleKnob.GetComponent<Image>();
        if (img != null)
            img.color = display >= 0f
                ? Color.Lerp(new Color(0.6f, 0.9f, 1f, 0.5f), new Color(0.3f, 1f, 0.4f, 0.6f), display)
                : Color.Lerp(new Color(0.6f, 0.9f, 1f, 0.5f), new Color(1f, 0.4f, 0.4f, 0.6f), -display);
    }

    private void PlaceStick(RectTransform baseRt, RectTransform knobRt, Vector2 origin, Vector2 v, float radius, bool visible)
    {
        if (baseRt == null) return;
        baseRt.gameObject.SetActive(visible);
        knobRt.gameObject.SetActive(visible);
        if (!visible) return;
        baseRt.sizeDelta = Vector2.one * radius * 2f;
        knobRt.sizeDelta = Vector2.one * radius * 0.9f;
        baseRt.anchoredPosition = origin;
        knobRt.anchoredPosition = origin + v * radius;
    }

    // -------------------------------------------------------------------------------------------
    // Tiny procedural sprites so no art assets are needed for the prototype.
    // -------------------------------------------------------------------------------------------
    private Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f, edge = 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
            float a = Mathf.Clamp01((r - d) / edge);
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite MakeSquareSprite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    // Filled triangle pointing up (+Y); rotate the RectTransform to point it at an off-screen target.
    private Sprite MakeTriangleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 a = new Vector2(0.5f, 0.95f), b = new Vector2(0.08f, 0.05f), c = new Vector2(0.92f, 0.05f);
        var clear = new Color(1, 1, 1, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                tex.SetPixel(x, y, PointInTri(p, a, b, c) ? Color.white : clear);
            }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0;
        bool pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    private static float Cross(Vector2 p1, Vector2 p2, Vector2 p3)
        => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    private static Font _hudFont;
    private Font ResolveFont()
    {
        if (uiFont != null) return uiFont;
        // Playful rounded/comic OS font for the whole HUD (Windows has Comic Sans MS); bundle a font for production.
        if (_hudFont == null)
            _hudFont = Font.CreateDynamicFontFromOSFont(new[] { "Comic Sans MS", "Baloo 2", "Chalkboard SE", "Verdana" }, 24);
        if (_hudFont != null) return _hudFont;
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        return f;
    }
}
