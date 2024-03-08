using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetManager : MonoBehaviour
{
    public void DisplayTargets(List<int> casterSlots, List<int> targetSlots, bool interactable)
    {
        // Reset targets and dims
        ResetAllTargets();

        foreach (int targetSlot in targetSlots)
        {
            Button targetButton = SlotAssignment.Elementals[targetSlot].targetButton;
            targetButton.gameObject.SetActive(true);
            targetButton.interactable = interactable;
        }

        // Dim all non-casters
        for (int i = 0; i < SlotAssignment.Elementals.Count; i++)
            if (!casterSlots.Contains(i))
                SlotAssignment.Elementals[i].icon.color = Color.black;
    }

    public void ResetCertainTargets(List<int> targetSlots)
    {
        // Used to turn off certain buttons while leaving others active

        foreach (int targetSlot in targetSlots)
        {
            Button targetButton = SlotAssignment.Elementals[targetSlot].targetButton;
            targetButton.gameObject.SetActive(false);
            targetButton.interactable = false;
        }
    }

    public void ResetAllTargets()
    {
        foreach (Elemental elemental in SlotAssignment.Elementals)
        {
            elemental.targetButton.gameObject.SetActive(false);
            elemental.targetButton.interactable = false;

            elemental.icon.color = Color.white;
        }
    }

    public bool AnyTargetsAvailable()
    {
        foreach (Elemental elemental in SlotAssignment.Elementals)
            if (elemental.targetButton.gameObject.activeSelf)
                return true;

        return false;
    }
}