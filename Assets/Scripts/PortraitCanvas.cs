using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class PortraitCanvas : MonoBehaviour
{
    CanvasScaler scaler;
    int width, height;
    void Awake() { scaler=GetComponent<CanvasScaler>(); Apply(); }
    void Update() { if(width!=Screen.width || height!=Screen.height) Apply(); }
    void Apply()
    {
        width=Screen.width; height=Screen.height;
        scaler.referenceResolution = height>width ? new Vector2(720,1280) : new Vector2(1080,1080);
        scaler.matchWidthOrHeight = height>width ? 0 : .5f;
    }
}

public class SafeHudAnchor : MonoBehaviour
{
    RectTransform rect;
    Vector2 originalMin, originalMax, originalSize;
    void Start()
    {
        rect=(RectTransform)transform; originalMin=rect.anchorMin; originalMax=rect.anchorMax; originalSize=rect.sizeDelta;
    }
    void LateUpdate()
    {
        if(!rect || Screen.width<1 || Screen.height<1) return;
        var safe=Screen.safeArea;
        Vector2 min=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);
        Vector2 span=new Vector2(safe.width/Screen.width,safe.height/Screen.height);
        rect.anchorMin=min+Vector2.Scale(originalMin,span);
        rect.anchorMax=min+Vector2.Scale(originalMax,span);
        var parent=rect.parent as RectTransform;
        if(parent && originalMin.x==originalMax.x && originalSize.x>0)
            rect.sizeDelta=new Vector2(Mathf.Min(originalSize.x,Mathf.Max(1,parent.rect.width*span.x-32)),originalSize.y);
    }
}
