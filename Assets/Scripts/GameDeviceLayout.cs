using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-150)]
public class GameDeviceLayout : MonoBehaviour
{
    public GameObject touchControls;
    public RectTransform safeArea;
    public GameObject desktopHelp;
    public GameObject desktopControlsText;
    public GameObject desktopHintText;
    public CanvasScaler scaler;
    public RectTransform titlePanel;
    public RectTransform scorePanel;
    public static float HorizontalLimit { get; private set; } = 9.5f;
    Camera gameCamera;
    MovingPlatform2D[] platforms;
    Transform leftWall, rightWall;
    int lastWidth, lastHeight;
    Rect lastSafeArea;
    bool touchEnabled;
    bool buttonMode;
    const string ControlPreference = "MobileControlMode";
    public bool TouchEnabled => touchEnabled;
    public bool ButtonMode => buttonMode;

    void Awake()
    {
        gameCamera = Camera.main;
        platforms = FindObjectsByType<MovingPlatform2D>(FindObjectsSortMode.None);
        leftWall = GameObject.Find("Sol Sinir").transform;
        rightWall = GameObject.Find("Sag Sinir").transform;
    }
    void Start()
    {
        Application.targetFrameRate = 60;
        touchEnabled = Application.isMobilePlatform || Input.touchSupported;
        buttonMode = PlayerPrefs.GetInt(ControlPreference, 0) == 1;
        GameInput.Configure(touchEnabled, buttonMode);
        Apply();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) { touchEnabled = !touchEnabled; GameInput.Configure(touchEnabled, buttonMode); Apply(); }
        if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea) Apply();
    }
    void LateUpdate() { GameInput.UpdateGesture(); }

    public void SetTouchPreview(bool enabled)
    {
        touchEnabled = enabled;
        buttonMode = enabled;
        GameInput.Configure(touchEnabled, buttonMode);
        Apply();
    }

    public void SetButtonControls(bool enabled)
    {
        buttonMode = enabled;
        PlayerPrefs.SetInt(ControlPreference, enabled ? 1 : 0);
        PlayerPrefs.Save();
        GameInput.Configure(touchEnabled, buttonMode);
        Apply();
    }

    public void Apply()
    {
        lastWidth = Screen.width; lastHeight = Screen.height; lastSafeArea = Screen.safeArea;
        GameInput.Configure(touchEnabled, buttonMode);
        ApplyLayout(Screen.width, Screen.height, Screen.safeArea);
    }

    public void ApplyLayout(int width, int height, Rect area)
    {
        bool portrait = height > width;
        bool reserveForButtons = touchEnabled && buttonMode && portrait;
        gameCamera.rect = reserveForButtons ? new Rect(0, .16f, 1, .84f) : new Rect(0, 0, 1, 1);
        float aspect = (float)width / (height * gameCamera.rect.height);
        gameCamera.aspect = aspect;
        HorizontalLimit = Mathf.Clamp(gameCamera.orthographicSize * aspect - .3f, 1.7f, 9.5f);
        platforms = FindObjectsByType<MovingPlatform2D>(FindObjectsSortMode.None);
        foreach (var platform in platforms) platform.amplitude = Mathf.Clamp(HorizontalLimit - 1.3f, .35f, 2.9f);
        leftWall.position = new Vector3(-HorizontalLimit, leftWall.position.y, 0);
        rightWall.position = new Vector3(HorizontalLimit, rightWall.position.y, 0);
        var player = FindFirstObjectByType<PlayerController>();
        if (player)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = new Vector2(Mathf.Clamp(rb.position.x, -HorizontalLimit + .4f, HorizontalLimit - .4f), rb.position.y);
        }
        var sequence = FindFirstObjectByType<CoinSequence>();
        if (sequence) sequence.ClampCurrentCoin();
        var background = GameObject.Find("Mezarlik").GetComponent<SpriteRenderer>();
        float backgroundWidth = Mathf.Max(21.7f, 2 * gameCamera.orthographicSize * aspect);
        background.transform.localScale = new Vector3(backgroundWidth / background.sprite.bounds.size.x, 12.2f / background.sprite.bounds.size.y, 1);
        if (touchControls) touchControls.SetActive(touchEnabled && buttonMode);
        if (desktopHelp) desktopHelp.SetActive(!touchEnabled);
        if (desktopControlsText) desktopControlsText.SetActive(!touchEnabled);
        if (desktopHintText) desktopHintText.SetActive(!touchEnabled);
        safeArea.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
        safeArea.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        float referenceWidth = 1600f * Mathf.Sqrt(((float)width / 1600) / ((float)height / 900)) * area.width / width;
        if (titlePanel) titlePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(385, referenceWidth * .52f - 25));
        if (scorePanel) scorePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(260, referenceWidth * .35f));
    }
    void OnApplicationFocus(bool focused) { if (!focused) GameInput.Clear(); }
    void OnApplicationPause(bool paused) { if (paused) GameInput.Clear(); }
    void OnDisable() { GameInput.Clear(); }
}
