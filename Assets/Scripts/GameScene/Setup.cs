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

    [SerializeField] private List<NetworkObject> guestElementalNetworkObjects = new();

    [SerializeField] private Teambuilder teambuilder;

    public delegate void GuestFlipAction();
    public static event GuestFlipAction GuestFlip;

    public override void OnNetworkSpawn()
    {
        if (IsServer) return;

        //get guest's team (string lists/arrays aren't serializable, so they're placed in serializable 'container' classes)
        string[] elementals = teambuilder.teamElementalNames.ToArray();
        StringContainer[] elementalStringContainers = StringContainerConverter.ContainStrings(elementals);
        string[] spells = teambuilder.teamSpellNames.ToArray();
        StringContainer[] spellStringContainers = StringContainerConverter.ContainStrings(spells);

        GuestConnectedServerRpc(elementalStringContainers, spellStringContainers);
    }

    [ServerRpc (RequireOwnership = false)]
    public void GuestConnectedServerRpc(StringContainer[] guestElementalNames, StringContainer[] guestSpellNames, ServerRpcParams serverRpcParams = default)
    {
        //make guest the owner of their Elementals
        foreach (NetworkObject networkObject in guestElementalNetworkObjects)
            networkObject.ChangeOwnership(serverRpcParams.Receive.SenderClientId);

        //get host's team (string lists/arrays aren't serializable, so they're placed in serializable 'container' classes)
        string[] hostElementalNames = teambuilder.teamElementalNames.ToArray();
        StringContainer[] elementalStringContainers = StringContainerConverter.ContainStrings(hostElementalNames);
        string[] hostSpellNames = teambuilder.teamSpellNames.ToArray();
        StringContainer[] spellStringContainers = StringContainerConverter.ContainStrings(hostSpellNames);

        SetUpTeamsClientRpc(elementalStringContainers, spellStringContainers, guestElementalNames, guestSpellNames);
    }

    [ClientRpc]
    public void SetUpTeamsClientRpc(StringContainer[] hostElementalNames, StringContainer[] hostSpellNames, StringContainer[] guestElementalNames, StringContainer[] guestSpellNames)
    {
        //flip before Elemental.Setup so Elemental can set status correctly
        if (!IsServer)
            GuestFlip?.Invoke();

        for (int i = 0; i < 4; i++)
        {
            hostSceneElementals[i].Setup(hostElementalNames[i].containedString);
            guestSceneElementals[i].Setup(guestElementalNames[i].containedString);
        }

        for (int i = 0; i < 12; i++)
        {
            hostSceneSpells[i].Setup(hostSpellNames[i].containedString);
            guestSceneSpells[i].Setup(guestSpellNames[i].containedString);
        }
    }
}