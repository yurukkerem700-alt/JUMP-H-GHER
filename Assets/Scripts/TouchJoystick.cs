using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// All mobile inputs share a dock whose space is reserved by the camera.
// Kontroller "field" içinde durur; oyuncu bunları sürükleyip güvenli alanın herhangi bir yerine koyabilir.
public class TouchJoystick : MonoBehaviour
{
    public const float DockHeight=144;
    public static bool Editing { get; private set; }
    Canvas canvas; CanvasGroup group;
    RectTransform dock,field,stick,knob,left,right,jump,kick;
    bool editing; System.Action onEditEnd; Vector2 dragOffset;
    RectTransform editUi; Image dim; TextMeshProUGUI joyToggleText;
    RectTransform[] Controls => new[]{stick,left,right,jump,kick};
    GameDeviceLayout layout;
    bool preview,active; int pointer,lastWidth,lastHeight;
    Vector2 origin; float radius; Rect lastSafe;
    public static float ScaleFor(int width,int height) => Mathf.Max(.75f,Mathf.Min(width/400f,height/700f));
    public static float ReservedBottom(int width,int height,Rect safe)
        => Mathf.Clamp01((safe.yMin+DockHeight*ScaleFor(width,height))/Mathf.Max(1,height));
    public static Rect[] Targets(float availableWidth,float joySize,float buttonSize)
    {
        float w=availableWidth, stickX=Mathf.Min(w*.22f,100), jumpX=w-Mathf.Min(w*.17f,68);
        float actionScale=Mathf.Clamp(buttonSize,.95f,1.1f);
        var result=new[] {
            Target(stickX,76,Mathf.Clamp(100*joySize,88,108)),
            Target(stickX-36,76,60), Target(stickX+36,76,60),
            Target(jumpX,80,76*actionScale), Target(jumpX-96,76,64*actionScale)
        };
        if(w<340)
        {
            result[1]=Target(w*.13f,76,60); result[2]=Target(w*.32f,76,60);
            result[3]=Target(w*.8f,48,64); result[4]=Target(w*.8f,112,48);
        }
        return result;
    }
    static Rect Target(float x,float y,float size) => new Rect(x-size*.5f,y-size*.5f,size,size);
    public static TouchJoystick Create(GameDeviceLayout owner)
    {
        var go=new GameObject("Mobil Kontrol Paneli",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(TouchJoystick));
        var joy=go.GetComponent<TouchJoystick>(); joy.layout=owner;
        joy.canvas=go.GetComponent<Canvas>(); joy.canvas.renderMode=RenderMode.ScreenSpaceOverlay; joy.canvas.sortingOrder=850;
        go.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
        joy.Build(); GameSettings.Changed+=joy.Apply; GameInput.Cleared+=joy.ResetState; joy.Apply(); return joy;
    }
    void Build()
    {
        var root=new GameObject("Kontrol Zemini",typeof(Image),typeof(CanvasGroup)); root.transform.SetParent(canvas.transform,false);
        dock=root.GetComponent<RectTransform>(); group=root.GetComponent<CanvasGroup>();
        root.GetComponent<Image>().color=new Color(.045f,.035f,.065f,.98f); root.GetComponent<Image>().raycastTarget=false;
        var line=UiArt.Pic(dock,null,new Color(.85f,.6f,.35f,.4f),.5f,1,0,1).rectTransform;
        line.anchorMin=new Vector2(0,1); line.anchorMax=Vector2.one; line.sizeDelta=new Vector2(0,1);
        field=new GameObject("Kontrol Alanı",typeof(RectTransform)).GetComponent<RectTransform>(); field.SetParent(dock,false);
        field.anchorMin=field.anchorMax=Vector2.zero; field.pivot=Vector2.zero; field.anchoredPosition=Vector2.zero;
        stick=new GameObject("Hareket",typeof(Image),typeof(CanvasGroup),typeof(JoystickPad)).GetComponent<RectTransform>();
        stick.SetParent(field,false); stick.GetComponent<Image>().sprite=UiArt.Disc;
        stick.GetComponent<Image>().color=new Color(.16f,.14f,.22f,1); stick.GetComponent<JoystickPad>().owner=this;
        UiArt.Pic(stick,UiArt.Disc,new Color(.6f,.49f,.38f,1),.5f,.5f,92,92);
        UiArt.Pic(stick,UiArt.Disc,new Color(.085f,.075f,.13f,1),.5f,.5f,88,88);
        knob=UiArt.Pic(stick,UiArt.Disc,new Color(.95f,.78f,.53f,1),.5f,.5f,40,40).rectTransform;
        Label(stick,"SOL / SAĞ",new Vector2(0,-60),11);
        left=Action("SOL",MobileDockAction.Kind.Left); right=Action("SAĞ",MobileDockAction.Kind.Right);
        jump=Action("ZIPLA",MobileDockAction.Kind.Jump); kick=Action("TEKME",MobileDockAction.Kind.Kick);
        var all=Controls;
        for(int i=0;i<all.Length;i++){var h=all[i].gameObject.AddComponent<ControlDragHandle>(); h.owner=this; h.index=i;}
    }
    void Label(Transform parent,string value,Vector2 at,float size)
    {
        var tower=FindFirstObjectByType<InfiniteTower>();
        var text=UiArt.Label(parent,tower?tower.floorLabel.font:null,value,.5f,.5f,100,26,size,new Color(.95f,.85f,.7f));
        text.rectTransform.anchoredPosition=at;
    }
    RectTransform Action(string label,MobileDockAction.Kind kind)
    {
        var go=new GameObject(label,typeof(Image),typeof(CanvasGroup),typeof(MobileDockAction)); go.transform.SetParent(field,false);
        var image=go.GetComponent<Image>(); image.sprite=UiArt.Disc;
        image.color=kind==MobileDockAction.Kind.Jump?new Color(.48f,.22f,.12f,1):new Color(.2f,.17f,.25f,1);
        var action=go.GetComponent<MobileDockAction>(); action.kind=kind; action.normal=image.color;
        Label(go.transform,label,Vector2.zero,kind==MobileDockAction.Kind.Jump?15:12);
        return go.GetComponent<RectTransform>();
    }
    static void Place(RectTransform rect,float x,float y,float size)
    {
        rect.anchorMin=rect.anchorMax=Vector2.zero; rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=Vector2.one*size;
    }
    public void SetLegacyButtons(GameObject[] buttons) { foreach(var b in buttons) if(b) b.SetActive(false); }
    public void SetPreview(bool on) { preview=on; ResetState(); Apply(); }
    public void Apply()
    {
        if(!canvas) return;
        GameInput.Clear();
        float scale=ScaleFor(Screen.width,Screen.height); canvas.scaleFactor=scale; canvas.GetComponent<CanvasScaler>().scaleFactor=scale;
        var safe=Screen.safeArea;
        dock.anchorMin=dock.anchorMax=Vector2.zero; dock.pivot=Vector2.zero;
        dock.anchoredPosition=new Vector2(safe.xMin/scale,safe.yMin/scale);
        dock.sizeDelta=new Vector2(safe.width/scale,DockHeight);
        float fieldW=safe.width/scale, fieldH=safe.height/scale; field.sizeDelta=new Vector2(fieldW,fieldH);
        var targets=Targets(dock.sizeDelta.x,GameSettings.JoySize,GameSettings.BtnSize);
        var controls=Controls;
        for(int i=0;i<controls.Length;i++)
        {
            Vector2 at=targets[i].center;
            if(GameSettings.HasLayout){var saved=GameSettings.Control(i); at=new Vector2(saved.x*fieldW,saved.y*fieldH);}
            at=ClampToField(at,targets[i].width);
            Place(controls[i],at.x,at.y,targets[i].width);
        }
        radius=targets[0].width*.5f*scale;
        stick.gameObject.SetActive(GameSettings.JoyEnabled);
        left.gameObject.SetActive(!GameSettings.JoyEnabled); right.gameObject.SetActive(!GameSettings.JoyEnabled);
        group.alpha=1;
        stick.GetComponent<CanvasGroup>().alpha=Mathf.Clamp(GameSettings.JoyOpacity,.65f,1);
        foreach(var target in new[]{left,right,jump,kick}) target.GetComponent<CanvasGroup>().alpha=Mathf.Clamp(GameSettings.BtnOpacity,.65f,1); group.blocksRaycasts=!preview||editing; group.interactable=!preview||editing;
        canvas.sortingOrder=preview||editing?1001:850;
        if(editUi) editUi.anchoredPosition=new Vector2(0,-((Screen.height-safe.yMax)/scale+12));
        UpdateVisibility(); lastWidth=Screen.width; lastHeight=Screen.height; lastSafe=safe;
    }
    void UpdateVisibility()
    {
        bool show=layout && (layout.TouchEnabled || preview || editing) && (preview || editing || !GameFlow.IsMenu);
        if(dock.gameObject.activeSelf!=show) { ResetState(); dock.gameObject.SetActive(show); }
    }
    void LateUpdate()
    {
        if(Screen.width!=lastWidth || Screen.height!=lastHeight || Screen.safeArea!=lastSafe) Apply();
        UpdateVisibility();
    }
    Vector2 ClampToField(Vector2 at,float size)
    {
        float h=size*.5f;
        return new Vector2(Mathf.Clamp(at.x,h,Mathf.Max(h,field.sizeDelta.x-h)),Mathf.Clamp(at.y,h,Mathf.Max(h,field.sizeDelta.y-h)));
    }
    // Yerleşim düzenleme: kontroller parmakla sürüklenir, TAMAM ile kaydedilir. dimAlpha: arkadaki ekranı karartma miktarı.
    public void BeginEdit(float dimAlpha,System.Action onEnd=null)
    {
        if(editing || !canvas) return;
        EnsureEditUi(); dim.color=new Color(.02f,.01f,.03f,dimAlpha); dim.gameObject.SetActive(true); editUi.gameObject.SetActive(true);
        onEditEnd=onEnd; editing=true; Editing=true; ResetState(); Apply(); RefreshEditUi();
    }
    void EndEdit()
    {
        if(!editing) return;
        SaveLayout(); GameSettings.LayoutDone=true;
        editing=false; Editing=false; dim.gameObject.SetActive(false); editUi.gameObject.SetActive(false);
        var done=onEditEnd; onEditEnd=null; Apply(); if(done!=null) done();
    }
    void SaveLayout()
    {
        var all=Controls; var saved=new Vector2[all.Length];
        for(int i=0;i<all.Length;i++) saved[i]=new Vector2(all[i].anchoredPosition.x/Mathf.Max(1,field.sizeDelta.x),all[i].anchoredPosition.y/Mathf.Max(1,field.sizeDelta.y));
        GameSettings.SaveLayout(saved);
    }
    public void BeginDragControl(int i,PointerEventData e)
    {
        if(!editing) return;
        Vector2 p; RectTransformUtility.ScreenPointToLocalPointInRectangle(field,e.position,null,out p);
        dragOffset=Controls[i].anchoredPosition-p;
    }
    public void DragControl(int i,PointerEventData e)
    {
        if(!editing) return;
        Vector2 p; RectTransformUtility.ScreenPointToLocalPointInRectangle(field,e.position,null,out p);
        var rect=Controls[i]; rect.anchoredPosition=ClampToField(p+dragOffset,rect.sizeDelta.x);
    }
    public void EndDragControl() { if(editing) SaveLayout(); }
    void EnsureEditUi()
    {
        if(editUi) return;
        var tower=FindFirstObjectByType<InfiniteTower>(); var font=tower?tower.floorLabel.font:null;
        var dimGo=new GameObject("Düzenleme Karartma",typeof(Image)); dimGo.transform.SetParent(canvas.transform,false);
        dim=dimGo.GetComponent<Image>(); var dr=dim.rectTransform; dr.anchorMin=Vector2.zero; dr.anchorMax=Vector2.one; dr.offsetMin=dr.offsetMax=Vector2.zero;
        dimGo.transform.SetAsFirstSibling();
        var go=new GameObject("Düzenleme Paneli",typeof(Image)); go.transform.SetParent(canvas.transform,false);
        editUi=go.GetComponent<RectTransform>(); editUi.anchorMin=editUi.anchorMax=new Vector2(.5f,1); editUi.pivot=new Vector2(.5f,1);
        editUi.sizeDelta=new Vector2(360,190); go.GetComponent<Image>().color=new Color(.05f,.03f,.08f,.94f);
        var hint=UiArt.Label(editUi,font,"Düğmeleri parmağınla sürükle,\nistediğin yere koy",.5f,.82f,340,70,20,new Color(1,.86f,.66f));
        hint.enableAutoSizing=true; hint.fontSizeMin=13; hint.fontSizeMax=20;
        var toggle=UiArt.Btn(editUi,font,"",.5f,.5f,320,52,()=>{GameSettings.JoyEnabled=!GameSettings.JoyEnabled; RefreshEditUi();});
        joyToggleText=toggle.GetComponentInChildren<TextMeshProUGUI>();
        UiArt.Btn(editUi,font,"SIFIRLA",.26f,.16f,160,52,()=>{GameSettings.ResetLayout(); RefreshEditUi();});
        UiArt.Btn(editUi,font,"TAMAM",.74f,.16f,160,52,EndEdit);
        go.SetActive(false); dimGo.SetActive(false);
    }
    void RefreshEditUi() { if(joyToggleText) joyToggleText.text=GameSettings.JoyEnabled?"JOYSTICK: AÇIK":"JOYSTICK KAPALI (OK düğmeleri)"; }
    public void Press(PointerEventData e)
    {
        if(active || preview || editing || GameFlow.IsMenu) return;
        active=true; pointer=e.pointerId; origin=RectTransformUtility.WorldToScreenPoint(null,stick.position); Drag(e);
    }
    public void Drag(PointerEventData e)
    {
        if(!active || pointer!=e.pointerId) return;
        var delta=Vector2.ClampMagnitude(e.position-origin,radius);
        float dead=radius*.18f/Mathf.Clamp(GameSettings.JoySensitivity,.5f,1.5f);
        GameInput.SetJoystick(Mathf.Abs(delta.x)>dead?Mathf.Sign(delta.x):0,false,false);
        knob.anchoredPosition=Vector2.ClampMagnitude(delta/canvas.scaleFactor,32);
    }
    public void Release(PointerEventData e) { if(active && pointer==e.pointerId) { ResetState(); GameInput.SetJoystick(0,false,false); } }
    void ResetState() { active=false; if(knob) knob.anchoredPosition=Vector2.zero; }
    void OnDisable() { ResetState(); GameInput.SetJoystick(0,false,false); }
    void OnDestroy() { GameSettings.Changed-=Apply; GameInput.Cleared-=ResetState; }
}

