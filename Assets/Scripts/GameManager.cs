using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    //I NEED A WAY TO TRIGGER AUTOMATIC DRAWS

    //FIX SHADOW TEXT COLOR HARD TO SEE

    //HIGHLIGHT AVAILABLE ACTIONS FOR ALLY AND ENEMY, INCLUDING PASS BUTTON

    //REMOVE SPELLGRAYS IN SCENE IF I DECIDE TO KEEP THEM ALL VISIBLE

    //SHORTCUT MENU, DEFAULTS HAVE SPACE AS PASS SUBMIT AND CONSOLE BUTTON, X AS CANCEL, ENTER AS CHAT

    //DON'T LOG CONSOLE MESSAGES IN CHAT


    //.temp code for faster connection
    bool alreadyConnected;
    private void Update()
    {
        if (alreadyConnected)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NetworkManager.Singleton.StartHost();
            alreadyConnected = true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NetworkManager.Singleton.StartClient();
            alreadyConnected = true;
        }
    }
}