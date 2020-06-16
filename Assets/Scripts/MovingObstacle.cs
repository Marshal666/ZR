using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Komponenta za prepreku koja se pomice
/// po pomacima može biti linearna ili uglađena (koristeći linearnu interpolaciju)
/// </summary>

[RequireComponent(typeof(Rigidbody))]   //za rad skripta treba rigidbody komponentu na objektu
public class MovingObstacle : MonoBehaviour {

	[System.Serializable]   //točka i vrijeme koje treba da se dođe do nje (za linearne prepreke)
    public class Point
    {
        public float time;  //vrijeme prijelaza do točke (za uglađeni tip to je samo brzina prijelaza)
        public Vector3 point;   //točka

        public Point(float t, Vector3 pos)
        {
            time = t;
            point = pos;
        }

        public static implicit operator Vector3(Point p)    //za baratenje točkom kao vektorom u kodu radi jednotavnosti
        {
            return p.point;
        }

    }

    public enum ObstacleType { Linear, Smoothed };  //mogući tipovi prepreke, linearni pomaci i "uglađeni"

    public ObstacleType type = ObstacleType.Linear; //tip prepreke

    public Point[] points;  //točke po kojima se prepreka miče

    public int currentPoint = 0;    //index trenutne točke na koju ide prepreka

    Vector3 relativePos,    //pozicija objekta u početku levela
            basePos;        //pozicija točke s koje objekt ide na sljedeću

    Rigidbody rig;          //Rigidbody komponenta objekta

    void Start()    //Inicijalizacija...
    {
        rig = GetComponent<Rigidbody>();
        relativePos = rig.position;
        basePos = rig.position;
    }

    void FixedUpdate()  //Fizički frame igre
    {
        if (Scene.currentGameState == Scene.GameState.playing)  //ako igrač igra igru
        {
            if (points.Length != 0) //ako objekt ima point-ove
            {
                //radi pomak s obzirom na tip prepreke
                if (type == ObstacleType.Linear)    //za linearnu prepreku
                {
                    Vector3 fromPos = (-(rig.position - (relativePos + points[currentPoint].point)));   //smjer od kuda objekt kreće
                    rig.velocity = (fromPos.normalized * (Vector3.Distance(basePos, points[currentPoint] + relativePos)) / points[currentPoint].time);  //postavi brzinu tako da objekt stigne na sljedeći point na određeno vrijeme
                    if (Vector3.Distance(rig.position, points[currentPoint] + relativePos) <= (rig.velocity.magnitude * Time.fixedDeltaTime) || Vector3.Distance(rig.position, points[currentPoint] + relativePos) <= 0.001f)   //ako je objekt stigao na točku
                    {
                        basePos = points[currentPoint] + relativePos;       //točka s koje se kreće postaje trenutna točka
                        currentPoint = (currentPoint + 1) % points.Length;  //povećaj currentPoint
                        rig.velocity = Vector3.zero;                        //zaustavi objekt
                    }
                }
                else    //za smoothed tip prepreke
                {
                    rig.velocity = (-(rig.position - (points[currentPoint] + relativePos)) / points[currentPoint].time);    //brzina je proporcinalna putu do sljedećeg point-a, a ornuto proporcinalna cremenu da se stigne do point-a
                    if (Vector3.Distance(rig.position, points[currentPoint] + relativePos) < 0.001f)    //ako je objekt stigao na točku
                    {
                        basePos = points[currentPoint] + relativePos;       //točka s koje se kreće postaje trenutna točka
                        currentPoint = (currentPoint + 1) % points.Length;  //povećaj currentPoint
                        rig.velocity = Vector3.zero;                        //zaustavi objekt
                    }
                }
            }
        }
    }

}
