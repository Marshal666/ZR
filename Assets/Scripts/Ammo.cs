using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Radi po principu AutoDisable.cs skripte samo što zahtjeva rigidbody komponentu i korutina se poziva od drugog objekta (AI objekta)
/// </summary>

[RequireComponent(typeof(Rigidbody))]
public class Ammo : MonoBehaviour {

    public float lifeTime;  //vrijeme života metka, nakon tog vremena ovaj objekt se isključuje

    public GameObject onCollisionParticles; //objekt koji se stvara tokom sudara ovog metka

    void Start()
    {
        onCollisionParticles = Instantiate(onCollisionParticles);   //stvori kopiji čestica za sudaranje
        onCollisionParticles.transform.parent = Scene.rootObject.transform; //postavi root kao parent tog efekta sudaranja
        //print("e");
    }

	IEnumerator end()   //korutina koja se izvodi tokom izvođenja ovog objekta
    {
        if (onCollisionParticles)   //ako postoji efekt sudara, isključi ga
        {
            onCollisionParticles.SetActive(false);
            
        }
        yield return new WaitForSeconds(lifeTime);  //čekaj da vrijeme života metka prijeđe
        ParticleSystem ps = GetComponent<ParticleSystem>(); //ako ovaj objekt ima na sebi ParticleSystem komponentu, iskluči je, isto vrijedi i za djecu ovog objekta
        if (ps)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        for (int i = 0; i < transform.childCount; i++)
        {
            ps = transform.GetChild(i).GetComponent<ParticleSystem>();
            if (ps)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider col)   //ako se objekt sudari s nečime
    {
        if ((col.gameObject.layer & Scene.player.groundLayers.value) == 1 && !col.gameObject.GetComponent<AI>())    //ako se sudari s nekim preprekama
        {
            StopAllCoroutines();    //ammo prestaje postojati
            ParticleSystem ps = GetComponent<ParticleSystem>();     //zaustavi sve efekte ovog objekta i njegove djece
            if (ps)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            for(int i = 0;i < transform.childCount; i++)
            {
                ps = transform.GetChild(i).GetComponent<ParticleSystem>();
                if (ps)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            gameObject.SetActive(false);    //isključi objekt
        }
    }
    
}
