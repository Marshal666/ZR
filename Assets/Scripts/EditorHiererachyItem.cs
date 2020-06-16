using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Objekt koji predstavlja objekt u hijerarhiji te njegovo upravljanje u hijerarhiji
/// Može se selektirati, postaviti i ukloniti kao child object drag n drop tehnikom
/// </summary>

public class EditorHiererachyItem : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler {


    public GameObject refObj; 
    /// <summary>
    /// referenca na pripadni objekt
    /// </summary>


    //MeshRenderer mr;    

    bool sel = false;   //dali je objekt selektiran?
    public bool selected { get { return sel; } set { sel = value; txt.color = sel ? GameData.main.editorSelectedColor : GameData.main.editorDeselectedColor; } }    //promjena boje prilikom postavljanja

    public bool selecable = true, draggable = true;     //kako se ovim item-om može upravljati

    Text txt;   //tekst komponenta item-a

    void Awake()
    {
        txt = GetComponent<Text>(); //inicijaliziraj tekst
    }

    public void OnDrop(PointerEventData eventData)  //metoda se poziva kada korisnik dropa neki drugi item na ovaj
    {
        if (draggable || refObj == Scene.rootObject)    //ako se item može draggati ili je item za root object, postavi sve selektirane objekte umjesto player-a i kamere kao djecu ref Objekta trenutnog item-a
        {
            if (GameEditor.main.selectedObjects.Count > 0)
            {
                for (int i = 0; i < GameEditor.main.selectedObjects.Count; i++)
                {
                    if (!GameEditor.main.selectedObjects[i].GetComponent<Player>() && GameEditor.main.selectedObjects[i].tag != "EditorCam")
                        GameEditor.main.selectedObjects[i].transform.parent = refObj.transform; 
                }
                GameEditor.main.UpdateHierarchy();  //update hijerarhije nakon promjena
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)  //metoda se poziva kada korisnik klikne na trenutni item, ako je item selektibilan onda selektiraj/deselektiraj item i njegov objekt
    {
        if (selecable)
        {
            selected = !selected;
            if (selected)
            {
                GameEditor.main.selectObject(refObj);
            }
            else
            {
                GameEditor.main.deselectObject(refObj);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData) //metoda se poziva kada korisnik počinje povlačiti ovaj item, ako je item "draggabilan" onda ga selektiraj
    {
        if (draggable)
        {
            if (!selected)
            {
                selected = true;
                GameEditor.main.selectObject(refObj);
            }
        }
    }
}
