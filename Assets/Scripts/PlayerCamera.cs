
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Komponenta za kameru igre

public class PlayerCamera : MonoBehaviour {

    //U kojem načinu kamera radi
    public enum PlayerCameraMode
    {
        ThirdPerson,
        PointLookAt,
        Stand,
        Editor
    }

    public static PlayerCamera main;    //za lakše dobivanje komponente u drugim skriptama

    public PlayerCameraMode mode = PlayerCameraMode.ThirdPerson;    //trenutni način rada

    public float minDistance = 5f;  //min i max udaljenosti od igrača
    public float maxDistance = 15f;

    public float smoothFactorPosition = 5f; //koliko glatko će se pozicija kamere mijenjati u ThirdPerson načinu rada

    public float distance = 10f;    //trenutna udaljenost kamere od igrača

    public float yAngleMinTP = 0f, yAngleMaxTP = 80f, yAngleMinFP = 0f, yAngleMaxFP = 175f; //min i max kutevi na y rotacijskoj osi za Third Person i druge načine rada

    public float xRotationFactor = 2f;  //brzina mjenjanja rotacije na x rotacijskoj osi

    //public float zoomFactor = 2.333333f;

    public Vector3 lookAtPointPositionOffset;   //dodatni offset koji se pribraja igračevoj poziciji kada kamera gleda u njega

    public Vector3[] points;    //point-i PointLookAt mode-a

	public Vector3 pointOffset; //dodatni offset koji se pribraja poziciji point-a prilikom traženja najbližeg

    public int currentPoint = 0;    //trenutni point na kojemu je kamera

    Transform player;   //Transform komponenta player-a

    public float mx, my;    //mouse x i mouse y, određuju pomak u ThirdPerson načinu rada

    public float switchSpeed = 0.85f;   //brzina mjenjanja point-a u PointLookAt mode-u

    //float w = 0f;

    public float editorSpeed = 3f;  //brzina pomicanja kamere u editoru

    public float editorRotationSpeed = 2f;  //brzina rotiranja u editoru

    public float editorScrollSpeed = 10f;   //brzina zumiranja u editoru

    public AudioClip menuMusic, gameMusic;  //zvučni klipovi za muziku u igri

    float rotationX = 0, rotationY = 0; //varijable koje pamte rotaciju

    public AudioSource a7;     //za mijenjanje zvuka

    public UnityEngine.UI.Slider volumeSlider;  //Slider za mijenjanje jačine zvuka

    float d = 0;    //udaljenost kamere i igrača

    public void SetVolume()  //postaljanje jačine zvuka (zvano od UI slider-a)
    {
        a7.volume = volumeSlider.value;
    }

    void Awake()    //inicijalizacija...
    {
        main = this;
        a7 = GetComponent<AudioSource>();
        a7.clip = menuMusic;
        a7.loop = true;
        a7.Play();
        if (Scene.player != null)
            player = Scene.player.GetComponent<Transform>();
        d = -main.distance;
    }

    void Update()   //svakog frame-a dobavljaj vrijednosti za vrijable koje služe kao input (mx i my)
    {
        mx += Input.GetAxis("Mouse X");
        /*if(!Scene.player.grounded)
            w = 0f;
        w += Input.GetAxis("Vertical");*/
        if (mode == PlayerCameraMode.ThirdPerson)
            my = Mathf.Clamp((my - Input.GetAxis("Mouse Y")), yAngleMinTP, yAngleMaxTP);
        else
            my = Mathf.Clamp((my - Input.GetAxis("Mouse Y")), yAngleMinFP, yAngleMaxFP);
    }

