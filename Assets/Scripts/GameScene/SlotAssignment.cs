using System.Collections.Generic;
using UnityEngine;

public class SlotAssignment : MonoBehaviour
{
    // STATIC:
    public List<Elemental> Elementals { get; private set; }

    // SCENE REFERENCE:
    [SerializeField] private List<Elemental> assignedElementals;

    // CONSTANT:
    private readonly List<Vector2> boardPositions = new();

    private void Awake()
    {
        Elementals = assignedElementals;

        foreach (Elemental elemental in Elementals)
            boardPositions.Add(elemental.transform.position);
    }

    public int GetSlot(Elemental elemental)
    {
        for (int i = 0; i < Elementals.Count; i++)
        {
            if (Elementals[i] == elemental)
                return i;
        }

        Debug.LogError("Elemental not found in slotAssignment");
        return default;
    }

    public Dictionary<string, int> GetSlotDesignations(int selfSlot)
    {
        List<int> slotsToDesignate = new();

        if (selfSlot == 0) slotsToDesignate = new() { 1, 2, 3, 4, 5 };
        else if (selfSlot == 1) slotsToDesignate = new() { 0, 2, 3, 4, 5 };
        else if (selfSlot == 2) slotsToDesignate = new() { 3, 0, 1, 6, 7 };
        else if (selfSlot == 3) slotsToDesignate = new() { 2, 0, 1, 6, 7 };

        return new Dictionary<string, int>()
        {
            { "allySlot", slotsToDesignate[0] },
            { "enemy1Slot", slotsToDesignate[1] },
            { "enemy2Slot", slotsToDesignate[2] },
            { "benchedAlly1Slot", slotsToDesignate[3] },
            { "benchedAlly2Slot", slotsToDesignate[4] },
        };
    }

    public Elemental GetAlly(Elemental elemental)
    {
        int elementalSlot = GetSlot(elemental);
        int allySlot = GetSlotDesignations(elementalSlot)["allySlot"];
        return Elementals[allySlot];
    }

    public bool CheckTargetAvailable(int slot)
    {
        Elemental target = Elementals[slot];

        // Slot does not contain an Elemental
        if (target == null)
            return false;

        // Slot contains an Elemental that is Disengaged
        if (target.DisengageStrength > 0)
            return false;

        return true;
    }

    public List<Elemental> GetAllAvailableTargets(Elemental caster, bool includeBenchedTargets)
    {
        List<Elemental> availableTargets = new();

        int slotsToCheck = includeBenchedTargets ? 8 : 4;
        for (int i = 0; i < slotsToCheck; i++)
        {
            if (!CheckTargetAvailable(i))
                continue;

            if (i == GetSlot(caster))
                continue;

            availableTargets.Add(Elementals[i]);
        }

        return availableTargets;
    }

    public void Swap(Elemental inPlayElemental, Elemental benchedElemental)
    {
        int inPlaySlot = GetSlot(inPlayElemental);
        int benchedSlot = GetSlot(benchedElemental);

        // Swap board position
        (benchedElemental.transform.position, inPlayElemental.transform.position) = 
            (inPlayElemental.transform.position, benchedElemental.transform.position);

        // Hide Spells/Items/Statuses
        inPlayElemental.ToggleBenched(true);
        benchedElemental.ToggleBenched(false);

        // Update SlotAssignment.Elementals
        (Elementals[inPlaySlot], Elementals[benchedSlot]) = (Elementals[benchedSlot], Elementals[inPlaySlot]);

        // Swap actions
        (Elementals[inPlaySlot].currentActions, Elementals[benchedSlot].currentActions) = 
            (Elementals[benchedSlot].currentActions, Elementals[inPlaySlot].currentActions);
    }

    public void Repopulate(int inPlaySlot, int benchedSlot)
    {
        Elemental benchedElemental = Elementals[benchedSlot];

        // Swap board position
        benchedElemental.transform.position = boardPositions[inPlaySlot];

        // Hide Spells/Items/Statuses
        benchedElemental.ToggleBenched(false);

        // Update SlotAssignment.Elementals
        Elementals[inPlaySlot] = benchedElemental;
        Elementals[benchedSlot] = null;
    }
}