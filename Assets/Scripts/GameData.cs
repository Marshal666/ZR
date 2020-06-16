using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameData : MonoBehaviour {


    /// <summary>
    /// Podaci potrebni za rad igre (postavke, reference, "template" objekti, grupe objekata za razne svrhe...)
    /// </summary>


    [System.Serializable]   //dodaje mogučnost uređivanja klase u editoru
    public struct ObjNIcon  //klasa za konfiguriranje editor object item-a za level editor za unutarnje (#Interal) objekte
    {       
        public GameObject obj;  //referenca na objet item-a
        public Texture2D tex;   //slika objekta
        public string src;      //izvor objekta
        public string name;     //ime objekta
    }

    public static GameData main;        //za lakše dohvačanje nekih varijabli

    public Material DefaultMaterial;    //standardni materijal u igri

    public Shader defaultShader, transparentShader, renderTransparentShader; //koriušteni shader-i u igri

    public LayerMask noPlayer;  //layer mask bez player-a

    public GameObject[] particleEffects;        //grupa particle effect-a

    public Button[] mainButtons;                //tipke meni-a

    public GameObject levelsContainer;          //objekt koji sadrži tipke koje pokreću level-e

    public GameObject customLevelsContainer;    //objekt koji sadrži tipke koje pokreću custom level-e

    public GameObject[] optionsStuff;           //UI opcije igre

    public GameObject[] levelSelectionStuff;    //polje objekata koje sadrži UI elemente level selekcije

    public GameObject[] pauseStuff;             //polje koje sadrži UI elmente kada je igra pauzirana 

    public GameObject[] youDiedStuff;           //polje koje sadrži UI elemente kada igrač umre

    public GameObject[] youWonStuff;            //polje koje sadrži UI elemente kada igrač pobijedi

    public GameObject[] editorStuff;            //polje koje sadrži UI elemente level editor-a

    public GameObject[] inGameStuff;            //polje koje sadrži UI elemente kada igrač igra

    public GameObject helpUIStuff;            //sadrži pomoć (kontrole) igre

    public GameObject levelButtonConstructor;   //template tipka za učitavanje level-a

    public GameObject FPSText;                  //tekst FPS-a igre

    public Mesh[] Meshes;                       //default mesh-evi igre
    /// <summary>
    /// 0 - Cube
    /// 1 - Sphere
    /// 2 - Capsule
    /// 3 - Quad
    /// </summary>

    public Color editorDeselectedColor, editorSelectedColor, editorDisabledColor;   //boje UI elemenata u editoru

    public Material editorSelectedMaterial,         //materijal koji objet ima dok je selektiran u editoru
                    TriggerMaterial,                //materijal ta trigger objekte
                    InvincibleColliderMaterial,     //materijal za objekte u editoru koji su igraču nevidljivi prilikom igranja
                    WinTriggerMaterial;             //materijal koji označuje trigger-e u editoru koji donose pobijedu igraču

	public Button nextLevelButton;                  //referenca na tipku koja učitava level nakon trenutog (ako postoji)

    public Image hpBarImage;                        //hp bar igrača

    public Text hpText;                             //prikaz hp-a igrača u obliku teksta 

    public ObjNIcon[] interalObjects;               //objekti rađeni u unity editoru

	public List<string> unlockedLevels;             //lista path-ova do level-a koje je igrač otključao

    public Toggle fpsToggle;                        //checkbox za prikaz FPS-a

    public Slider musicVolumeSlider;                //Slider za jačinu zvuka

    public float playerMaxHp = 4;

    //statične referece za neke od gornjih varijabli za lakše dohvaćanje u kodu

	public static List<string> UnlockedLevels { get { return main.unlockedLevels; } set { main.unlockedLevels = value; } }

    public static Mesh[] GameMeshes { get { return main.Meshes; } }

    public static Material DefaultGameMaterial { get { return main.DefaultMaterial; } }

    public static Shader DefaultShader { get { return main.defaultShader; } }

    public static Shader TransparentShader { get { return main.transparentShader; } }

    public static Shader RenderTransparentShader { get { return main.renderTransparentShader; } }

    public static GameObject[] ParticleEffects { get { return main.particleEffects; } }

	public static Button NextLevelButton { get { return main.nextLevelButton; } set { main.nextLevelButton = value; } }

    public static ObjNIcon[] InteralObjects { get { return main.interalObjects; } }

    public static GameObject FPSTextR { get { return main.FPSText; } } 

    void Awake()    //inicijalizacija..
    {
        if(!main)
            main = this;
    }

}
