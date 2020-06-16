using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Komponenta koja kontrolira AI objekte koji se bore protiv player-a
/// Po ponašanju AI može patrolirati (ići sa jedne točke na drugu), biti napadač (kakda igrač dođe blizu, on se pomiče prema njemu), pucač (kada igrač dođe dovoljo blizu njega, on ga krene pucati)
/// </summary>


[RequireComponent(typeof(Rigidbody))]   //koristi Rigidbody komponentu za pokrete
public class AI : MonoBehaviour {

    public float hp = 100f,         //trenutni hp
                 hp_max = 100f;     //max mogući hp

    public enum AIType { Patrol, Attack, Shooter }  //tipovi

    public AIType type;     //tip AI-a

    public float speed;                 //brzina pomicanja
    public bool rotateStop = true;      //hoće li se Ai rotitati u smjeru sljedećeg patrol pointa (ako je tipa Patrol) ili se može rotirati (ako je tipa Attack ili Shooter)
    public float rotSpeed = 1.5f;       //faktor rotiranja

    public int currentPoint = 0;        //trenutni patrol point
    public Vector3[] patrolPoints;      //patrol point-i relativni s obzirom na početnu poziciju objekta u početku levela
    public float toleratedDistance;     //udaljenost od trenutnog point-a za koju se smatra da je AI došao do point-a

    public float activeDistance;        //ako je igrač bliži AI-u od te udaljenosti i ako je AI tipa Attack ili Shooter, napad na igrača počinje

    Rigidbody rig;  //referenca na Rigidbody komponentu objekta

    Vector3 relativePos;    //pozicija objekta pri početku levela

    public bool alive = true;   //da li je AI živ?

    public float onGroundRaycastLenght = 0.501f;    //duljina zrake koja provjerava da li je AI na tlu

    public bool checksBeforeItGoes = true;          //dali je AI "pametan", tj provjerava što ga čeka na putu

    public float forwardRacastCheckLength, downRaycastCheckLength;  //zrake koje "pametan" AI koristi da provjeri put

    public GameObject ammo; //za Shooter AI-ove, objekt (municija) koji AI baca prema igraču

    public float ammoLifeTime = 3f; //koliko dugo je objekt ammo uključen

    GameObject[] ammos;                 //svi ammo-i ovog AI-a
    int ca = 0;                         //index trenutnog ammo objekta u polju ammos
    bool shooting = false;              //dali AI puca?

    public float rateOfFire;            //koliko ammo objekata ovaj AI može ispaliti u sekundi

    public float relaxTime = 1f;        //dodatno vrijeme čekanja nakon što AI ispali metak

    public Vector3 shootPointOffset;    //pozicija s obzirom na poziciju i rotaciju AI-a iz koje se ispaljuje ammo

    public float shootHeight = 7f;      //max visina koju ammo dostiže pri putu do player-a

    public float damageOnCollision;     //damage koji player dobije kada se sudari s ovim objektom

    public Animator anim;              //za animaicije..
                                        /// <summary>
                                        /// idle
                                        /// walk
                                        /// shoot
                                        /// death
                                        /// turn        //180°
                                        /// nothing
                                        /// </summary>


    public Vector3 deathScaleFactor = Vector3.one;    //vektor s kojim se scale collider-a množi dok AI umre

    //public float firingStartDelay = 0f;               //vrijeme u sec prije nego što Shooter AI krene pucati

    void playAnim(string clipName)  //"robusna" metoda za pokretanje animacija
    {
        if(anim)
        {
            bool contains = false;
            for (int i = 0; i < anim.parameterCount; i++)   //provjerava postoji li parametar u animatoru, ako postoji i ako je tipa bool, postavlja ga u true
                if (anim.parameters[i].name == clipName)
                {
                    contains = true;
                    break;
                }
            if(contains)
            {
                for (int i = 0; i < anim.parameterCount; i++)
                    if (anim.parameters[i].name == clipName)
                        anim.SetBool(clipName, true);
                    else if (anim.parameters[i].type == AnimatorControllerParameterType.Bool)
                        anim.SetBool(anim.parameters[i].name, false);
            }
        }
    }

