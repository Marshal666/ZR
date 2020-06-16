using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSStats : MonoBehaviour {

    float delta = 0f;   //promjena vremena od slijedećeg frame-a

    UnityEngine.UI.Text txt;    //tekst na koji se ispisuje
    
	//inicijalizacija
	void Start () {
        txt = GetComponent<UnityEngine.UI.Text>();
        txt.text = "";
        StartCoroutine(UpdateTxt());
	}
	
	//svakog frame-a računaj promjenu vremena
	void Update () {
        delta += (Time.deltaTime - delta) * 0.1f;
	}

    IEnumerator UpdateTxt() //Update-aj text dvaput u sekundi
    {
        while(true)
        {
            if (delta != 0f)
            {
                txt.text = "FPS: " + (1f / delta).ToString("00.0");
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
