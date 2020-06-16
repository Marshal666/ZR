using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// moponenta koji svaki objekt kojim se može upravljati u level editoru mora imati
/// </summary>

public class EditorObject : MonoBehaviour {

    public Material objMat; //oroginalni materijal objekta

    public string src;  //izvor objekta (relativni path do njegove xml datoteke)

}
