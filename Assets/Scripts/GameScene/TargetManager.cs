using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetManager : MonoBehaviour
{
    [SerializeField] private List<Image> elementalIcons = new();
    [SerializeField] private List<Button> elementalTargetButtons = new();

    public void DisplayTargets(List<int> casterSlots, List<int> targetSlots, bool interactable)
    {
        // Reset targets and dims
        ResetAllTargets();

        foreach (int targetSlot in targetSlots)
        {
            elementalTargetButtons[targetSlot].gameObject.SetActive(true);
            elementalTargetButtons[targetSlot].interactable = interactable;
        }

        // Dim all non-casters
        for (int i = 0; i < elementalIcons.Count; i++)
            if (!casterSlots.Contains(i))
                elementalIcons[i].color = Color.black; //new Color32(130, 130, 130, 255);
    }

    public void ResetCertainTargets(List<int> targetSlots)
    {
        // Used to turn off certain buttons while leaving others active

        foreach (int targetSlot in targetSlots)
        {
            elementalTargetButtons[targetSlot].gameObject.SetActive(false);
            elementalTargetButtons[targetSlot].interactable = false;
        }
    }

    public void ResetAllTargets()
    {
        foreach (Button targetButton in elementalTargetButtons)
        {
            targetButton.gameObject.SetActive(false);
            targetButton.interactable = false;
        }

        foreach (Image elementalIcon in elementalIcons)
            elementalIcon.color = Color.white;
    }

    public bool AnyTargetsAvailable()
    {
        foreach (Button targetButton in elementalTargetButtons)
            if (targetButton.gameObject.activeSelf)
                return true;

        return false;
    }
}