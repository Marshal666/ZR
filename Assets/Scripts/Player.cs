using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// kontrolna klasa za igrača
/// </summary>

public class Player : MonoBehaviour
{

    [System.Serializable]
    public struct Jump  //info o skokovima
    {
        public float height;
        public float time;
        public float gravity;
        public float velocity;

        public Jump(float h, float t)   //v=sqrt(2*a*s), g = 2*s/(t/2)^2
        {
            height = h;
            time = t;
            gravity = 2 * h / ((t / 2) * (t / 2));
            velocity = Mathf.Sqrt(2 * gravity * h);
        }

        public void Recalculate() 
        {
            gravity = 2 * height / ((time / 2) * (time / 2));
            velocity = Mathf.Sqrt(2 * gravity * height);
        }

    }

    CapsuleCollider cs; //collider objekta

    public float hp = 100f; //trenutni health point-i

    public float hp_max = 100f; //max mogući hp-ovi

    public float speed = 2.5f;  //brzina pomicanja

    public int currentJump = 0; //index trenutnog skoka u polju skokova
    public Jump[] jumps;    //polje skokova

    public float gravity = -9.81f;  //gravitacija (ako igrač nije napravio skok)

    public Animator anim;   //animator komponenta za izvođenje animacija 

    public bool grounded;   //da li je igrač na tlu?

    public Rigidbody rig;  //Rigidbody componenta objekta

    public Vector3 motion = Vector3.zero;   //vector pomaka u svakom fizičkom(FixedUpdate) frameu
    Vector3 platformVelocity = Vector3.zero, jumpVelocity = Vector3.zero;   //pribrajaju se svakog fizičkog frame-a na motion

    public Vector2 inputVector = Vector2.zero;  //input za naprijed/nazad, lijevo/desno
    public bool jump = false;   //input za skok
    public bool isAlive = true; //da li je igrač živ?

    public float outWhenHit = 2.0f; //faktor za out motion
    public Vector3 outMotion;   //pomak izazvan vanjskim utjecajima (npr. sudar s AI-em)

    public float rayLen = 1.01f;    //duljina linije koja se cast-a u provjeri da li je igrač na tlu

    public SkinnedMeshRenderer mr;  //animirana mesh renderer komponenta igrača (dino)

    public LayerMask groundLayers;  //layeri objekta koji se smatraju tlom prilikom provjere da li je igrač na tlu

    public static Player main;  //za lakše dohvačanje u drugim skriptama (uvjek postoji točno jedan igrač u sceni!)

    public static bool playing = false; //da li igrač igra?

    GameObject waterSplashParticle; //objekt koji se pokaže dok igrač padne u vodu

    void Awake() //inicijalizacija prije prvog framea
    {
        main = this;  
    }

    void Start()    //inicijalizacija pri prvom frame-u
    {
        rig = GetComponent<Rigidbody>();
        cs = GetComponent<CapsuleCollider>();
        for (int i = 0; i < jumps.Length; i++)  //za svaki slučaj potrebno je rekalkulirati svaki skok (promjenom visine i trajanja skoka u editoru gravitacija i početna brzina se ne mijenjaju!)
            jumps[i].Recalculate();
        waterSplashParticle = Instantiate(GameData.ParticleEffects[2]); //kreiraj efekt "pljuska" vode, isključi ga, dodaj mu koponentu za automatsko isklučivanje i vrijeme toga postavi u 1 s
        waterSplashParticle.SetActive(false);
        waterSplashParticle.AddComponent<AutoDisable>().time = 1.0f;
    }

    bool si = false;    //dali se izvodi korutina SwitchIdle?
    bool ch = false;    //dali se izvodi korutina smoothChangeIdleState?

    IEnumerator smoothChangeIdleState()     //u jednoj sekundi prebacuje IdleRand iz animatora u suprotnu vrijednost (1->0, 0->1)
    {
        ch = true;
        float aim = anim.GetFloat("IdleRand") == 0f ? 1f : 0f, vel = 0f, current = anim.GetFloat("IdleRand");
        while (current != aim)
        {
            vel += Time.deltaTime;
            current = Mathf.Lerp(aim == 0f ? 1f : 0f, aim, vel);
            anim.SetFloat("IdleRand", current);
            //print("anim: " + aim + " current: " + current);
            yield return new WaitForSeconds(0.04f);
        }
        ch = false;
    }

