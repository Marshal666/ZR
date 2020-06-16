using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Xml;

/// <summary>
/// komponenta koja osnovi kontrolira igru, 
/// sadrži metode koje se pozivaju kada korisnik klikne na gumbe,
/// inicijalizira igru, prati njeno stanje, genrira i podešava UI, čisti scenu, 
/// postavlja pauzu
/// </summary>

public class Scene : MonoBehaviour {

    public enum GameState   //state-ovi igre
    {
        menu,
        playing,
        editing,
        paused,
        won
    }

    static GameState gs = 0;
    public static GameState currentGameState    //svojstvo koje prilikom promjene stanja igre mijenja glazbu u igri
    {
        get { return gs; }
        set
        {
            gs = value;
            switch (gs)
            {
                case GameState.menu:
                    if (PlayerCamera.main.a7.clip != PlayerCamera.main.menuMusic)
                    {
                        PlayerCamera.main.a7.clip = PlayerCamera.main.menuMusic;
                        PlayerCamera.main.a7.Play();
                    }
                    break;
                case GameState.playing:
                    if (PlayerCamera.main.a7.clip != PlayerCamera.main.gameMusic)
                    {
                        PlayerCamera.main.a7.clip = PlayerCamera.main.gameMusic;
                        PlayerCamera.main.a7.Play();
                    }
                    break;
                case GameState.editing:
                    PlayerCamera.main.a7.Stop();
                    PlayerCamera.main.a7.clip = null;
                    break;
                case GameState.paused:
                    break;
                case GameState.won:
                    break;
                default:
                    break;
            }
        } }   //trenutni game state je meni (u početku)

    public static Scene main;   //za lakše baratanje

    public static GameObject rootObject;    //root objekt koji sadrži sve objekte level-a u sceni

    public static Light mainLight;          //glavna svjetlost u sceni

    public static Camera mainCamera;        //glavna kamera u sceni

    public static Player player;            //referenca na igrača

    public static float minHeight = -10f;   //min dozvoljena visina igrača

	public static string nextLevel = "";    //path do datoteke sljedećeg level-a

    //public float playerMaxHp = 4;

