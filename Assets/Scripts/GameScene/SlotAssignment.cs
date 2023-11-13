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
}