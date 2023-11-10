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

    [SerializeField] private Teambuilder teambuilder;

    public delegate void GuestFlipAction();
    public static event GuestFlipAction GuestFlip;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            BothConnectedServerRpc();
    }

    [ServerRpc (RequireOwnership = false)]
    public void BothConnectedServerRpc()
    {
        BothConnectedClientRpc();
    }

    [ClientRpc]
    public void BothConnectedClientRpc()
    {
        if (!IsServer)
            GuestFlip?.Invoke();

        List<Elemental> sceneElementals;
        List<Spell> sceneSpells;

        if (IsServer)
        {
            sceneElementals = hostSceneElementals;
            sceneSpells = hostSceneSpells;
        }
        else
        {
            sceneElementals = guestSceneElementals;
            sceneSpells = guestSceneSpells;
        }

        for (int i = 0; i < teambuilder.teamElementalNames.Count; i++)
            sceneElementals[i].Setup(teambuilder.teamElementalNames[i]);

        for (int i = 0; i < teambuilder.teamSpellNames.Count; i++)
            sceneSpells[i].Setup(teambuilder.teamSpellNames[i]);
    }
}