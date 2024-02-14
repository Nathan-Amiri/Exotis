using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Setup : NetworkBehaviour
{
    // STATIC:
    public delegate void GuestFlipAction();
    public static event GuestFlipAction GuestFlip;

    public delegate void GuestSwitchAction();
    public static event GuestSwitchAction GuestSwitch;

    // SCENE REFERENCE:
    [SerializeField] private List<Elemental> hostSceneElementals = new();
    [SerializeField] private List<Spell> hostSceneSpells = new();

    [SerializeField] private List<Elemental> guestSceneElementals = new();
    [SerializeField] private List<Spell> guestSceneSpells = new();

    [SerializeField] private Teambuilder teambuilder;
    [SerializeField] private DelegationCore delegationCore;

    public override void OnNetworkSpawn()
    {
        if (IsServer) return;

        // Get guest's team (string lists/arrays aren't serializable, so they're placed in serializable 'container' classes)
        string[] elementals = teambuilder.teamElementalNames.ToArray();
        StringContainer[] elementalStringContainers = StringContainerConverter.ContainStrings(elementals);
        string[] spells = teambuilder.teamSpellNames.ToArray();
        StringContainer[] spellStringContainers = StringContainerConverter.ContainStrings(spells);

        GuestConnectedRpc(elementalStringContainers, spellStringContainers);
    }

    [Rpc(SendTo.Server)]
    public void GuestConnectedRpc(StringContainer[] guestElementalNames, StringContainer[] guestSpellNames)
    {
        // Get host's team (string lists/arrays aren't serializable, so they're placed in serializable 'container' classes)
        string[] hostElementalNames = teambuilder.teamElementalNames.ToArray();
        StringContainer[] elementalStringContainers = StringContainerConverter.ContainStrings(hostElementalNames);
        string[] hostSpellNames = teambuilder.teamSpellNames.ToArray();
        StringContainer[] spellStringContainers = StringContainerConverter.ContainStrings(hostSpellNames);

        SetUpTeamsRpc(elementalStringContainers, spellStringContainers, guestElementalNames, guestSpellNames);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetUpTeamsRpc(StringContainer[] hostElementalNames, StringContainer[] hostSpellNames, StringContainer[] guestElementalNames, StringContainer[] guestSpellNames)
    {
        List<Elemental> allyElementals = IsHost ? hostSceneElementals : guestSceneElementals;
        foreach (Elemental elemental in allyElementals)
            elemental.isAlly = true;

        // Flip before Elemental.Setup so Elemental can set status correctly
        // Switch benched Elementals after Flipping to prevent execution order bugs
        if (!IsHost)
        {
            GuestFlip?.Invoke();
            GuestSwitch?.Invoke();
        }

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

        delegationCore.RequestDelegation(DelegationCore.DelegationScenario.TimeScale);
    }
}