    IEnumerator SwitchIdle()    //sve dok je igrač živ, svakih 3-5 sekundi generiraj random 0 ili 1 te ako su različiti od IdleRand-a, promjeni IdleRand u smoothChangeIdleState korutini
    {
        si = true;
        while (isAlive)
        {
            if (Mathf.Round(Random.Range(0f, 1f)) != anim.GetFloat("IdleRand") && !ch)
                StartCoroutine(smoothChangeIdleState());
            yield return new WaitForSeconds(Random.Range(3f, 5f));
        }
        si = false;
    }

    void Update()   //poziva se svakog frame-a u igri
    {
        if (isAlive && !si) //ako SwitchIdle nije pokrenut, pokreni ga
            StartCoroutine(SwitchIdle());
        if (playing && Scene.currentGameState != Scene.GameState.editing)
        {
            if (transform.position.y < Scene.minHeight) //ako je igrač ispod min. dozvoljene y razine level-a, on umire
                Die();
            if (isAlive)
            {
#if UNITY_STANDALONE    //dio koda koji se prevodi samo za PC platforme (Win, Mac i Linux)
                if (!jump)
                    jump = Input.GetKeyDown(KeyCode.Space); //jump je vrijednost 
#endif
                float angle = Mathf.Atan2(inputVector.x, inputVector.y) * Mathf.Rad2Deg;    //kut koji input vector čini sa y+ osi
                if (anim.GetBool("Walking"))    //ako se igrač pomiče, pomiči i njegovu rotaciju ovisno o načinu rada kamere pomoću Rigidbody komponente
                    switch (PlayerCamera.main.mode)
                    {
                        case PlayerCamera.PlayerCameraMode.ThirdPerson: //Slerp daje klaki pomak rotacije
                            //transform.eulerAngles = new Vector3(transform.eulerAngles.x, Mathf.LerpAngle(transform.eulerAngles.y, Scene.mainCamera.transform.eulerAngles.y + angle, 50 * Time.deltaTime), transform.eulerAngles.z);
                            rig.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, Mathf.MoveTowardsAngle(transform.eulerAngles.y, Scene.mainCamera.transform.eulerAngles.y + angle, 50), transform.eulerAngles.z), 15 * Time.fixedDeltaTime));
                            break;
                        case PlayerCamera.PlayerCameraMode.PointLookAt:
                            //transform.eulerAngles = new Vector3(transform.eulerAngles.x, Mathf.LerpAngle(transform.eulerAngles.y, Scene.mainCamera.transform.eulerAngles.y + angle, 50 * Time.deltaTime), transform.eulerAngles.z);
                            rig.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, Mathf.LerpAngle(transform.eulerAngles.y, Scene.mainCamera.transform.eulerAngles.y + angle, 50 * Time.fixedDeltaTime), transform.eulerAngles.z), 15 * Time.fixedDeltaTime));
                            break;
                        default:
                            //transform.eulerAngles = new Vector3(transform.eulerAngles.x, Mathf.LerpAngle(transform.eulerAngles.y, angle, 50 * Time.deltaTime), transform.eulerAngles.z);
                            rig.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, Mathf.LerpAngle(transform.eulerAngles.y, angle, 50 * Time.deltaTime), transform.eulerAngles.z), 15 * Time.fixedDeltaTime));
                            break;
                    }
            }
        }
    }

    public bool killingAI = false; //da li je igrač unutar trigger-a koji uništava AI objekte
    public bool jumpPending = false;//jeli skok u izvođenju

    void FixedUpdate()  //fizički frame igre (poziva se svakih 20 ms(ovisno o Fixed Timestep-u u postavkama vremena))
    {
        if (playing && Scene.currentGameState != Scene.GameState.editing)
        {
            rig.useGravity = false;
            if (rig.IsSleeping())
                rig.WakeUp();   //ako igrač igra, uključi Rigidbody komponentu koja simulira fizičke događaje
#if UNITY_STANDALONE
            inputVector = (new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))).normalized;   //input vektor ovisi o vrijednostima za horizontalne i vetikalne osi i treba biti normaliziran (magnituda = 1)
