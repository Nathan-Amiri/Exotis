using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Setup : NetworkBehaviour
{
    //temporary class for testing

    [SerializeField] private List<Elemental> hostSceneElementals = new();
    [SerializeField] private List<Spell> hostSceneSpells = new();

    [SerializeField] private List<Elemental> guestSceneElementals = new();
    [SerializeField] private List<Spell> guestSceneSpells = new();

    [SerializeField] private List<Elemental> sceneElementals = new();
    [SerializeField] private List<Spell> sceneSpells = new();

    [SerializeField] private List<NetworkObject> guestElementalNetworkObjects = new();

    [SerializeField] private Teambuilder teambuilder;

    public delegate void GuestFlipAction();
    public static event GuestFlipAction GuestFlip;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            GuestConnectedServerRpc();
    }

    [ServerRpc (RequireOwnership = false)]
    public void GuestConnectedServerRpc(ServerRpcParams serverRpcParams = default)
    {
        //make guest the owner of their Elementals
        foreach (NetworkObject networkObject in guestElementalNetworkObjects)
            networkObject.ChangeOwnership(serverRpcParams.Receive.SenderClientId);        

        SetUpTeamsClientRpc();
    }

    [ClientRpc]
    public void SetUpTeamsClientRpc()
    {
        //flip before setting up so Elemental can set status correctly
        if (!IsServer)
            GuestFlip?.Invoke();

        //List<Elemental> sceneElementals;
        //List<Spell> sceneSpells;

        //if (IsServer)
        //{
        //    sceneElementals = hostSceneElementals;
        //    sceneSpells = hostSceneSpells;
        //}
        //else
        //{
        //    sceneElementals = guestSceneElementals;
        //    sceneSpells = guestSceneSpells;
        //}

        //for (int i = 0; i < 4; i++)
        //{
        //    hostSceneElementals[i].Setup(hostElementalNames[i]);
        //    guestSceneElementals[i].Setup(guestElementalNames[i]);
        //}

        //for (int i = 0; i < 12; i++)
        //{
        //    hostSceneSpells[i].Setup(hostSpellNames[i]);
        //    guestSceneSpells[i].Setup(guestSpellNames[i]);
        //}
    }
}