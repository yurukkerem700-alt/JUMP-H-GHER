using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Action { Left, Right, Jump }
    public Action action;
    readonly HashSet<int> held = new HashSet<int>();
    Image image;
    readonly Color normal = new Color(.15f, .085f, .065f, .88f);
    readonly Color pressed = new Color(.51f, .18f, .10f, .95f);

    void Awake() { image = GetComponent<Image>(); }
    public void OnPointerDown(PointerEventData data)
    {
        held.Add(data.pointerId);
        if (action == Action.Jump) GameInput.Jump();
        else GameInput.Hold(action == Action.Left, data.pointerId, true);
        if (image) image.color = pressed;
    }
    public void OnPointerUp(PointerEventData data)
    {
        held.Remove(data.pointerId);
        if (action != Action.Jump) GameInput.Hold(action == Action.Left, data.pointerId, false);
        if (image && held.Count == 0) image.color = normal;
    }
    void OnDisable()
    {
        foreach (int id in held) if (action != Action.Jump) GameInput.Hold(action == Action.Left, id, false);
        held.Clear();
        if (image) image.color = normal;
    }
}
