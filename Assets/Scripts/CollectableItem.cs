using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// komponenta za objekte koje igrač može pokupiti
/// kada se pokupe, oni nestanu, svaki se može pokupiti jednnom
/// pod pojmom nestanu, oni se samo isključe te ostaju u sceni jer pozivanje metode Destroy() barata memorijom što nekad može loše utjecati na performanse igre
/// </summary>

public class CollectableItem : MonoBehaviour {

	public enum CollectableItemGain
    {
        MaxHpIncrease,  //povećava hp_max player-a
        HealPlayer,     //povećava hp player-a, uporabom ovg tima moguće je i napraviti objet koji smanjuje hp player-a, not to nije moralno
        BoostSpeed,     //povećava brzinu player-a na neko vrijeme
        None            //ništa
    }

    public CollectableItemGain action;  //koji tip poboljšanja daje ovaj item?

    public object arg1, arg2;   //argumenti poboljšanja

    public GameObject dissapearEffect;  //objekt koji se pojavi nakon što se pokubi ovaj item (neki particle effect)

    Rigidbody rig;  //rigidbody komponenta ovog objekta

    public static Vector3 angVel = new Vector3(0, 2.00712864f, 0);  //brzina rotacije svakog collectable item-a

    void Start()    //inicijalizacija
    {
        rig = GetComponent<Rigidbody>();
    }

    void FixedUpdate()  //fizički frame
    {
        if (Scene.currentGameState == Scene.GameState.playing)  //postavi brzinu item-a
            rig.angularVelocity = angVel;
    }

    void OnTriggerStay(Collider col)    //poziva se kada se nekim objet sudari sa ovim item-om
    {
        if(col.tag == "Player" && Scene.currentGameState == Scene.GameState.playing)    //ako se sudario igrač prilikom igranja
        {
            switch (action)     //ovisno o tipu, poboljšaj/pogoršaj player-a
            {
                case CollectableItemGain.MaxHpIncrease:
                    Scene.player.hp_max += (float)arg1;
                    break;
                case CollectableItemGain.HealPlayer:
                    Scene.player.GainHp((float)arg1);
                    break;
                case CollectableItemGain.BoostSpeed:
                    Scene.player.StartCoroutine(Scene.player.GainSpeed((float)arg1,(float)arg2));
                    break;
                default:
                    break;
            }
            dissapearEffect.SetActive(true);    //ukluči efekt nestanka
            gameObject.SetActive(false);    //iskluči ovaj objekt
        }
    }

}
