using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    public float amplitude = 2.9f;
    public float frequency = .65f;
    public float phase;
    public float topOffset = .17f;
    public SpriteRenderer centerLight;
    public int floorIndex;
    private Rigidbody2D rb;
    private Vector2 origin;
    public Vector2 CurrentVelocity { get; private set; }
    public float Top => transform.position.y + topOffset;

    void Awake() { rb = GetComponent<Rigidbody2D>(); origin = rb.position; }

    public void Configure(int floor, float surfaceY)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        floorIndex = floor;
        origin = new Vector2(0, surfaceY - topOffset);

        phase = floor % 2 == 0 ? 0 : Mathf.PI;
        amplitude = Mathf.Clamp(GameDeviceLayout.HorizontalLimit - 1.3f, .35f, 2.9f);
        Vector2 initial = origin + Vector2.right * (amplitude * Mathf.Sin(Time.fixedTime * frequency + phase));
        rb.position = initial; transform.position = initial;
        CurrentVelocity = Vector2.right * (amplitude * frequency * Mathf.Cos(Time.fixedTime * frequency + phase));
    }

    void FixedUpdate()
    {
        Vector2 next = origin + Vector2.right * (amplitude * Mathf.Sin(Time.fixedTime * frequency + phase));
        CurrentVelocity = Vector2.right * (amplitude * frequency * Mathf.Cos(Time.fixedTime * frequency + phase));
        rb.MovePosition(next);
    }

    void Update()
    {
        if (centerLight) centerLight.color = new Color(.85f, .13f, .06f, .45f + Mathf.Sin(Time.time * 2) * .12f);
    }
}
