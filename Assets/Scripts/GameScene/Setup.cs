using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Setup : NetworkBehaviour
{
    // STATIC:
    public delegate void GuestSwitchAction();
    public static event GuestSwitchAction GuestSwitch;

    // SCENE REFERENCE:
    [SerializeField] private List<Elemental> hostSceneElementals = new();
    [SerializeField] private List<Spell> hostSceneSpells = new();

    [SerializeField] private List<Elemental> guestSceneElementals = new();
    [SerializeField] private List<Spell> guestSceneSpells = new();

    [SerializeField] private Teambuilder teambuilder;
    [SerializeField] private ExecutionCore executionCore;
    
    [SerializeField] private TMP_Text allyUsernameText;
    [SerializeField] private TMP_Text enemyUsernameText;

    public override void OnNetworkSpawn()
    {
        if (IsServer) return;

        // Get guest's team (string lists/arrays aren't serializable, so they're placed in serializable 'container' classes)
        string[] elementals = teambuilder.teamElementalNames.ToArray();
        StringContainer[] elementalStringContainers = StringContainerConverter.ContainStrings(elementals);
        string[] spells = teambuilder.teamSpellNames.ToArray();
        StringContainer[] spellStringContainers = StringContainerConverter.ContainStrings(spells);

        GuestConnectedRpc(PlayerPrefs.GetString("Username"), elementalStringContainers, spellStringContainers);
    }

    [Rpc(SendTo.Server)]
    public void GuestConnectedRpc(string guestUsername, StringContainer[] guestElementalNames, StringContainer[] guestSpellNames)
    {
        // Get host's team (string lists/arrays aren't serializable, so they're placed in serializable 'container' classes)
        string[] hostElementalNames = teambuilder.teamElementalNames.ToArray();
        StringContainer[] elementalStringContainers = StringContainerConverter.ContainStrings(hostElementalNames);
        string[] hostSpellNames = teambuilder.teamSpellNames.ToArray();
        StringContainer[] spellStringContainers = StringContainerConverter.ContainStrings(hostSpellNames);

        SetUpTeamsRpc(PlayerPrefs.GetString("Username"), guestUsername, elementalStringContainers, spellStringContainers, guestElementalNames, guestSpellNames);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetUpTeamsRpc(string hostUsername, string guestUsername, 
        StringContainer[] hostElementalNames, StringContainer[] hostSpellNames, StringContainer[] guestElementalNames, StringContainer[] guestSpellNames)
    {
        List<Elemental> allyElementals = IsHost ? hostSceneElementals : guestSceneElementals;
        foreach (Elemental elemental in allyElementals)
            elemental.isAlly = true;

        if (!IsHost)
            GuestSwitch?.Invoke();

        allyUsernameText.text = IsHost ? hostUsername : guestUsername;
        enemyUsernameText.text = IsHost ? guestUsername : hostUsername;

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

        executionCore.RoundStart();
    }
}