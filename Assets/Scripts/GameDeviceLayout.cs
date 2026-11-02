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
    bool touchEnabled, lastMenu, helpDismissed;
    public bool TouchEnabled => touchEnabled;

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
        // PC'de (WebGL dahil) joystick/butonlar görünmez; dokunmatik ekranlı bilgisayarlar da PC sayılır.
        // T tuşu elle değiştirir.
        touchEnabled = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
        if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;
        joystick = TouchJoystick.Create(this);
        var legacy = new System.Collections.Generic.List<GameObject>();
        foreach (var b in touchControls.GetComponentsInChildren<TouchControlButton>(true)) legacy.Add(b.gameObject);
        joystick.SetLegacyButtons(legacy.ToArray());
        Apply();
    }
    TouchJoystick joystick;
    public TouchJoystick Joystick => joystick;
    void Update()
    {
        if (!GameFlow.IsMenu && !helpDismissed && GameInput.HasGameplayInput) { helpDismissed=true; Apply(); }
        if (!touchEnabled && Input.touchCount > 0) { touchEnabled=true; Apply(); }
        if (Input.GetKeyDown(KeyCode.T)) { touchEnabled = !touchEnabled; GameInput.Clear(); Apply(); }
        if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea || lastMenu!=GameFlow.IsMenu) Apply();
    }

    public void SetTouchPreview(bool enabled) { touchEnabled = enabled; GameInput.Clear(); Apply(); }

    public void Apply()
    {
        lastWidth = Screen.width; lastHeight = Screen.height; lastSafeArea = Screen.safeArea;
        lastMenu=GameFlow.IsMenu;
        ApplyLayout(Screen.width, Screen.height, Screen.safeArea);
    }

    public void ApplyLayout(int width, int height, Rect area)
    {
        width=Mathf.Max(1,width); height=Mathf.Max(1,height);
        float bottom = touchEnabled && !GameFlow.IsMenu ? TouchJoystick.ReservedBottom(width,height,area) : 0;
        // URP renders overlay canvases after each base camera. A second clearing camera
        // makes the world overwrite the menu on mobile. Keep one full-screen camera;
        // an asymmetric projection reserves world space behind the opaque control dock.
        gameCamera.rect = new Rect(0,0,1,1);
        float aspect = (float)width / (height * (1-bottom));
        gameCamera.aspect = aspect;
        float size=gameCamera.orthographicSize;
        gameCamera.projectionMatrix=Matrix4x4.Ortho(-size*aspect,size*aspect,-size-2*size*bottom/(1-bottom),size,gameCamera.nearClipPlane,gameCamera.farClipPlane);
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
        touchControls.SetActive(touchEnabled);
        if (joystick) joystick.Apply();
        if (desktopHelp) desktopHelp.SetActive(!touchEnabled && !helpDismissed);
        if (desktopControlsText) desktopControlsText.SetActive(!touchEnabled && !helpDismissed);
        if (desktopHintText) desktopHintText.SetActive(!touchEnabled && !helpDismissed);
        safeArea.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
        safeArea.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        float referenceWidth = 1600f * Mathf.Sqrt(((float)width / 1600) / ((float)height / 900)) * area.width / width;
        if (titlePanel) titlePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(385, referenceWidth * .52f - 25));
        if (scorePanel) scorePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(260, referenceWidth * .35f));
    }
    void OnApplicationFocus(bool focused) { AudioListener.pause = !focused; if (!focused) SuspendGame(); }
    void OnApplicationPause(bool paused) { AudioListener.pause = paused; if (paused) SuspendGame(); }
    void SuspendGame()
    {
        GameInput.Clear();
        if (GameFlow.Instance) GameFlow.Instance.PauseForBackground();
    }
    void OnDisable() { GameInput.Clear(); }
}
