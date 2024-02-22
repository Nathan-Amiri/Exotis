using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    //I NEED A WAY TO TRIGGER AUTOMATIC DRAWS

    //FIX SHADOW TEXT COLOR HARD TO SEE

    //HIGHLIGHT AVAILABLE ACTIONS


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