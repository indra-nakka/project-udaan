using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// The demo scene's single entry point. Builds a runtime Start menu (name, difficulty, volume, Play)
/// and a Pause overlay from code (same code-first pattern as the rest of the project), then launches
/// gameplay by spawning <see cref="SinglePlayerBootstrap"/>. Put this on one empty GameObject in the
/// Demo scene and assign the Drone_Player prefab (the editor menu "Udaan/Create or Update Demo Scene"
/// does this for you).
/// </summary>
public class DemoFlow : MonoBehaviour
{
    [Tooltip("Assign Assets/Prefabs/Drone_Player.prefab")]
    public GameObject dronePrefab;

    [Header("Faction models (auto-assigned by 'Create Demo Scene'; tune scales here)")]
    public GameObject enemyModel, allyModel, bossModel;   // FBX from Assets/Art/Models
    public float enemyScale = 0.35f, allyScale = 0.35f, bossScale = 0.4f;
    public Vector3 enemyEuler, allyEuler, bossEuler;      // rotation nudge if a model's nose faces wrong

    [Header("Park models (auto-assigned by 'Create Demo Scene'; null = greybox fallback)")]
    public GameObject treeModel, slideModel, swingModel, gymModel, sandboxModel, seesawModel;
    public GameObject playsetModel, merryModel, domeModel, tyreSwingModel;
    public GameObject rockWallModel, tyreWallModel, trampolineModel, benchModel, animalMerryModel;

    private enum State { Menu, Playing, Paused, Results }
    private State _state = State.Menu;

    private Canvas _canvas;
    private GameObject _startMenu, _pauseMenu, _gameplay, _results;
    private Font _font;
    private Button[] _diffButtons = new Button[4];
    private static readonly Color Sel = new Color(0.30f, 0.62f, 1f, 1f);
    private static readonly Color Unsel = new Color(0.25f, 0.25f, 0.3f, 1f);

    void OnEnable()  { MissionDirector.OnMissionEnd += ShowResults; }
    void OnDisable() { MissionDirector.OnMissionEnd -= ShowResults; }

    void Start()
    {
        Time.timeScale = 1f;
        GameConfig.Load();
        FactionVisuals.Enemy = enemyModel; FactionVisuals.Ally = allyModel; FactionVisuals.Boss = bossModel;
        FactionVisuals.EnemyScale = enemyScale; FactionVisuals.AllyScale = allyScale; FactionVisuals.BossScale = bossScale;
        FactionVisuals.EnemyEuler = enemyEuler; FactionVisuals.AllyEuler = allyEuler; FactionVisuals.BossEuler = bossEuler;
        ParkProps.Tree = treeModel; ParkProps.Slide = slideModel; ParkProps.Swing = swingModel;
        ParkProps.Gym = gymModel; ParkProps.Sandbox = sandboxModel; ParkProps.Seesaw = seesawModel;
        ParkProps.Playset = playsetModel; ParkProps.Merry = merryModel; ParkProps.Dome = domeModel; ParkProps.TyreSwing = tyreSwingModel;
        ParkProps.RockWall = rockWallModel; ParkProps.TyreWall = tyreWallModel; ParkProps.Trampoline = trampolineModel; ParkProps.Bench = benchModel; ParkProps.AnimalMerry = animalMerryModel;
        Music.Menu();
        _font = UIFont();
        EnsureEventSystem();
        BuildCanvas();
        BuildStartMenu();
        BuildPauseMenu();
        _pauseMenu.SetActive(false);

        if (GameConfig.AutoPlay)               // came from Restart → skip the menu
        {
            GameConfig.AutoPlay = false;
            _startMenu.SetActive(false);
            Play();
        }
        else { _startMenu.SetActive(true); _state = State.Menu; }
    }