    void Start() {                          //inicijalizacija
        rig = GetComponent<Rigidbody>();
        relativePos = rig.position;
        if(ammo)
        {
            ammos = new GameObject[(((int)rateOfFire) * ((int)ammoLifeTime)) + 1];      //ukupni broj ammo-a = KolikoIhMožeIspalitiUSekundi * KolikoAmmoTraje + 1,
                                                                                        //više ammo-a za Shooter AI-a od te brojke nije potrebno, ammo-i se ne brišu i ponovo stvaraju,
                                                                                        //nego se samo isključiju i ponovo uključuju radi boljih performansi (object pooling)
            for (int i = 0; i < ammos.Length; i++)  //svakoj referenci u polju ammos-a dodaj novi novi ammo i inicijaliziraj ga
            {
                ammos[i] = Instantiate(ammo);
                ammos[i].transform.parent = Scene.rootObject.transform;
                if (!ammos[ca].GetComponent<Ammo>())
                    ammos[ca].AddComponent<Ammo>();
                ammos[i].GetComponent<Ammo>().lifeTime = ammoLifeTime;
                ammos[i].SetActive(false);
            }
        }
        playAnim("nothing");
    }

    void FixedUpdate()      //fizički frame
    {
        if (alive && Scene.player.isAlive && Scene.currentGameState == Scene.GameState.playing) //preduvjeti za rad
        {
            if (transform.position.y < Scene.minHeight) //baš kao i igrač, AI umire ako je ispod min. dozvoljene razine
                Die();
            switch (type)       
            {
                case AIType.Patrol:         //ako je AI tipa Patrol
                    Vector3 direction = (-(rig.position - (relativePos + patrolPoints[currentPoint]))).normalized;      //smjer prema sljedećem point-u
                    direction.y = 0;                                                                                    //smjer treba biti po XZ plohi
                    direction.Normalize();
                    //Debug.DrawLine(transform.position, transform.position + direction*5, Color.green);
                    rig.velocity = (direction * speed * Time.fixedDeltaTime);                                           //postavi brzinu da pomiče AI preme sljedećem point-u
                    playAnim("walk");
                    if (Vector3.Distance(rig.position, patrolPoints[currentPoint] + relativePos) <= toleratedDistance)  //ako je AI stigao na point
                    {
                        if (rotateStop)     //ako je rotateStop true, rotiraj ga prema sljedećm point-u
                        {
                            Quaternion dir = Quaternion.LookRotation(-direction);
                            Quaternion rot = Quaternion.RotateTowards(rig.rotation, dir, rotSpeed * Time.fixedDeltaTime);   //rotiraj trenutnu rotaciju prema potrebnoj
                            rig.MoveRotation(rot);
                            playAnim("turn");
                            if (Quaternion.Dot(rig.rotation, dir) > 0.99998f)   //ako su rotacije gotovo jednake
                            {
                                currentPoint = (currentPoint + 1) % patrolPoints.Length;    //idi na sljedeći point
                            }
                        }
                        else
                        {
                            currentPoint = (currentPoint + 1) % patrolPoints.Length;    //idi na sljedeći point
                        }
                        rig.velocity = Vector3.zero;    //zaustavi objekt na neko vrijeme
                    }
                    break;
                case AIType.Attack:         //ako je AI tipa Attack
                    bool onGround = Physics.Raycast(rig.position, Vector3.down, onGroundRaycastLenght);         //proevjeravaj dali je AI na tlu
                    playAnim("idle");
                    //Debug.DrawRay(transform.position, -(transform.position - Scene.player.transform.position), Color.red);
                    if(Vector3.Distance(rig.position, Scene.player.transform.position) <= activeDistance &&  onGround)  //ako je AI na tlu i igrač mu je pre blizu
                    {
                        Vector3 directionToPlayer = (Scene.player.transform.position - transform.position);  //smjer prema igraču
                        directionToPlayer.y = 0f;
                        directionToPlayer.Normalize();  //normaliziraj (postavi duljunu na 1) po XZ plohi
                        //Debug.DrawLine(transform.position, transform.position + directionToPlayer*5, Color.green);
                        playAnim("walk");
                        if (rotateStop)     //ako se koristi rotacija, rotiraj AI prema igraču
                        {
                            rig.MoveRotation(Quaternion.RotateTowards(rig.rotation, Quaternion.LookRotation(directionToPlayer), rotSpeed * Time.fixedDeltaTime));
                        }
                        
                        if (checksBeforeItGoes) //ako je AI "pametan"
                        {
                            //Debug.DrawLine(rig.position, rig.position + directionToPlayer * forwardRacastCheckLength, Color.magenta);
                            //Debug.DrawLine(rig.position + directionToPlayer * forwardRacastCheckLength, (rig.position + directionToPlayer * forwardRacastCheckLength) + Vector3.down * downRaycastCheckLength, Color.red);
                            //pošalji jednu zraku naprijed, drugu zraku od kraja te zrake prema dolje, ako nijedna detektira ništa, AI se slobodno može kretati, u suprotnom AI samo stoji
                            if (!Physics.Raycast(rig.position, Vector3.forward, forwardRacastCheckLength, GameData.main.noPlayer) && Physics.Raycast(rig.position + directionToPlayer * forwardRacastCheckLength, Vector3.down, downRaycastCheckLength, GameData.main.noPlayer))
                            {
                                rig.velocity = directionToPlayer * speed * Time.fixedDeltaTime;
                            } else
                            {
                                rig.velocity = Vector3.zero;
                            }
                        } else
                        {
                            rig.velocity = directionToPlayer * speed * Time.fixedDeltaTime;
                        }
                    } else
                    {
                        playAnim("idle");
                    }
                    break;
                case AIType.Shooter:    //ako je AI tipa Shooter
                    if (!shooting)
                        playAnim("idle");
                    if (Vector3.Distance(rig.position, Scene.player.transform.position) <= activeDistance) {    //ako je igrač preblizu
                        Vector3 directionToPlayer = (Scene.player.transform.position - rig.position);
                        directionToPlayer.y = 0f;
                        directionToPlayer.Normalize();
                        //print(directionToPlayer + " " + rig.rotation);
                        if (rotateStop)     //ako se AI može rotirati, rotiraj ga prema player-u
                        {
                            //print("h");
                            transform.rotation = (Quaternion.RotateTowards(rig.rotation, Quaternion.LookRotation(directionToPlayer), rotSpeed * Time.fixedDeltaTime));
                        }
                        if(ammo && Quaternion.Dot(rig.rotation, Quaternion.LookRotation(directionToPlayer)) > 0.99998f)  //ako ima municije i ako je AI usmjeren prema igraču, počni pucati (ako već nije započeto)
                        {
                            
                            if(!shooting)
                                StartCoroutine(shoot());
                        }
                    }
                    break;
                default:
                    break;
            }
        } else  //ako preduvijeti za rad nisu ispunjeni, AI ne radi ništa, jedino u meniju izvodi animaciju idle
        {
            rig.velocity = Vector3.zero;
            rig.angularVelocity = Vector3.zero;
            if (Scene.currentGameState == Scene.GameState.editing)
                playAnim("nothing");
            if (Scene.currentGameState == Scene.GameState.menu)
                playAnim("idle");
        }
    }

