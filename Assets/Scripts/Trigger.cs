using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// komponenta za nevidljive objekte kroz koje se prolazi,
/// no sudaranja s njima se obrađuju
/// </summary>

[ExecuteInEditMode]                         //korišteno prilikom testiranja
public class Trigger : MonoBehaviour
{

    public enum EventAction     //što sudar s igračem donosi
    {
        None,
        PlayerWin,
        PlayerLose,
        PlayerGainHp
    }

    public EventAction OnPlayerEnter;   //što se dogodi dok se igrač sudari s trigger-om

    public EventAction OnPlayerStay;    //što se događa dok je igrač u sudaru s trigger-om

    public EventAction OnPlayerExit;    //što se dogodi dok igrač izađe iz trigger-a

    public object arg1, arg2, arg3;     //argumenti za događaje

    void ProcessActions(EventAction ea, object arg) //procesiranje događaja za njegov tip
    {
        switch (ea)
        {
            case EventAction.None:
                break;
            case EventAction.PlayerGainHp:
                Scene.player.GainHp((float)arg);
                break;
            case EventAction.PlayerWin:
                Scene.player.Win();
                break;
            case EventAction.PlayerLose:
                Scene.player.Die();
                break;
            default:
                break;
        }
    }

    void OnTriggerEnter(Collider col)   //poziva se dok se nešto počinje sudarati sa trigger-om
    {
        if (Scene.currentGameState == Scene.GameState.playing)
        {
            if (col.tag == "Player")    //ako je objekt koji se sudario igrač, procesiraj događaje za ovaj trigger
                ProcessActions(OnPlayerEnter, arg1);
        }
    }

    void OnTriggerStay(Collider col)    //poziva se dok je nešto u sudaru sa trigger-om
    {
        if (Scene.currentGameState == Scene.GameState.playing)
        {
            if (col.tag == "Player")    //ako je objekt koji se sudara igrač, procesiraj događaje za ovaj trigger
                ProcessActions(OnPlayerStay, arg2);
        }
    }

    void OnTriggerExit(Collider col)    //poziva se dok nešto izađe iz sudara sa trigger-om
    {
        if (Scene.currentGameState == Scene.GameState.playing)
        {
            if (col.tag == "Player")    //ako je objekt koji je izašao iz sudara igrač, procesiraj događaje za ovaj trigger
                ProcessActions(OnPlayerExit, arg3);
        }
    }

}