public class JoystickPad : MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
{
    public TouchJoystick owner;
    public void OnPointerDown(PointerEventData e) { owner.Press(e); }
    public void OnDrag(PointerEventData e) { owner.Drag(e); }
    public void OnPointerUp(PointerEventData e) { owner.Release(e); }
}

// Düzenleme modunda kontrolü parmakla taşır.
public class ControlDragHandle : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    public TouchJoystick owner; public int index;
    public void OnBeginDrag(PointerEventData e) { owner.BeginDragControl(index,e); }
    public void OnDrag(PointerEventData e) { owner.DragControl(index,e); }
    public void OnEndDrag(PointerEventData e) { owner.EndDragControl(); }
}

public class MobileDockAction : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
{
    public enum Kind { Left,Right,Jump,Kick }
    public Kind kind; public Color normal;
    bool held; int pointer;
    void OnEnable() { GameInput.Cleared+=ResetState; }
    public void OnPointerDown(PointerEventData e)
    {
        if(held || TouchJoystick.Editing || GameFlow.IsMenu) return;
        held=true; pointer=e.pointerId;
        if(kind==Kind.Jump) GameInput.Jump(); else if(kind==Kind.Kick) GameInput.Kick(); else GameInput.Hold(kind==Kind.Left,pointer,true);
        GetComponent<Image>().color=new Color(.55f,.22f,.08f,1);
    }
    public void OnPointerUp(PointerEventData e) { if(held && pointer==e.pointerId) Release(); }
    public void OnPointerExit(PointerEventData e) { if(held && pointer==e.pointerId) Release(); }
    void Release() { if(held && (kind==Kind.Left || kind==Kind.Right)) GameInput.Hold(kind==Kind.Left,pointer,false); ResetState(); }
    void ResetState() { held=false; GetComponent<Image>().color=normal; }
    void OnDisable() { Release(); GameInput.Cleared-=ResetState; }
}
