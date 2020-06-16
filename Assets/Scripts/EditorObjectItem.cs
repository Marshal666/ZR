using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Komponenta za UI objekte u game editoru koji reprezentiraju objekte za level,
/// omogućuju dodavanje objekata u scenu drag n drop tehnikom
/// </summary>

public class EditorObjectItem : MonoBehaviour, UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler
{

    public GameObject obj;  //objekt na koji se ovaj item referira

    public GameObject copy = null;  //kopija tog objekta (koji se dodaje u scenu pri početku drag-a, na drop-u se uključuje)

    public void OnDrag(PointerEventData eventData)  //kada se item povlači
    {
        bool mouseIsInGameWindow = (Input.mousePosition.x / Screen.width >= 0.25f && Input.mousePosition.x / Screen.width <= 0.75f) && (Input.mousePosition.y / Screen.height >= 0.33f) && !GameEditor.editingSettings; //provjera dali je kursor unutar scene preview-a
        GameEditor.main.deselectAll();  //deselektiraj sve objekte u editoru
        Collider[] co;                  //svi sudarači kopije referiranog objekta
        if (copy == null)               //ako kopija ne postoji, stvori novu, pri tome se pobrini da dobije ime koje se već koristi
        {
            copy = Instantiate(obj);
            if (!GameEditor.main.names.Contains(obj.name))
            {
                copy.name = obj.name;
                GameEditor.main.names.Add(obj.name);
            } else
            {
                copy.name = GameEditor.main.findAnotherName(obj.name);
                GameEditor.main.names.Add(copy.name);
            }
            copy.SetActive(false);
        }
        co = copy.GetComponents<Collider>();    //dodjeli refrece collider-a u co
        if (mouseIsInGameWindow)                //ako je kursor u scene preview-u
        {
            GameEditor.main.X.gameObject.SetActive(false);      //isključi objet X
            copy.SetActive(true);                               //aktiviraj kopiju
            RaycastHit hit;                                     //zraka koja se šalje od kamere u scenu te ako pogodi nešto, pozicija kopije je malo pomaknuta od te površine
            if (co.Length > 0)                                  //iskluči sve collider-e kopije
                for (int i = 0; i < co.Length; i++)
                    co[i].enabled = false;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 250f))
            {
                copy.transform.position = hit.point + hit.normal / 2f;
            }
            else
            {
                copy.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 15f));
            }
            if (co.Length > 0)                                  //ponovo uključi sve collider-e kopije
                for (int i = 0; i < co.Length; i++)
                    co[i].enabled = true;
        }
        else                                    //ako kursor nije u scene preview-u
        {
            GameEditor.main.X.gameObject.SetActive(true);   //uključi objekt X i postavi ga na poziciju kursora, kopiju isključi (ako već nije isključena)
            GameEditor.main.X.anchoredPosition = Input.mousePosition - (new Vector3(Screen.width / 2f, Screen.height / 2f));
            copy.SetActive(false);
        }
    }

    public void OnEndDrag(PointerEventData eventData)   //kada se item prestane dragg-ati
    {
        bool mouseIsInGameWindow = (Input.mousePosition.x / Screen.width >= 0.25f && Input.mousePosition.x / Screen.width <= 0.75f) && (Input.mousePosition.y / Screen.height >= 0.33f); //provjera dali je kursor unutar scene preview-a
        GameEditor.main.X.gameObject.SetActive(false);  //isključi X (ako već nije bio uključen)
        if (mouseIsInGameWindow)    //ako je kursor u scene preview-u
        {
            copy.transform.parent = Scene.rootObject.transform; //postavi root kao parent novog objekta u sceni kopije)
            var co = copy.GetComponent<Collider>();             //uključi collider kopije
            if (co)
                co.enabled = true;
            copy = null;                                        //poništi refrencu na kopiju
            GameEditor.main.UpdateHierarchy();                  //dodaj objekt u hijerarhiju
        }
        else        //ako kursore nije u scene preview-u, samo isključi kopiju
        {
            copy.SetActive(false);
        }   
    }
}
