using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellTraitEffect : MonoBehaviour
{
    private delegate void EffectMethod(DelayedInfo delayedInfo);

    private readonly Dictionary<string, EffectMethod> effectMethodIndex = new();

    private void Awake()
    {
        PopulateIndex();
    }

    public void CallEffectMethod(DelayedInfo info)
    {
        if (!effectMethodIndex.ContainsKey(info.spellOrTraitName))
        {
            Debug.LogError("The following effect method was not found: " + info.spellOrTraitName);
            return;
        }

        effectMethodIndex[info.spellOrTraitName](info);
    }

    private void Flow(DelayedInfo info)
    {
        Debug.Log("Flow");
    }







    private void PopulateIndex()
    {
        effectMethodIndex.Add("Flow", Flow);
    }
}