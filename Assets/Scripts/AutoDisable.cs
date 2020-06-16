using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skript automatski isključije objekt kojemu pripada nakon nekog vremena
/// </summary>

public class AutoDisable : MonoBehaviour {

    public float time;  //tijeme u sekundama prije isključenja

	void OnEnable() {   //kad se skripta uključi, polreni korutinu koja ga isključuje
        StartCoroutine(DisableAfterTime());
        ParticleSystem ps = GetComponent<ParticleSystem>(); //ako je objekt particle system, pokreni ga
        if(ps)
        {
            ps.Play(true);
        }
	}

    IEnumerator DisableAfterTime()
    {
        yield return new WaitForSeconds(time);  //pričekaj određeno vrijeme
        gameObject.SetActive(false);            //isključi objekt
    }
	
}
