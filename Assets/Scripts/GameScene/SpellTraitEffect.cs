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

    }
    private void Cleanse(SpellTraitEffectInfo info)
    {

    }
    private void Mirage(SpellTraitEffectInfo info)
    {

    }
    private void TidalWave(SpellTraitEffectInfo info)
    {

    }
    private void Erupt(SpellTraitEffectInfo info)
    {

    }
    private void Singe(SpellTraitEffectInfo info)
    {

    }
    private void HeatUp(SpellTraitEffectInfo info)
    {

    }
    private void Hellfire(SpellTraitEffectInfo info)
    {

    }
    private void Empower(SpellTraitEffectInfo info)
    {

    }
    private void Fortify(SpellTraitEffectInfo info)
    {

    }
    private void Block(SpellTraitEffectInfo info)
    {

    }
    private void Landslide(SpellTraitEffectInfo info)
    {

    }
    private void Swoop(SpellTraitEffectInfo info)
    {

    }
    private void TakeFlight(SpellTraitEffectInfo info)
    {

    }
    private void Whirlwind(SpellTraitEffectInfo info)
    {

    }
    private void Flurry(SpellTraitEffectInfo info)
    {

    }
    private void Surge(SpellTraitEffectInfo info)
    {

    }
    private void Blink(SpellTraitEffectInfo info)
    {

    }
    private void Defuse(SpellTraitEffectInfo info)
    {

    }
    private void Recharge(SpellTraitEffectInfo info)
    {

    }
    private void IcyBreath(SpellTraitEffectInfo info)
    {

    }
    private void Hail(SpellTraitEffectInfo info)
    {

    }
    private void Freeze(SpellTraitEffectInfo info)
    {

    }
    private void NumbingCold(SpellTraitEffectInfo info)
    {

    }
    private void Enchain(SpellTraitEffectInfo info)
    {

    }
    private void Animate(SpellTraitEffectInfo info)
    {

    }
    private void Nightmare(SpellTraitEffectInfo info)
    {

    }
    private void Hex(SpellTraitEffectInfo info)
    {

    }
    private void FangedBite(SpellTraitEffectInfo info)
    {

    }
    private void Infect(SpellTraitEffectInfo info)
    {

    }
    private void PoisonCloud(SpellTraitEffectInfo info)
    {

    }
    private void Cripple(SpellTraitEffectInfo info)
    {

    }
    private void Sparkle(SpellTraitEffectInfo info)
    {

    }
    private void Allure(SpellTraitEffectInfo info)
    {

    }
    private void FairyDust(SpellTraitEffectInfo info)
    {

    }
    private void Gift(SpellTraitEffectInfo info)
    {

    }



    // I'll need to add a lot more action available things, maybe--like hellhound's trait, and Gems....
    // do I want to make Gems unusable if full hp but not do the same check for cleanse? That's fine, I just need to decide



    // Run in Awake
    private void PopulateIndex()
    {
        effectMethodIndex.Add("Flow", Flow);
        effectMethodIndex.Add("Cleanse", Cleanse);
        effectMethodIndex.Add("Mirage", Mirage);
        effectMethodIndex.Add("Tidal Wave", TidalWave);
        effectMethodIndex.Add("Erupt", Erupt);
        effectMethodIndex.Add("Singe", Singe);
        effectMethodIndex.Add("Heat Up", HeatUp);
        effectMethodIndex.Add("Hellfire", Hellfire);
        effectMethodIndex.Add("Empower", Empower);
        effectMethodIndex.Add("Fortify", Fortify);
        effectMethodIndex.Add("Block", Block);
        effectMethodIndex.Add("Landslide", Landslide);
        effectMethodIndex.Add("Swoop", Swoop);
        effectMethodIndex.Add("Take Flight", TakeFlight);
        effectMethodIndex.Add("Whirlwind", Whirlwind);
        effectMethodIndex.Add("Flurry", Flurry);
        effectMethodIndex.Add("Surge", Surge);
        effectMethodIndex.Add("Blink", Blink);
        effectMethodIndex.Add("Defuse", Defuse);
        effectMethodIndex.Add("Recharge", Recharge);
        effectMethodIndex.Add("Icy Breath", IcyBreath);
        effectMethodIndex.Add("Hail", Hail);
        effectMethodIndex.Add("Freeze", Freeze);
        effectMethodIndex.Add("Numbing Cold", NumbingCold);
        effectMethodIndex.Add("Enchain", Enchain);
        effectMethodIndex.Add("Animate", Animate);
        effectMethodIndex.Add("Nightmare", Nightmare);
        effectMethodIndex.Add("Hex", Hex);
        effectMethodIndex.Add("Fanged Bite", FangedBite);
        effectMethodIndex.Add("Infect", Infect);
        effectMethodIndex.Add("Poison Cloud", PoisonCloud);
        effectMethodIndex.Add("Cripple", Cripple);
        effectMethodIndex.Add("Sparkle", Sparkle);
        effectMethodIndex.Add("Allure", Allure);
        effectMethodIndex.Add("Fairy Dust", FairyDust);
        effectMethodIndex.Add("Gift", Gift);
    }
}
public struct SpellTraitEffectInfo
{
    public string spellOrTraitName;

    public int occurance;

    public Elemental caster;
    public List<Elemental> targets;
}