    void Awake()    //inicijalizacija
    {
        if (main == null)
            main = this;
        if (main != this)
            main = this;
        if (!mainCamera)
            mainCamera = Camera.main;
        if(!rootObject)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Root");
            if (obj)
                rootObject = obj;
            else
            {
                rootObject = new GameObject("Root");
                rootObject.tag = "Root";
            }
        }
        if (!mainLight)
            mainLight = GameObject.FindObjectOfType<Light>();
        player = GameObject.FindObjectOfType<Player>();
        XmlDocument settings = new XmlDocument();
        settings.Load("GameSettings.xml");
        XmlNode options = settings.GetElementsByTagName("Options")[0];
        PlayerCamera.main.a7.volume = float.Parse(options.ChildNodes[0].InnerText);
        GameData.main.musicVolumeSlider.value = PlayerCamera.main.a7.volume;
        if (bool.Parse(options.ChildNodes[1].InnerText))
        {
            //ToggleFPS();
            GameData.main.fpsToggle.isOn = true;
        }
        LoadMenu(); //učitaj meni..
    }

    public void SaveOptions()   //spremanje promjenjenih opcija igre
    {
        XmlDocument settings = new XmlDocument();
        settings.Load("GameSettings.xml");
        XmlNode options = settings.GetElementsByTagName("Options")[0];
        options.ChildNodes[0].InnerText = PlayerCamera.main.a7.volume.ToString();
        options.ChildNodes[1].InnerText = GameData.main.fpsToggle.isOn.ToString();
        settings.Save("GameSettings.xml");
    }

	public void InitUnlockedLevels() {                  //update-aj otključane level-e sa GameSettings.xml datoteke
		XmlDocument doc = new XmlDocument ();
		doc.Load (System.IO.Directory.GetCurrentDirectory () + "/GameSettings.xml");
		XmlNodeList l = doc.GetElementsByTagName ("LevelPath");
		for (int i = 0; i < l.Count; i++) {
			if (!GameData.UnlockedLevels.Contains (l [i].InnerText))
				GameData.UnlockedLevels.Add (l [i].InnerText);
		}
	}

    public void ToggleFPS ()    //za uključivanje prikaza FPS-a
    {
        GameData.FPSTextR.SetActive(!GameData.FPSTextR.activeSelf);
    }

    public void LoadMenu()  //učitavanje meni-ja
    {
        Cursor.visible = true;
        if (currentGameState == GameState.editing)  //ako je igrač prije bio u level editor-u, počisti sve što je od toga ostalo
            GameEditor.Clear();
        if (LevelLoader.currentLevel != "Menu.xml") //učitaj Level menija (ako već nije učitan)
            LevelLoader.Load("Menu.xml");
        //iskluči sve ostale UI grupe objekata
        for (int i = 0; i < GameData.main.youDiedStuff.Length; i++)
            GameData.main.youDiedStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.youWonStuff.Length; i++)
            GameData.main.youWonStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.inGameStuff.Length; i++)
            GameData.main.inGameStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.pauseStuff.Length; i++)
            GameData.main.pauseStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.editorStuff.Length; i++)
            GameData.main.editorStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.levelSelectionStuff.Length; i++)
            GameData.main.levelSelectionStuff[i].gameObject.SetActive(false);
        for (int i = 0; i < GameData.main.optionsStuff.Length; i++)
            GameData.main.optionsStuff[i].SetActive(false);
        GameData.main.helpUIStuff.SetActive(false);
        Time.timeScale = 1f;    //za uklanjanje pauze (ako je bila)
        //općenite postavke scene i igre...
        PlayerCamera.main.mode = PlayerCamera.PlayerCameraMode.Stand;
        Player.playing = false;
        currentGameState = GameState.menu;
        //uključi UI menija 
        for(int i = 0; i < GameData.main.mainButtons.Length; i++)
            GameData.main.mainButtons[i].gameObject.SetActive(true);
        if (Player.main)
            Player.main.anim.Play("Idles", 0); //dino treba biti u idle animaciji
    }

    public void clearUIAndStartLevel()  //prije početka levela
    {
        currentGameState = GameState.playing;   //postavljanje ispravong game state-a
        Player.playing = true;
        //isključi sve nepotrebne UI objekte
        for (int i = 0; i < GameData.main.mainButtons.Length; i++)
            GameData.main.mainButtons[i].gameObject.SetActive(false);
        for (int i = 0; i < GameData.main.levelSelectionStuff.Length; i++)
            GameData.main.levelSelectionStuff[i].gameObject.SetActive(false);
        for (int i = 0; i < GameData.main.youWonStuff.Length; i++)
            GameData.main.youWonStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.optionsStuff.Length; i++)
            GameData.main.optionsStuff[i].SetActive(false);
        //uključi "in game" UI objekte
        for (int i = 0; i < GameData.main.inGameStuff.Length; i++)
            GameData.main.inGameStuff[i].SetActive(true);
    }

    public void OpenOptions()   //Otvaranje opcija igre
    {
        for (int i = 0; i < GameData.main.optionsStuff.Length; i++) //uključi UI objekte opcija
            GameData.main.optionsStuff[i].SetActive(true);
        //isključi sve nepotrebne UI objekte
        for (int i = 0; i < GameData.main.mainButtons.Length; i++)
            GameData.main.mainButtons[i].gameObject.SetActive(false);
        for (int i = 0; i < GameData.main.levelSelectionStuff.Length; i++)
            GameData.main.levelSelectionStuff[i].gameObject.SetActive(false);

    }
    
    public void LoadLevelSelection()    //učitavanje level selekcije
    {
		InitUnlockedLevels ();          //update-aj otklučane level-e
        if (LevelLoader.currentLevel != "Menu.xml")
            LevelLoader.Load("Menu.xml");
        //update-aj scenu (ako već nije..)
        PlayerCamera.main.mode = PlayerCamera.PlayerCameraMode.Stand;
        currentGameState = GameState.menu;
        //isključi nepotrebne UI objekte
        for (int i = 0; i < GameData.main.mainButtons.Length; i++)
            GameData.main.mainButtons[i].gameObject.SetActive(false);
        for (int i = 0; i < GameData.main.inGameStuff.Length; i++)
            GameData.main.inGameStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.optionsStuff.Length; i++)
            GameData.main.optionsStuff[i].SetActive(false);
        //uključu UI objekte level selekcije
        for (int i = 0; i < GameData.main.levelSelectionStuff.Length; i++)
            GameData.main.levelSelectionStuff[i].gameObject.SetActive(true);
        //počisti već učitane tipke za level-e (ako postoje)
        for (int i = 0; i < GameData.main.levelsContainer.transform.childCount; i++)
        {
            Destroy(GameData.main.levelsContainer.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < GameData.main.customLevelsContainer.transform.childCount; i++)
        {
            Destroy(GameData.main.customLevelsContainer.transform.GetChild(i).gameObject); 
        }
        List<string> levels = new List<string>(System.IO.Directory.GetDirectories("Levels"));   //pretpostavka je da svaka mapa u direktoriju Levels sadrži Level.xml datoteku koja sadrži info o levelu
        for (int i = 0; i < levels.Count; i++)
        {
            levels[i] = levels[i].Replace('\\', '/');   //za podršku na Mac OS X-u
			string arg = System.IO.Directory.GetCurrentDirectory() + "/" + levels[i] + "/Level.xml";    //argument za delegaciju
			if (System.IO.File.Exists (arg)) {                                                          //ako datoteka postoji, kreiraj gumb za učitavanje level-a po template-u za to
				GameObject obj = Instantiate (GameData.main.levelButtonConstructor);
				obj.name = levels [i].Remove (0, 7);
				obj.transform.SetParent (GameData.main.levelsContainer.transform);
				obj.transform.GetChild (0).GetComponent<Text> ().text = obj.name;
				XmlDocument doc = new XmlDocument ();
				doc.Load (arg);
				XmlNodeList l = doc.GetElementsByTagName ("Locked");    //za provjeru da li je level zaključan
				bool locked = false;
				if (l.Count > 0)
					locked = bool.Parse (l [0].InnerText);
				if (!locked || GameData.UnlockedLevels.Contains("/"+levels[i]+"/Level.xml"))    //ako nije zaklučan, igrač može igrati level
					obj.GetComponent<Button> ().onClick.AddListener (delegate {
						LevelLoader.Load (arg);
					}); //dok korisnik stisne gumb, level se počinje učitavati
				else {
					obj.transform.GetChild (1).gameObject.SetActive (true);                     //u suprotnom, zaključaj level (uključuje lokot)
				}
			}
        }
        levels.Clear(); //stvaranje cutom levela po istom principu kao i za obične Level-e
        levels = new List<string>(System.IO.Directory.GetDirectories("CustomLevels"));
        for (int i = 0; i < levels.Count; i++)
        {
            levels[i] = levels[i].Replace('\\', '/');
			string arg = System.IO.Directory.GetCurrentDirectory() + "/" + levels[i] + "/Level.xml";
			if (System.IO.File.Exists (arg)) {
				GameObject obj = Instantiate (GameData.main.levelButtonConstructor);
				obj.name = levels [i].Remove (0, 13);
				obj.transform.SetParent (GameData.main.customLevelsContainer.transform);
				obj.transform.GetChild (0).GetComponent<Text> ().text = obj.name;
				XmlDocument doc = new XmlDocument ();
				doc.Load (arg);
				XmlNodeList l = doc.GetElementsByTagName ("Locked");
				bool locked = false;
				if (l.Count > 0)
					locked = bool.Parse (l [0].InnerText);
				if (!locked || GameData.UnlockedLevels.Contains("/"+levels[i]+"/Level.xml"))
					obj.GetComponent<Button> ().onClick.AddListener (delegate {
						LevelLoader.Load (arg);
					});
				else {
					obj.transform.GetChild (1).gameObject.SetActive (true);
				}
			}
        }
        Player.main.anim.Play("Idles", 0);  //dino je u početku u idle animaciji
    }

    public void LoadEditor()    //djelomična inicijializacija game editora
    {
        LevelLoader.Load("Empty.xml");  //učitaj prazni level
        //postavke scene i igre...
        PlayerCamera.main.mode = PlayerCamera.PlayerCameraMode.Editor;
        Player.playing = false;
        //isključi nepotrebne UI objekte...
        for (int i = 0; i < GameData.main.mainButtons.Length; i++)
            GameData.main.mainButtons[i].gameObject.SetActive(false);
        for (int i = 0; i < GameData.main.inGameStuff.Length; i++)
            GameData.main.inGameStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.optionsStuff.Length; i++)
            GameData.main.optionsStuff[i].SetActive(false);
        //uključi UI editora
        for (int i = 0; i < GameData.main.editorStuff.Length; i++)
            GameData.main.editorStuff[i].SetActive(true);
        currentGameState = GameState.editing;
        GameEditor.Setup();     //editor se treba inicijalizirati
        Player.main.anim.SetBool("Idle", false);    //dino ne smije biti u ikakvoj animaciji
        Player.main.anim.Play("Nothing", 0);
    }

    public void LoadHelp()
    {
        //isključi nepotrebne UI objekte...
        for (int i = 0; i < GameData.main.mainButtons.Length; i++)
            GameData.main.mainButtons[i].gameObject.SetActive(false);
        //uključi help UI
        GameData.main.helpUIStuff.SetActive(true);
    }

    public void Quit()  //izlaz iz igre...
    {
        Application.Quit();
    }

    public void restartLevel()  //ponovno pokretanje level-a
    {
        currentGameState = GameState.playing;
        //iskluči nepotreban UI
        for (int i = 0; i < GameData.main.youDiedStuff.Length; i++)
            GameData.main.youDiedStuff[i].SetActive(false);
        for (int i = 0; i < GameData.main.youWonStuff.Length; i++)
            GameData.main.youWonStuff[i].SetActive(false);
        LevelLoader.Load(LevelLoader.currentLevel); //reload-aj level
        player.motion = Vector3.zero;   //resertiraj player-ove postavke
        player.currentJump = 0;
		Player.playing = false;
        Player.main.jump = false;
        Player.main.hp_max = GameData.main.playerMaxHp;
        Player.main.anim.SetBool("Dead", false);
        Player.main.anim.SetBool("Idle", true);
        Player.main.anim.Play("Idles", 0);
        unPauseGame();                  //ukloni pauzu
    }

    public void PauseGame()         //pauziranje igre
    {
        if (Player.playing)
        {
            currentGameState = GameState.paused;
            Time.timeScale = 0f;                    //zamrzni vrijeme
            for (int i = 0; i < GameData.main.pauseStuff.Length; i++)
                GameData.main.pauseStuff[i].SetActive(true);            //ukluči UI od pauze
            Player.playing = false;
        }
    }

    public void unPauseGame()   //uklanjanje pauze
    {
        if (!Player.playing && currentGameState != GameState.won)
        {
            currentGameState = GameState.playing;
            Time.timeScale = 1f;
            for (int i = 0; i < GameData.main.pauseStuff.Length; i++)   //isključi UI pauze
                GameData.main.pauseStuff[i].SetActive(false);
            Player.playing = true;
        }
    }

    public void playerDied()    //kada igrač umre
    {
        PlayerCamera.main.mode = PlayerCamera.PlayerCameraMode.Stand;   //resertiraj postavke igrača
        Player.main.hp_max = GameData.main.playerMaxHp;
        for (int i = 0; i < GameData.main.youDiedStuff.Length; i++)
            GameData.main.youDiedStuff[i].SetActive(true);              //uključi potreban UI
        Cursor.visible = true;
        Player.playing = false;
        Player.main.outMotion = Vector3.zero;
        Player.main.motion = Vector3.zero;
    }

    public void playerWon()     //kada igrač pobijedi
    {
        currentGameState = GameState.won;   //postavi postavke igre...
        Cursor.visible = true;
        Player.main.hp_max = GameData.main.playerMaxHp;
        PlayerCamera.main.mode = PlayerCamera.PlayerCameraMode.Stand;
        for (int i = 0; i < GameData.main.youWonStuff.Length; i++)  //uključi potrebne uI objekte
            GameData.main.youWonStuff[i].SetActive(true);
        Player.playing = false;
		if (!string.IsNullOrEmpty (nextLevel) && File.Exists(nextLevel)) {      //ako postoji sljedeći level nakon ovoga i zaključan je, otključaj ga, također uključi next level gumb
			XmlDocument doc = new XmlDocument ();
			doc.Load (nextLevel);
			XmlNode locked = doc.ChildNodes [1].ChildNodes [5];
			if (bool.Parse (locked.InnerText)) {
				string l = nextLevel.Replace (Directory.GetCurrentDirectory (), "");
				if (!GameData.UnlockedLevels.Contains (l)) {
					XmlDocument gs = new XmlDocument ();
					gs.Load (Directory.GetCurrentDirectory () + "/GameSettings.xml");
					XmlNode lp = gs.CreateElement (string.Empty, "LevelPath", string.Empty);
					lp.AppendChild (gs.CreateTextNode (l));
					gs.ChildNodes [1].ChildNodes [0].AppendChild (lp);
					gs.Save (Directory.GetCurrentDirectory () + "/GameSettings.xml");
					GameData.UnlockedLevels.Add (l);
				}
			}
		}
    }

    static void clearChildren(GameObject o) //uklanjanje child objekata..
    {
        for (int i = 0; i < o.transform.childCount; i++)
        {
            if (o.transform.GetChild(i).childCount > 0)
                clearChildren(o.transform.GetChild(i).gameObject);
            o.transform.GetChild(i).name = "deleted";
            o.transform.GetChild(i).gameObject.SetActive(false);
            Destroy(o.transform.GetChild(i).gameObject);
        }
    }

    public static void ClearScene() //čiščenje scene
    {
        if (Player.main)
        {
            Player.main.hp_max = GameData.main.playerMaxHp;                  //resertiraj postavke igrača
            Player.main.outMotion = Vector3.zero;
            Player.main.motion = Vector3.zero;
            Player.main.anim.SetBool("Dead", false);
            Player.main.anim.SetBool("Walking", false);
            Player.main.anim.SetBool("Idle", true);
            if (currentGameState == GameState.playing)
                Player.main.anim.Play("Idles");
            else
                Player.main.anim.Play("Nothing");
        }
        clearChildren(rootObject);                      //počisti sve objekte level-a
    }
        
    void Update()   //svakog frame-a u igri
    {
		//print (Player.playing+" "+currentGameState);
        if (Input.GetKeyDown(KeyCode.Escape))                   //uključuj/isključuj pauzu ako je pritisnut space
            if (currentGameState == GameState.playing)
                PauseGame();
            else if (currentGameState == GameState.paused)
                unPauseGame();
        if(currentGameState == GameState.playing)               //update-aj hp bar i text hp-a igrača
        {
            if (Player.main.isAlive)
                Cursor.visible = false;
            GameData.main.hpBarImage.fillAmount = Player.main.hp / Player.main.hp_max;
            GameData.main.hpText.text = Player.main.hp.ToString("0");
        }
        if (currentGameState == GameState.paused)
            Cursor.visible = true;
    }
	
}
