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
    Camera gameCamera, floorCamera;
    MovingPlatform2D[] platforms;
    Transform leftWall, rightWall;
    int lastWidth, lastHeight;
    Rect lastSafeArea;
    bool touchEnabled;
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
        if (!touchEnabled && Input.touchCount > 0) { touchEnabled=true; Apply(); }
        if (Input.GetKeyDown(KeyCode.T)) { touchEnabled = !touchEnabled; GameInput.Clear(); Apply(); }
        if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea) Apply();
    }

    public void SetTouchPreview(bool enabled) { touchEnabled = enabled; GameInput.Clear(); Apply(); }

    public void Apply()
    {
        lastWidth = Screen.width; lastHeight = Screen.height; lastSafeArea = Screen.safeArea;
        ApplyLayout(Screen.width, Screen.height, Screen.safeArea);
    }

    public void ApplyLayout(int width, int height, Rect area)
    {
        float bottom = touchEnabled ? TouchJoystick.ReservedBottom(width,height,area) : 0;
        gameCamera.rect = new Rect(0,bottom,1,1-bottom);
        // Oyun kamerası ekranın alt %16'sına çizmez; telefonda (iOS Safari) orası temizlenmediği için eski kare/menü
        // kalıntıları kalıyordu. Tüm ekranı her karede koyu renge boyayan boş bir kamera bunu önler.
        if (!floorCamera)
        {
            var go = new GameObject("Zemin Kamerası"); go.transform.SetParent(transform, false);
            floorCamera = go.AddComponent<Camera>();
            floorCamera.clearFlags = CameraClearFlags.SolidColor; floorCamera.backgroundColor = new Color(.035f, .025f, .045f);
            floorCamera.cullingMask = 0; floorCamera.orthographic = true; floorCamera.rect = new Rect(0, 0, 1, 1);
        }
        floorCamera.depth = gameCamera.depth - 10;
        floorCamera.enabled = gameCamera.rect.height < 1;
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
        touchControls.SetActive(touchEnabled);
        if (joystick) joystick.Apply();
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
    void OnApplicationFocus(bool focused) { if (!focused) SuspendGame(); }
    void OnApplicationPause(bool paused) { if (paused) SuspendGame(); }
    void SuspendGame()
    {
        GameInput.Clear();
        if (GameFlow.Instance) GameFlow.Instance.PauseForBackground();
    }
    void OnDisable() { GameInput.Clear(); }
}
