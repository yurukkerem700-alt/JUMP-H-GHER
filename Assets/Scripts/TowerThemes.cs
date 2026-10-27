using UnityEngine;

public class TowerThemes : MonoBehaviour
{
    public static TowerThemes Instance { get; private set; }
    public static readonly string[] Names={"Kül Mezarlığı","Yitik Bulutlar","Fırtına Geçidi","Kızıl Kökler","Yıldız Külleri"};
    static readonly Color[] Tints={new Color(1,.78f,.6f),new Color(.67f,.8f,1),new Color(.48f,.56f,.8f),new Color(1,.38f,.34f),new Color(.66f,.5f,1)};
    public int CurrentTheme { get; private set; }
    public int ThemeCycle { get; private set; }
    public string CurrentName => Names[CurrentTheme];
    InfiniteTower tower;
    // Five regions now fit inside the 100-floor campaign.
    public const int FloorsPerTheme=20;
    public static int ThemeAtFloor(int floor)=>Mathf.Max(0,floor)/FloorsPerTheme%Names.Length;
    public static Color TintAtFloor(int floor)=>Tints[ThemeAtFloor(floor)];
    void Awake(){Instance=this;}
    void Start(){tower=FindFirstObjectByType<InfiniteTower>();}
    void LateUpdate()
    {
        int floor=Mathf.Max(0,tower.HighestReachedFloor);
        CurrentTheme=ThemeAtFloor(floor);ThemeCycle=floor/FloorsPerTheme;
        foreach(var step in tower.coins.steps)
        {
            Color tint=TintAtFloor(step.floorIndex);
            foreach(var r in step.GetComponentsInChildren<SpriteRenderer>())
                if(r!=step.centerLight)r.color=tint;
        }
        var coin=tower.coins.coin.GetComponent<SpriteRenderer>();
        if(coin)coin.color=Color.Lerp(Color.white,TintAtFloor(tower.coins.SpawnFloor),.32f);
        if(tower.floorLabel)tower.floorLabel.text="KAT "+Mathf.Max(0,tower.HighestReachedFloor+1)+"  •  "+CurrentName;
    }
}