#endif
            float mag = inputVector.magnitude;  //ili je 1 (strelice ili jedna od wsad tipki je pritisnuta) ili 0 (ništa nije pritisnuto)
            //float angle = Mathf.Atan2(inputVector.x, inputVector.y) * Mathf.Rad2Deg; //kut koji input vector čini sa y+ osi
            Physics.queriesHitTriggers = false; //privremeno isključi pogađanje trigger collidera sa zrakama
            grounded = Physics.SphereCast(new Ray(transform.position, Vector3.down), 0.4f, rayLen, groundLayers); //provjeri dali je igrač na tlu "šaljući kuglu" prema dolje
            Physics.queriesHitTriggers = true;
            anim.SetBool("Grounded", grounded); //informiraj animatora da je igrač na tlu
            motion = transform.TransformDirection(Vector3.forward * mag * speed) + Vector3.up * rig.velocity.y + platformVelocity + (outMotion * outWhenHit);   //pomak je relatvan s obzirm na roataviju igrača, ovisan o brzini na y osi (za skokove i gravitaciju) i vanjske pomake (od platformi i sudara sa AI objektima)
            platformVelocity = Vector3.zero;    //resertiraj platform velocity na nul vektor
            anim.SetBool("Walking", mag > 0.01f ? true : false);    //ako je magnituda input vekora veća od nule, animator smatra se da igrać hoda
            if (grounded && !killingAI && !jumpPending) //ako je igrač na tlu i nije u sudaru sa triggerom AI objekta resertiraj njegove skokove (omogućava novu seriju skokova)
                currentJump = 0;
            if (jump && !jumpPending)   //ako je zatražen skok i trenutni se ne obavlja
            {
                if (currentJump + 1 <= jumps.Length)
                {   //skoči ako je moguće
                    anim.SetTrigger("Jump"); //informiraj animator o skoku, event u animaciji će pokrenuti DoJump() Metodu
                    jumpPending = true; //priprema za skok (JumpPrepare klip) počinje
                    
                }
                jump = false;   //resertiraj jump
                //print("jumpy " + currentJump);    //korištenu u debug-u
            }
            if (jump && jumpPending && currentJump == jumps.Length) //svaki novi skok može se izvesti nakon kratkog vremena, ako trenutni skok nije zadnji, novi se može odraditi u kratkom vremenu
                jump = false;   //resertiraj jump
            if (jumpVelocity.y != 0f)   //ako je skok zadan
            {
                motion.y = jumpVelocity.y;  //postavi brzinu skoka na zatraženu
                jumpVelocity = Vector3.zero;    //resertiraj jumpVelocity na nul vektor
            }
            rig.velocity = motion;  //postavi trenutnu brzinu gibanja na sve što je prije bilo uračunato
            
            if (currentJump == 0)   //gravitacija
            {
                rig.AddForce(Vector3.down * -gravity);  //"normalna" tj. zadana gravitacija
            }
            else
            {
                rig.AddForce(Vector3.down * +jumps[currentJump - 1].gravity);   //gravitacija skoka (potrebno kako bi skok trajao onoliko koliko je zadan)
            }
        }
        else    //ako igrač ne igra, isključi Rigidbody komponentu
        {
            if (!rig.IsSleeping())  
                rig.Sleep();
        }
        if (Scene.currentGameState == Scene.GameState.playing && !isAlive)  //ako je igrač umro, vuci ga prema dolje
            rig.AddForce(Vector3.up * -10);
        //print(grounded + " v " + rig.velocity + " m " + motion);  //korišteno za debug...
        //print(currentJump);
    }

    public void DoJump()    //metoda za izvođenje skokova
    {
        if (currentJump + 1 <= jumps.Length)    //ako je skok moguć
        {
            jumpVelocity.y = jumps[currentJump].velocity;   //postavi jump velocity na brzinu trenutnog skoka
            currentJump++;  //postavi index trenutnog skoka na index sljedećeg
            grounded = false;   //smatra se da igrač nemože biti na tlu ako je upravo skočio
            StartCoroutine(AllowAnotherJump()); //za mogučnost drugog skoka treba prijeći određeno vrijeme
            //anim.SetTrigger("Jump");    
            //print("jump: " + currentJump);
        }
        
    }

    bool aaj = false;   //dali je korutina AllowAnotherJump u izvođenju 

    IEnumerator AllowAnotherJump()  //nakon određenog vremena (0.21 s) omogućuje drugi skok
    {
        if (!aaj)
        {
            aaj = true;
            yield return new WaitForSeconds(0.21f);
            jumpPending = false;
            aaj = false;
            yield break;
        }
    }

    void OnTriggerEnter(Collider col)   //poziva se kada se igrač sudari sa collider-om koji je trigger
    {
        if (playing)
        {
            if (Scene.currentGameState != Scene.GameState.editing)
            {
                if (col.tag == "AI")    //ako je objekt s kojim se igrač sudario tipa AI
                {
                    AI ai = col.GetComponent<AI>(); //referenca za lakše i brže baratanje komponentom
                    if (transform.position.y - cs.height / 2f > col.transform.position.y + col.bounds.size.y / 2f && ai.alive)  //ako je igrač iznad AI-jevog trigger collider-a i AI je je živ
                    {
                        ai.gainHp(-1);  //ai dobiva štetu (umire jer mu je hp 1)
                        //if (currentJump + 1 > jumps.Length)    //resertiraj skokove ako je potrebno
                        //    currentJump = 0;
                        anim.CrossFade("JumpPrepare", 0.18f, 0);   //napravi jedan skok od AI objekta
                        killingAI = true;   //uloga ove varijable je uglavnom ta da se u FixedUpdate-u currentJump opet ne resetira na nulu jer kad igrač skoči ili padne na AI raycast gotovo uvijek detektira da je on došao na tlo
                        jumpPending = false;    //poništi trenutni skok (ako uopče ima skoka)
                                                //print("ai hit " + currentJump);   //korišteno prilikom debug-a
                    }
                }
                if(col.gameObject.layer == 4)   //ako je objekt voda
                {
                    if(rig.velocity.y < -0.5)
                    {
                        waterSplashParticle.transform.position = new Vector3(transform.position.x, col.transform.position.y + 0.1f, transform.position.z);  //postavi splash objekt na xz poziciju igrača i malo više od vode
                        waterSplashParticle.SetActive(true);
                    }
                }
                if(col.gameObject.layer == 11)  //ako je objekt municija AI-a
                {
                    GainHp(-1);
                    Ammo amm = col.gameObject.GetComponent<Ammo>(); //isključi taj ammo
                    amm.StopAllCoroutines();
                    ParticleSystem ps = amm.GetComponent<ParticleSystem>();
                    if (ps)
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    for (int i = 0; i < amm.transform.childCount; i++)
                    {
                        ps = amm.transform.GetChild(i).GetComponent<ParticleSystem>();
                        if (ps)
                            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                    if (amm.onCollisionParticles)
                    {
                        amm.onCollisionParticles.SetActive(true);
                        amm.onCollisionParticles.transform.position = (amm.transform.position + transform.position) / 2f;
                        amm.onCollisionParticles.GetComponent<ParticleSystem>().Play();
                    }
                    amm.gameObject.SetActive(false);
                }
            }
        }
    }

    void OnTriggerExit(Collider col)    //poziva se kada je igrač izašao iz sudara sa trigger collider-om
    {
        if (Scene.currentGameState != Scene.GameState.editing && playing)
        {
            if (col.tag == "AI")
            {
                //print("ai out " + currentJump);
                killingAI = false;  //dopusti da grounded ponovo može resertirati serije skokova
            }
        }
    }

    void OnCollisionStay(Collision col) //poziva se sve dok je igrač u kontaktu sa nekim collider-om
    {
        if (Scene.currentGameState != Scene.GameState.editing)
        {
            if (playing)
            {
                if (col.collider.tag == "MovingPlatform")   //ako igrač igra i ako je na pomičnoj platformi postavi njeguvu brzinu na brzinu platforme tako da se pomiče s platformom
                {
                    platformVelocity = col.collider.GetComponent<Rigidbody>().velocity;
                }
            }
        }
    }

    public void Die()   //poziva se kada igrač umire
    {
        isAlive = false;
        rig.useGravity = true;
        Scene.main.playerDied();    //informiraj scenu o smrti igrača
        anim.SetBool("Dead", true);     //informiraj animator-a o smrti
#if UNITY_EDITOR
        print("death"); //ispisuje se samo u editoru
#endif
    }

    public void Win()   //poziva se kada igrač pobijedi (sudari se sa WinTriggerom)
    {
#if UNITY_EDITOR
        print("won");   //ispisuje se samo u editoru
#endif
        playing = false;
        Scene.main.playerWon(); //informiraj scenu o tome da je level gotov
        anim.SetBool("Jump", false);
        anim.SetBool("Walking", false);
        anim.SetBool("Idle", true);
        anim.CrossFade("Idles", 0.4f);
    }

    public void GainHp(float gain)  //promjena hp-a igrača ( < 0 daje damage (štetu), > 0 daje liječenje (healing))
    {
        hp = Mathf.Clamp(hp + gain, 0f, hp_max);    //postavi hp u interval od 0 do hp_max
        if (hp <= 0f)
            Die();  //ako je hp <= 0 igrač je izgubio...
    }

    public IEnumerator GainSpeed(float boost, float seconds)    //povećava brzinu za određen boost na određeno vrijeme (za speed powerup-ove), u igri se ne koristi
    {
        speed += boost;
        yield return new WaitForSeconds(seconds);
        speed -= boost;
        yield break;
    }

}