    void Update()
    {
        if (_state == State.Playing && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))) Pause();
        else if (_state == State.Paused && Input.GetKeyDown(KeyCode.Escape)) Resume();
    }

    // ---- flow ----
    private void Play()
    {
        GameConfig.Save();
        Music.Battle();
        _startMenu.SetActive(false);
        _pauseMenu.SetActive(false);
        if (_gameplay == null)
        {
            _gameplay = new GameObject("Gameplay");
            var boot = _gameplay.AddComponent<SinglePlayerBootstrap>();
            boot.dronePrefab = dronePrefab;   // defaults: missionMode + enemies + map (the combat demo)
        }
        _state = State.Playing;
    }

    private void Pause()  { _state = State.Paused;  Time.timeScale = 0f; _pauseMenu.SetActive(true); }
    private void Resume() { _state = State.Playing; Time.timeScale = 1f; _pauseMenu.SetActive(false); }
    private void Restart() { Time.timeScale = 1f; GameConfig.AutoPlay = true;  Reload(); }
    private void ToMenu()  { Time.timeScale = 1f; GameConfig.AutoPlay = false; Reload(); }
    private void Reload()  { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

    private void Quit()
    {
        GameConfig.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetDifficulty(MissionDirector.Difficulty d) { GameConfig.Difficulty = d; RefreshDiff(); }
    private void RefreshDiff()
    {
        for (int i = 0; i < _diffButtons.Length; i++)
            if (_diffButtons[i] != null)
                _diffButtons[i].GetComponent<Image>().color = ((int)GameConfig.Difficulty == i) ? Sel : Unsel;
    }
    // ---- UI construction ----
    private void BuildCanvas()
    {
        var go = new GameObject("DemoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void BuildStartMenu()
    {
        _startMenu = Panel("StartMenu", new Color(0.05f, 0.06f, 0.09f, 0.98f));
        var rt = _startMenu.GetComponent<RectTransform>();

        Label(rt, "UDAAN", 92, new Vector2(0, 268), new Vector2(900, 130), new Color(0.4f, 0.85f, 1f), TextAnchor.MiddleCenter, true);

        // stacked, centered rows — labels ABOVE controls so nothing clips
        Label(rt, "Pilot name", 24, new Vector2(0, 150), new Vector2(360, 36), new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter);
        MakeInput(rt, new Vector2(0, 110), new Vector2(360, 58), GameConfig.PlayerName);

        Label(rt, "Difficulty", 24, new Vector2(0, 54), new Vector2(360, 36), new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter);
        string[] names = { "Easy", "Medium", "Hard", "Pain" };
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            _diffButtons[i] = Button(rt, names[i], new Vector2(-198 + i * 132, 8), new Vector2(120, 50),
                                     () => SetDifficulty((MissionDirector.Difficulty)idx));
        }
        RefreshDiff();

        // Level selector — one level for now, shown as the chosen pill.
        Label(rt, "Level", 24, new Vector2(0, -52), new Vector2(360, 36), new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter);
        var lvl = Button(rt, "Children's Park", new Vector2(0, -94), new Vector2(300, 52), null, 26);
        lvl.GetComponent<Image>().color = Sel;

        var play = Button(rt, "PLAY", new Vector2(0, -172), new Vector2(300, 78), Play, 38);
        play.GetComponent<Image>().color = new Color(0.20f, 0.70f, 0.35f);   // inviting green
        Button(rt, "Quit", new Vector2(0, -256), new Vector2(150, 46), Quit, 22);
    }

    // Kid-friendly victory/defeat screen (fired by MissionDirector.OnMissionEnd).
    private void ShowResults(bool victory)
    {
        Time.timeScale = 0f;   // freeze the field behind the screen
        Music.Menu();          // ease back to the calm theme over the results screen
        _state = State.Results;
        var stats = MissionStats.Active;
        Font pf = PlayfulFont();

        if (_results != null) Destroy(_results);
        _results = Panel("Results", new Color(0.05f, 0.06f, 0.09f, 0.94f));
        var rt = _results.GetComponent<RectTransform>();

        Label(rt, victory ? "YOU DID IT!" : "OOPS!", 88, new Vector2(0, 195), new Vector2(1000, 130),
              victory ? new Color(0.4f, 0.9f, 0.5f) : new Color(1f, 0.6f, 0.3f), TextAnchor.MiddleCenter, true, pf);

        if (victory)
        {
            int stars = Mathf.Clamp(3 - (stats != null ? stats.livesUsed : 0), 1, 3);
            BuildStars(rt, stars);   // 3 real star icons in 3 slots
        }

        // personalized, varied outcome line (replayability)
        Label(rt, OutcomeMessage(victory, stats), 32, new Vector2(0, victory ? -5 : 70), new Vector2(1100, 50),
              new Color(1, 1, 1, 0.92f), TextAnchor.MiddleCenter, false, pf);

        if (stats != null)
            Label(rt, $"Time  {Fmt(Time.time - stats.startTime)}", 22, new Vector2(0, victory ? -50 : 20),
                  new Vector2(400, 34), new Color(1, 1, 1, 0.55f), TextAnchor.MiddleCenter);

        Button(rt, "Play Again", new Vector2(0, -105), new Vector2(300, 70), Restart, 32);
        Button(rt, "Main Menu", new Vector2(0, -188), new Vector2(300, 70), ToMenu, 32);
    }

    private void BuildStars(RectTransform parent, int stars)
    {
        if (_starSprite == null) _starSprite = MakeStarSprite(72);
        const float spacing = 92f;
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("Star", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchoredPosition = new Vector2(-spacing + i * spacing, 92); rt.sizeDelta = new Vector2(80, 80);
            var img = go.GetComponent<Image>();
            img.sprite = _starSprite; img.raycastTarget = false;
            img.color = i < stars ? new Color(1f, 0.85f, 0.2f) : new Color(0.28f, 0.28f, 0.33f);
            go.AddComponent<StarPop>().Init(0.15f + i * 0.14f);   // staggered bouncy pop-in
        }
    }

    // Personalized outcome messages (a small dictionary with variants → replayability + a personal touch).
    private string OutcomeMessage(bool victory, MissionStats s)
    {
        string name = string.IsNullOrWhiteSpace(GameConfig.PlayerName) ? "Pilot" : GameConfig.PlayerName;
        string[] pool;
        if (!victory)
        {
            bool core = s != null && !string.IsNullOrEmpty(s.defeatReason) && s.defeatReason.Contains("Core");
            pool = core
                ? new[] { $"The Core fell, {name}. Give it another go!", $"So close to holding on, {name}!", $"Shake it off, {name} — try again!" }
                : new[] { $"Down but not out, {name}!", $"Dust yourself off, {name}!", $"One more try, {name}!" };
        }
        else
        {
            int lives = s != null ? s.livesUsed : 0;
            float dmg = s != null ? s.damageTaken : 0f;
            if (lives == 0 && dmg < 80f) pool = new[] { $"FLAWLESS, {name}! Not a scratch!", $"Perfect flying, {name}!", $"Untouchable, {name}!" };
            else if (lives == 0)         pool = new[] { $"Nailed it, {name}!", $"Great flying, {name}!", $"The sky is yours, {name}!" };
            else                         pool = new[] { $"Close one, {name}!", $"Phew — you made it, {name}!", $"Hard-won, {name}. Well flown!" };
        }
        return pool[Random.Range(0, pool.Length)];
    }

    private static string Fmt(float t) { int m = (int)(t / 60f); int s = (int)(t - m * 60f); return $"{m:00}:{s:00}"; }

    private static Font _playful;
    private static Font PlayfulFont()
    {
        if (_playful != null) return _playful;
        // A rounded/comic OS font if present (Windows has Comic Sans MS); production should bundle a font asset.
        _playful = Font.CreateDynamicFontFromOSFont(new[] { "Comic Sans MS", "Baloo 2", "Chalkboard SE", "Verdana" }, 40);
        return _playful;
    }

    private Sprite _starSprite;
    private static Sprite MakeStarSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float outR = size * 0.5f, inR = size * 0.26f;   // chunky cartoon star (visible points, not a pentagon)
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.Deg2Rad * (-90f + i * 36f);
            float r = (i % 2 == 0) ? outR : inR;
            pts[i] = c + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = PointInPoly(new Vector2(x + 0.5f, y + 0.5f), pts);
                tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static bool PointInPoly(Vector2 p, Vector2[] v)
    {
        bool inside = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            if (((v[i].y > p.y) != (v[j].y > p.y)) &&
                (p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x))
                inside = !inside;
        return inside;
    }

    private void BuildPauseMenu()
    {
        _pauseMenu = Panel("PauseMenu", new Color(0.03f, 0.04f, 0.06f, 0.7f));
        var rt = _pauseMenu.GetComponent<RectTransform>();
        Label(rt, "PAUSED", 64, new Vector2(0, 150), new Vector2(600, 100), Color.white, TextAnchor.MiddleCenter, true);
        Button(rt, "Resume", new Vector2(0, 40), new Vector2(260, 64), Resume, 30);
        Button(rt, "Restart", new Vector2(0, -40), new Vector2(260, 64), Restart, 30);
        Button(rt, "Main Menu", new Vector2(0, -120), new Vector2(260, 64), ToMenu, 30);
    }

    // ---- helpers ----
    private GameObject Panel(string name, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_canvas.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = c;
        return go;
    }

    private Text Label(RectTransform parent, string s, int size, Vector2 pos, Vector2 sz, Color c, TextAnchor anchor, bool bold = false, Font font = null)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false); rt.anchoredPosition = pos; rt.sizeDelta = sz;
        var t = go.GetComponent<Text>();
        t.font = font != null ? font : _font; t.fontSize = size; t.color = c; t.alignment = anchor;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.text = s;
        return t;
    }

    private Button Button(RectTransform parent, string s, Vector2 pos, Vector2 sz, System.Action onClick, int fontSize = 24)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false); rt.anchoredPosition = pos; rt.sizeDelta = sz;
        go.GetComponent<Image>().color = Unsel;
        var lbl = Label(rt, s, fontSize, Vector2.zero, sz, Color.white, TextAnchor.MiddleCenter);
        lbl.raycastTarget = false;
        var b = go.GetComponent<Button>();
        if (onClick != null) b.onClick.AddListener(() => onClick());
        return b;
    }

    private InputField MakeInput(RectTransform parent, Vector2 pos, Vector2 sz, string initial)
    {
        var go = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(InputField));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false); rt.anchoredPosition = pos; rt.sizeDelta = sz;
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);
        var input = go.GetComponent<InputField>();

        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var trt = txtGo.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12, 4); trt.offsetMax = new Vector2(-12, -4);
        var txt = txtGo.GetComponent<Text>();
        txt.font = _font; txt.fontSize = 28; txt.color = Color.black;
        txt.alignment = TextAnchor.MiddleLeft; txt.supportRichText = false;

        input.textComponent = txt;
        input.characterLimit = 12;
        input.text = initial;
        input.onValueChanged.AddListener(v => GameConfig.PlayerName = v);
        return input;
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static Font UIFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return f;
    }
}

/// <summary>Bouncy scale-in for the result stars (unscaled time — the results screen freezes the game).</summary>
public class StarPop : MonoBehaviour
{
    private float _delay, _t;
    private RectTransform _rt;

    public void Init(float delay)
    {
        _delay = delay;
        _rt = GetComponent<RectTransform>();
        if (_rt != null) _rt.localScale = Vector3.zero;
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        if (_t < _delay) return;
        float k = Mathf.Clamp01((_t - _delay) / 0.4f);
        if (_rt != null) _rt.localScale = Vector3.one * EaseOutBack(k);
        if (k >= 1f) enabled = false;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