	void LateUpdate () {    //ova metoda poziva se nakon Update metode svakog frame-a, 
        if (player == null)
            player = Scene.player.transform;
        switch (mode)   //svaki mode radi na zaseban način
        {
            case PlayerCameraMode.ThirdPerson:  //igrač može kameru pomicati mišem oko njega
                d = -distance;
                RaycastHit h;
                Physics.queriesHitTriggers = !Physics.queriesHitTriggers;
                if (Physics.Linecast(player.position, transform.position, out h))
                {
                    d = -Vector3.Distance(player.position, h.point);
                    if (d > -1f) d = -1f;
                }
                Physics.queriesHitTriggers = !Physics.queriesHitTriggers;

                Vector3 dir = new Vector3(0, 0, d);

                Quaternion rot = Quaternion.Euler(my, mx * xRotationFactor, 0f);

                transform.position = Vector3.Lerp(transform.position, (player.position + lookAtPointPositionOffset) + rot * dir, smoothFactorPosition * Time.deltaTime);

                transform.LookAt(player.position + lookAtPointPositionOffset);

                break;
            case PlayerCameraMode.PointLookAt:  //kamera se pommiče po pointima najbližim igraču i gleda u njega
                if (points.Length < currentPoint)
                    currentPoint = 0;
                    float cd = Vector3.Distance(points[currentPoint] + pointOffset, player.position);

                    for (int i = 0; i < points.Length; i++)
                    {
                        if (i != currentPoint && Vector3.Distance(points[i] + pointOffset, player.position) < cd)
                            currentPoint = i;
                    }

                    /*if (currentPoint + 1 < points.Length && cd > Vector3.Distance(points[currentPoint + 1], player.position))
                        currentPoint++;

                    if (currentPoint - 1 >= 0 && cd > Vector3.Distance(points[currentPoint - 1], player.position))
                        currentPoint--;*/

                    transform.position = Vector3.Lerp(transform.position, points[currentPoint], switchSpeed * Time.deltaTime);

                    transform.LookAt(player);
                
                break;
            case PlayerCameraMode.Stand:    //stoji...
                //nothing
                break;
            case PlayerCameraMode.Editor:   //detaljna kontrola nad kamerom, ako je kursor unutar preview prozora kamera se može pomicati vertikalno i horizontalno, hover-ati, rotirati dok je pritisnuta desna tipka miša i pomicati naprijed/nazad kotačićem
                //print(Input.mousePosition);
                bool mouseIsInGameWindow = (Input.mousePosition.x / Screen.width >= 0.25f && Input.mousePosition.x / Screen.width <= 0.75f) && (Input.mousePosition.y / Screen.height >= 0.33f) && !GameEditor.editingSettings;
                if (mouseIsInGameWindow)
                {
                    if (Input.GetKey(KeyCode.Mouse2))   //hover
                    {
                        transform.position += transform.TransformDirection(new Vector3(-Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"), 0)) * editorSpeed * Time.deltaTime;
                    }
                    else    //obični pomak strelicama
                    {
                        Quaternion r = transform.rotation; 
                        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, transform.eulerAngles.z);
                        transform.Translate((new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"))) * editorSpeed * Time.deltaTime);
                        transform.rotation = r;
                    }
					if (Input.GetKey (KeyCode.Mouse1)) {    //rotiraj ako je pritisnuta desna tipka miša
                        //Vector3 r = transform.eulerAngles + new Vector3 (Input.GetAxis ("Mouse Y"), -Input.GetAxis ("Mouse X")) * editorRotationSpeed * Time.deltaTime;
                        //transform.rotation = Quaternion.Euler(r);
                        
                        rotationX += Input.GetAxis("Mouse X") * editorRotationSpeed * Time.deltaTime;
                        rotationY += Input.GetAxis("Mouse Y") * editorRotationSpeed * Time.deltaTime;
                        Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                        Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);
                        transform.localRotation = Quaternion.identity * xQuaternion * yQuaternion;
                        //transform.RotateAround(transform.TransformPoint(0,0,5), new Vector3(Input.GetAxis("Mouse Y") * Time.deltaTime,Input.GetAxis("Mouse X") * Time.deltaTime,0), editorRotationSpeed);
                    }
                   	transform.Translate(0, 0, Input.mouseScrollDelta.y * editorScrollSpeed);    //scoll (pomak naprijed nazad)
                } 
                break;
            default:
                break;
        }
    }

    public void UpdateEditorRotation() {    //za vraćanje rotacije u editoru
        Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
        Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);
        transform.localRotation = Quaternion.identity * xQuaternion * yQuaternion;
    }

    //za prikaz point-ova u editoru
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Gizmos.color = Color.red;
        if(mode == PlayerCameraMode.PointLookAt)
        {
            for(int i = 0; i < points.Length-1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }
#endif
    }
}
