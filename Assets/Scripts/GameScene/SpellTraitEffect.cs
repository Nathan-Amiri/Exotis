using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SpellTraitEffect : MonoBehaviour
{
    [SerializeField] private ExecutionCore executionCore;
    [SerializeField] private SlotAssignment slotAssignment;

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

    //.swap issue:
    //.SWAPPING NEEDS TO HAPPEN AFTER SIMULTANEOUS SPELLS
    //.my solution will be to convert all spell packets before calling any effect methods so that the elementals targeted
    //.don't change from swapping
    //.only an issue during spell ties

    private void Flow(SpellTraitEffectInfo info)
    {
        info.targets[0].DealDamage(2, info.caster);
        info.caster.HealthChange(1);
    }
    private void Cleanse(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].HealthChange(2);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
            info.caster.ToggleWearied(true);
        else // 2
            info.caster.ToggleWearied(false);
    }
    private void Mirage(SpellTraitEffectInfo info)
    {
        //.swap issue
        //.special treatment
    }
    private void TidalWave(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].HealthChange(3);
            info.caster.ToggleWeakened(true);

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else if (info.occurance == 1)
            info.targets[0].ToggleWeakened(false);
    }
    private void Erupt(SpellTraitEffectInfo info)
    {
        info.targets[0].DealDamage(3, info.caster);
        info.caster.DealDamage(1, info.caster);
    }
    private void Singe(SpellTraitEffectInfo info)
    {
        //.swap issue
        //.recast
        //.special treatment
    }
    private void HeatUp(SpellTraitEffectInfo info)
    {
        //.can't cast until swap
    }
    private void Hellfire(SpellTraitEffectInfo info)
    {
        info.targets[0].DealDamage(4, info.caster);
        info.caster.Eliminate();
    }
    private void Empower(SpellTraitEffectInfo info)
    {
        int damage = info.caster.isEmpowered ? 3 : 2;
        info.targets[0].DealDamage(damage, info.caster, true);
    }
    private void Fortify(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets.Add(slotAssignment.GetAlly(info.caster));

            info.caster.ToggleDisengaged(true);
            info.targets[0].ToggleArmored(true);

            executionCore.AddRoundEndDelayedEffect(1, info);
            executionCore.AddRoundStartDelayedEffect(2, info);
            executionCore.AddNextRoundEndDelayedEffect(3, info);
        }
        else if (info.occurance == 1)
        {
            info.caster.ToggleDisengaged(false);
            info.targets[0].ToggleArmored(false);
        }
        else if (info.occurance == 2)
            info.caster.ToggleWearied(true);
        else // 3
            info.caster.ToggleWearied(false);
    }
    private void Block(SpellTraitEffectInfo info)
    {
        if (info.recast)
        {
            foreach (Elemental target in info.targets)
                target.DealDamage(1, info.caster, true);

            info.caster.GetSpell("Block").ToggleRecast(false);
        }
        else if (info.occurance == 0)
        {
            info.caster.ToggleArmored(true);

            executionCore.AddRoundEndDelayedEffect(1, info);

            info.caster.GetSpell("Block").ToggleRecast(true);
        }
        else // 1
            info.caster.ToggleArmored(false);
    }
    private void Landslide(SpellTraitEffectInfo info)
    {
        foreach (Elemental target in info.targets)
            target.DealDamage(2, info.caster);
    }
    private void Swoop(SpellTraitEffectInfo info)
    {
        info.targets[0].DealDamage(2, info.caster);
    }
    private void TakeFlight(SpellTraitEffectInfo info)
    {
        //.swap issue
    }
    private void Whirlwind(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].DealDamage(2, info.caster);
            info.caster.ToggleDisengaged(true);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
        {
            info.caster.ToggleEnraged(true);
            info.caster.ToggleWeakened(true);
            info.caster.ToggleWearied(true);
        }
        else // 2
        {
            info.caster.ToggleEnraged(false);
            info.caster.ToggleWeakened(false);
            info.caster.ToggleWearied(false);
        }
    }
    private void Flurry(SpellTraitEffectInfo info)
    {
        //.swap issue
    }
    private void Surge(SpellTraitEffectInfo info)
    {
        info.targets[0].DealDamage(2, info.caster);
        if (!info.caster.hasCastSurge)
        {
            info.caster.ToggleSpark(true);
            info.caster.hasCastSurge = true;
        }
    }
    private void Blink(SpellTraitEffectInfo info)
    {
        //.can't cast until swap
        //.until spell occurs
    }
    private void Defuse(SpellTraitEffectInfo info)
    {
        //.can't cast until swap
    }
    private void Recharge(SpellTraitEffectInfo info)
    {
        info.targets[0].HealthChange(1);
        info.caster.ToggleSpark(true);
        slotAssignment.GetAlly(info.caster).ToggleSpark(true);
    }
    private void IcyBreath(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].DealDamage(2, info.caster);
            info.targets[0].ToggleSlowed(true);

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else // 1
            info.targets[0].ToggleSlowed(false);
    }
    private void Hail(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            foreach (Elemental target in info.targets)
            {
                target.DealDamage(1, info.caster);
                target.ToggleSlowed(true);
            }

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else // 1
            foreach (Elemental target in info.targets)
                target.ToggleSlowed(false);
    }
    private void Freeze(SpellTraitEffectInfo info)
    {
        //.can't cast until swap
    }
    private void NumbingCold(SpellTraitEffectInfo info)
    {
        //.special treatment
    }
    private void Enchain(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].DealDamage(2, info.caster);
            info.targets[0].ToggleTrapped(true);

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else // 1
            info.targets[0].ToggleTrapped(false);
    }
    private void Animate(SpellTraitEffectInfo info)
    {
        //.can't cast until swap
    }
    private void Nightmare(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
        {
            info.targets[0].ToggleStunned(true);
            info.caster.ToggleWearied(true);
        }
        else // 2
        {
            info.targets[0].ToggleStunned(false);
            info.caster.ToggleWearied(false);
        }
    }
    private void Hex(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            if (info.hexType == "slow") info.targets[0].ToggleSlowed(true);
            else if (info.hexType == "poison") info.targets[0].TogglePoisoned(true);
            else if (info.hexType == "weaken") info.targets[0].ToggleWeakened(true);

            if (!info.caster.hasCastHex)
            {
                info.caster.HealthChange(2);
                info.caster.hasCastHex = true;
            }
            else
                info.caster.HealthChange(1);

            executionCore.AddRoundEndDelayedEffect(1, info);
        }
        else // 1
        {
            if (info.hexType == "slow") info.targets[0].ToggleSlowed(false);
            else if (info.hexType == "poison") info.targets[0].TogglePoisoned(false);
            else if (info.hexType == "weaken") info.targets[0].ToggleWeakened(false);
        }
    }
    private void FangedBite(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].HealthChange(2);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
            info.targets[0].TogglePoisoned(true);
        else // 2
            info.targets[0].TogglePoisoned(false);
    }
    private void Infect(SpellTraitEffectInfo info)
    {
        info.targets[0].TogglePoisoned(true);
    }
    private void PoisonCloud(SpellTraitEffectInfo info)
    {
        //.recast
        //.special treatment
    }
    private void Cripple(SpellTraitEffectInfo info)
    {
        //.can't cast until swap
    }
    private void Sparkle(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].HealthChange(2);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
            info.targets[0].ToggleWeakened(true);
        else // 2
            info.targets[0].ToggleWeakened(false);
    }
    private void Allure(SpellTraitEffectInfo info)
    {
        //.special treatment--consider all possible problematic interactions
    }
    private void FairyDust(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
            executionCore.AddRoundStartDelayedEffect(1, info);
        else // 1
            foreach (Elemental target in info.targets)
                target.TogglePotion(true);
    }
    private void Gift(SpellTraitEffectInfo info)
    {
        if (info.occurance == 0)
        {
            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
        {
            info.targets[0].ToggleGem(true);
            info.caster.ToggleWearied(true);
        }
        else // 2
            info.caster.ToggleWearied(false);
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
    public bool recast;

    public Elemental caster;
    public List<Elemental> targets;

    public string hexType;
}