using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellTraitEffect : MonoBehaviour
{
    private delegate void EffectMethod(SpellTraitEffectInfo info);

    private readonly Dictionary<string, EffectMethod> effectMethodIndex = new();

    private void Awake()
    {
        PopulateIndex();
    }

    public void CallEffectMethod(SpellTraitEffectInfo info)
    {
        if (!effectMethodIndex.ContainsKey(info.spellOrTraitName))
        {
            Debug.LogError("The following effect method was not found: " + info.spellOrTraitName);
            return;
        }

        effectMethodIndex[info.spellOrTraitName](info);
    }

    private void Flow(SpellTraitEffectInfo info)
    {
        Debug.Log("Flow");
    }



    // I'll need to add a lot more action available things, maybe--like hellhound's trait, and Gems....
    // do I want to make Gems unusable if full hp but not do the same check for cleanse? That's fine, I just need to decide



    // Run in Awake
    private void PopulateIndex()
    {
        effectMethodIndex.Add("Flow", Flow);
    }
}
public struct SpellTraitEffectInfo
{
    public string spellOrTraitName;

    public int occurance;

    public Elemental caster;
    public List<Elemental> targets;
}