using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotAssignment : MonoBehaviour
{
    public static List<Elemental> Elementals {  get; private set; }

    [SerializeField] private List<Elemental> assignedElementals; //assigned in scene

    private void Awake()
    {
        Elementals = assignedElementals;
    }

    public static int GetSlot(Elemental elemental)
    {
        for (int i = 0; i < Elementals.Count; i++)
        {
            if (Elementals[i] == elemental)
                return i;
        }

        Debug.LogError("Elemental not found in slotAssignment");
        return default;
    }

    public static Dictionary<string, int> GetSlotDesignations(int selfSlot)
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
            { "enemy2Slot", slotsToDesignate[1] },
            { "benchedAlly1Slot", slotsToDesignate[1] },
            { "benchedAlly2Slot", slotsToDesignate[1] },
        };
    }
}