    IEnumerator shoot()     //za Shooter AI-ove, korutina ispaljuje metke na igrača
    {
        shooting = true;
        playAnim("shoot");
        //print("shooting");
        //if (firingStartDelay > 0.00001f)
        //    yield return new WaitForSeconds(firingStartDelay);
        while(alive && Vector3.Distance(rig.position, Scene.player.transform.position) <= activeDistance && Player.main.isAlive)   //sve do je AI živ i igrač je u kritičnoj blizini
        {
            if (!anim)  //animirani AI puca po eventu iz animacije
            {
                shootAmmo();
                yield return new WaitForSeconds(1f / rateOfFire);
                ca = (ca + 1) % ammos.Length;   //pripremi sljedeći ammo za paljbu
                yield return new WaitForSeconds(relaxTime);
            } else
            {
                playAnim("shoot");
                yield return new WaitForFixedUpdate();
            }
        }
        shooting = false;
    }

    public void shootAmmo()
    {
        
        ammos[ca].SetActive(true);                                                                                  //aktiviraj trenutni ammo
        ammos[ca].transform.position = transform.position + transform.TransformDirection(shootPointOffset);         //postavi ammo na početno mjesto uz offset
                                                                                                                    //Ispali ammo, brzina se računa formulom:
                                                                                                                    //Vxz   = Dx / (sqrt(-2*h/g) + sqrt(2*(Dy-h)/g)) za XZ plohu
                                                                                                                    //Vy    = sqrt(-2*g*h) za Y os
                                                                                                                    //Dx    - udaljenost igrača i ovog AI-a po XZ plohi
                                                                                                                    //h     - shootHeight
                                                                                                                    //g     - gravitacija po y osi
                                                                                                                    //Dy    - udaljenost igrača i ovog AI-a po y osi

        ammos[ca].GetComponent<Rigidbody>().velocity =
            transform.TransformDirection(Vector3.forward * (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(Scene.player.transform.position.x + Player.main.rig.velocity.x, 0f, Scene.player.transform.position.z + Player.main.rig.velocity.z)) / (Mathf.Sqrt((-2f * shootHeight) / Physics.gravity.y) + Mathf.Sqrt((2f * ((-Mathf.Abs(Scene.player.transform.position.y - transform.position.y)) - shootHeight)) / Physics.gravity.y)))
            + Vector3.up * Mathf.Sqrt(-2f * Physics.gravity.y * shootHeight));
        ammos[ca].GetComponent<Ammo>().StartCoroutine("end");   //ammo se isključuje nakon nekog vremena
        if (anim)
            ca = (ca + 1) % ammos.Length;
    }

    public void OnCollisionEnter(Collision col) //ako se igrač sudai s ovim AI-om, daj mu damage i odbaci ga u suprotom smjeru
    {
        if (alive)
        {
            if (col.collider.tag == "Player")
            {
                Scene.player.GainHp(damageOnCollision);
                Scene.player.outMotion = -(transform.position - Scene.player.transform.position).normalized * Scene.player.outWhenHit;
                //print("jump out");
            }
        }
    }

    public void OnCollisionExit()   //kada se igrač prestane sudarati s ovim objektom, poništi sve vanjske pokrete
    {
        //if (alive)
            Player.main.outMotion = Vector3.zero;
    }

    public void gainHp(float gain)  //za dodavanje/uklanjanje hp-as
    {
        hp = Mathf.Clamp(hp + gain, 0f, hp_max);
        if (hp <= 0f)
            Die();
    } 

    public void Die()   //poziva se kada ai umre (hp <= 0)
    {
        alive = false;
        rig.constraints = RigidbodyConstraints.FreezeAll;
        playAnim("death");
        BoxCollider[] bcs = GetComponents<BoxCollider>();
        if (bcs.Length > 1) //deathScaleFactor radi samo na box collider-ima
        {
            bcs[0].size = new Vector3(bcs[0].size.x * deathScaleFactor.x, bcs[0].size.y * deathScaleFactor.y, bcs[0].size.z * deathScaleFactor.z);
            bcs[0].center = bcs[0].center - absVec3(Vector3.one - deathScaleFactor);
        }
        if(bcs.Length > 2)
        {
            bcs[2].enabled = false; //isključi gornji trigger
        }
        rig.isKinematic = true;     //za poništavanje fizike
    }

    Vector3 absVec3(Vector3 v)  //abs svakog dijela vektora
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

}
