using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpellTraitEffect : MonoBehaviour
{
    [SerializeField] private ExecutionCore executionCore;
    [SerializeField] private SlotAssignment slotAssignment;

    private delegate void EffectMethod(EffectInfo info);

    private readonly Dictionary<string, EffectMethod> effectMethodIndex = new();

    private void Awake()
    {
        PopulateIndex();
    }

    public void CallEffectMethod(EffectInfo info)
    {
        if (!effectMethodIndex.ContainsKey(info.spellOrTraitName))
        {
            Debug.LogError("The following effect method was not found: " + info.spellOrTraitName);
            return;
        }

        effectMethodIndex[info.spellOrTraitName](info);
    }

    private void Flow(EffectInfo info)
    {
        info.targets[0].DealDamage(2, info.caster);
        info.caster.Heal(1);
    }
    private void Cleanse(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].Heal(2);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
            info.caster.ToggleWearied(true);
        else // 2
            info.caster.ToggleWearied(false);
    }
    private void Mirage(EffectInfo info)
    {
        //.special treatment
    }
    private void TidalWave(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].Heal(3);
            info.caster.ToggleWeakened(true);

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else if (info.occurance == 1)
            info.targets[0].ToggleWeakened(false);
    }
    private void Erupt(EffectInfo info)
    {
        info.targets[0].DealDamage(3, info.caster);
        info.caster.DealDamage(1, info.caster);
    }
    private void Singe(EffectInfo info)
    {
        //.special treatment
    }
    private void HeatUp(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.caster.TogglePotion(true);
            info.caster.ToggleEnraged(true);

            executionCore.AddRoundEndDelayedEffect(1, info);

            info.caster.GetSpell("Heat Up").cannotCastUntilSwap = true;
        }
        else // 1
        {
            info.caster.ToggleEnraged(false);
        }
    }
    private void Hellfire(EffectInfo info)
    {
        info.targets[0].DealDamage(4, info.caster);
        info.caster.Eliminate();
    }
    private void Empower(EffectInfo info)
    {
        int damage = info.caster.isEmpowered ? 3 : 2;
        info.targets[0].DealDamage(damage, info.caster, true);
    }
    private void Fortify(EffectInfo info)
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
            info.caster.ToggleDisengaged(false);
        else if (info.occurance == 2)
            info.caster.ToggleWearied(true);
        else // 3
            info.caster.ToggleWearied(false);
    }
    private void Block(EffectInfo info)
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

            info.caster.GetSpell("Block").ToggleRecast(true);

            executionCore.AddRoundEndDelayedEffect(1, info);
        }
        else // 1
            info.caster.GetSpell("Block").ToggleRecast(false);
    }
    private void Landslide(EffectInfo info)
    {
        foreach (Elemental target in info.targets)
            target.DealDamage(2, info.caster);
    }
    private void Swoop(EffectInfo info)
    {
        info.targets[0].DealDamage(2, info.caster);
    }
    private void TakeFlight(EffectInfo info)
    {
        info.targets[0].DealDamage(1, info.caster);
        info.caster.Heal(1);

        if (info.targets[1] != null)
            slotAssignment.Swap(info.caster, info.targets[1]);
    }
    private void Whirlwind(EffectInfo info)
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
    private void Flurry(EffectInfo info)
    {
        slotAssignment.Swap(slotAssignment.GetAlly(info.caster), info.targets[0]);
    }
    private void Surge(EffectInfo info)
    {
        info.targets[0].DealDamage(2, info.caster);
        if (!info.caster.hasCastSurge)
        {
            info.caster.ToggleSpark(true);
            info.caster.hasCastSurge = true;
        }
    }
    private void Blink(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.caster.ToggleSpark(true);
            info.caster.ToggleEnraged(true);

            executionCore.AddAfterSpellOccursDelayedEffect(1, info);

            info.caster.GetSpell("Blink").cannotCastUntilSwap = true;
        }
        else // 1
            info.caster.ToggleEnraged(false);
    }
        private void Defuse(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].ToggleStunned(true);

            executionCore.AddRoundEndDelayedEffect(1, info);

            info.caster.GetSpell("Defuse").cannotCastUntilSwap = true;
        }
        else // 1
            info.targets[0].ToggleStunned(false);
    }
    private void Recharge(EffectInfo info)
    {
        info.targets[0].Heal(1);
        info.caster.ToggleSpark(true);
        slotAssignment.GetAlly(info.caster).ToggleSpark(true);
    }
    private void IcyBreath(EffectInfo info)
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
    private void Hail(EffectInfo info)
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
    private void Freeze(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].ToggleDisengaged(true);
            info.caster.ToggleDisengaged(true);

            executionCore.AddNextRoundEndDelayedEffect(1, info);

            info.caster.GetSpell("Freeze").cannotCastUntilSwap = true;
        }
        else // 1
        {
            info.targets[0].ToggleDisengaged(false);
            info.caster.ToggleDisengaged(false);
        }
    }
    private void NumbingCold(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].isNumb = true;

            executionCore.AddRoundEndDelayedEffect(1, info);
            executionCore.AddRoundStartDelayedEffect(2, info);
            executionCore.AddNextRoundEndDelayedEffect(3, info);
        }
        else if (info.occurance == 1)
            info.targets[0].isNumb = false;
        else if (info.occurance == 2)
            info.caster.ToggleWearied(true);
        else // 2
            info.caster.ToggleWearied(false);
    }
    private void Enchain(EffectInfo info)
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
    private void Animate(EffectInfo info)
    {
        slotAssignment.GetAlly(info.caster).currentActions += 1;

        info.caster.ToggleArmored(true);

        executionCore.AddNextRoundEndDelayedEffect(1, info);

        info.caster.GetSpell("Animate").cannotCastUntilSwap = true;
    }
    private void Nightmare(EffectInfo info)
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
    private void Hex(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            if (info.hexType == "slow") info.targets[0].ToggleSlowed(true);
            else if (info.hexType == "poison") info.targets[0].TogglePoisoned(true);
            else if (info.hexType == "weaken") info.targets[0].ToggleWeakened(true);

            if (!info.caster.hasCastHex)
            {
                info.caster.Heal(2);
                info.caster.hasCastHex = true;
            }
            else
                info.caster.Heal(1);

            executionCore.AddRoundEndDelayedEffect(1, info);
        }
        else // 1
        {
            if (info.hexType == "slow") info.targets[0].ToggleSlowed(false);
            else if (info.hexType == "poison") info.targets[0].TogglePoisoned(false);
            else if (info.hexType == "weaken") info.targets[0].ToggleWeakened(false);
        }
    }
    private void FangedBite(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].Heal(2);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
            info.targets[0].TogglePoisoned(true);
        else // 2
            info.targets[0].TogglePoisoned(false);
    }
    private void Infect(EffectInfo info)
    {
        info.targets[0].TogglePoisoned(true);
    }
    private void PoisonCloud(EffectInfo info)
    {
        if (info.recast)
        {
            info.targets[0].DealDamage(2, info.caster);

            info.caster.GetSpell("Poison Cloud").ToggleRecast(false);
        }
        else if (info.occurance == 0)
        {
            info.caster.inPoisonCloud = true;

            info.caster.GetSpell("Poison Cloud").ToggleRecast(true);

            executionCore.AddRoundEndDelayedEffect(1, info);
        }
        else // 1
        {
            info.caster.inPoisonCloud = false;

            info.caster.GetSpell("Poison Cloud").ToggleRecast(false);

            foreach (Elemental elemental in info.caster.poisonedByPoisonCloud)
                elemental.TogglePoisoned(false);
        }
    }
    private void Cripple(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            foreach (Elemental availableTarget in slotAssignment.GetAllAvailableTargets(info.caster, true))
                if (availableTarget.PoisonStrength > 0)
                {
                    info.targets.Add(availableTarget);

                    availableTarget.ToggleSlowed(true);
                }

            executionCore.AddRoundEndDelayedEffect(1, info);

            info.caster.currentActions += 1;

            info.caster.GetSpell("Cripple").cannotCastUntilSwap = true;
        }
        else // 1
            foreach (Elemental target in info.targets)
                target.ToggleSlowed(false);
    }
    private void Sparkle(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].Heal(2);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.occurance == 1)
            info.targets[0].ToggleWeakened(true);
        else // 2
            info.targets[0].ToggleWeakened(false);
    }
    private void Allure(EffectInfo info)
    {
        executionCore.AllureRedirect(info.caster, slotAssignment.GetAlly(info.caster));
    }
    private void FairyDust(EffectInfo info)
    {
        if (info.occurance == 0)
            executionCore.AddRoundStartDelayedEffect(1, info);
        else // 1
            foreach (Elemental target in info.targets)
                target.TogglePotion(true);
    }
    private void Gift(EffectInfo info)
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



    // I'll need to add a lot more action available things, maybe--like hellhound's trait



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