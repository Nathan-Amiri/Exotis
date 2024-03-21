using Mono.Cecil.Cil;
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
        info.targets[0].DealDamage(2, info.caster, true);
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
        else if (info.caster == null)
            return;
        else if (info.occurance == 1)
            info.caster.ToggleWearied(true);
        else // 2
            info.caster.ToggleWearied(false);
    }
    private void Mirage(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            slotAssignment.Swap(info.caster, info.targets[0]);

            info.targets[0].currentActions += 1;
            info.targets[0].ToggleTrapped(true);

            info.targets[0].mirageRedirectTarget = info.caster;

            info.caster.GetSpell("Mirage").hasBeenCast = true;

            executionCore.AddRoundStartDelayedEffect(1, info);
        }
        else if (info.targets[0] != null) // 1
        {
            info.targets[0].ToggleTrapped(false);

            info.targets[0].mirageRedirectTarget = null;
        }
    }
    private void TidalWave(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].DealDamage(3, info.caster, true);
            info.caster.ToggleWeakened(true);

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else if (info.caster != null) // 1
            info.caster.ToggleWeakened(false);
    }
    private void Erupt(EffectInfo info)
    {
        info.targets[0].DealDamage(3, info.caster, true);
        info.caster.DealDamage(1, info.caster, true, true);
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
        else if (info.caster != null) // 1
            info.caster.ToggleEnraged(false);
    }
    private void Hellfire(EffectInfo info)
    {
        info.targets[0].DealDamage(4, info.caster, true);
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
        else if (info.caster == null)
            return;
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
        else if (info.caster != null) // 1
            info.caster.GetSpell("Block").ToggleRecast(false);
    }
    private void Landslide(EffectInfo info)
    {
        foreach (Elemental target in info.targets)
            target.DealDamage(2, info.caster, true);
    }
    private void Swoop(EffectInfo info)
    {
        info.targets[0].DealDamage(2, info.caster, true);
    }
    private void TakeFlight(EffectInfo info)
    {
        info.targets[0].DealDamage(1, info.caster, true);
        info.caster.Heal(1);

        if (info.targets[1] != null)
            slotAssignment.Swap(info.caster, info.targets[1]);
    }
    private void Whirlwind(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].DealDamage(2, info.caster, true);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.caster == null)
            return;
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
        info.targets[0].DealDamage(2, info.caster, true);

        Spell spell = info.caster.GetSpell("Surge");
        if (!spell.hasBeenCast)
        {
            info.caster.ToggleSpark(true);
            spell.hasBeenCast = true;
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
        else if (info.caster != null) // 1
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
        else if (info.targets[0] != null) // 1
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
            info.targets[0].DealDamage(2, info.caster, true);
            info.targets[0].ToggleSlowed(true);

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else if (info.targets[0] != null) // 1
            info.targets[0].ToggleSlowed(false);
    }
    private void Hail(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            foreach (Elemental target in info.targets)
            {
                target.DealDamage(1, info.caster, true);
                target.ToggleSlowed(true);
            }

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else // 1
            foreach (Elemental target in info.targets)
                if (target != null)
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
            if (info.targets[0] != null)
                info.targets[0].ToggleDisengaged(false);
            if (info.caster != null)
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
        else if (info.occurance == 1 && info.targets[0] != null)
            info.targets[0].isNumb = false;
        else if (info.caster == null)
            return;
        else if (info.occurance == 2)
            info.caster.ToggleWearied(true);
        else // 2
            info.caster.ToggleWearied(false);
    }
    private void Enchain(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].DealDamage(2, info.caster, true);
            info.targets[0].ToggleTrapped(true);

            executionCore.AddNextRoundEndDelayedEffect(1, info);
        }
        else if (info.targets[0] != null) // 1
            info.targets[0].ToggleTrapped(false);
    }
    private void Animate(EffectInfo info)
    {
        slotAssignment.GetAlly(info.caster).currentActions += 1;

        info.caster.ToggleArmored(true);

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
            if (info.targets[0] != null)
                info.targets[0].ToggleStunned(true);
            if (info.caster != null)
                info.caster.ToggleWearied(true);
        }
        else // 2
        {
            if (info.targets[0] != null)
                info.targets[0].ToggleStunned(false);
            if (info.caster != null)
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

            Spell spell = info.caster.GetSpell("Hex");
            if (!spell.hasBeenCast)
            {
                info.caster.Heal(2);
                spell.hasBeenCast = true;
            }
            else
                info.caster.Heal(1);

            executionCore.AddRoundEndDelayedEffect(1, info);
        }
        else if (info.targets[0] != null) // 1
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
            info.targets[0].DealDamage(2, info.caster, true);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.targets[0] == null)
            return;
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
            info.targets[0].DealDamage(2, info.caster, true);

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
            if (info.caster != null)
            {
                info.caster.inPoisonCloud = false;

                info.caster.GetSpell("Poison Cloud").ToggleRecast(false);
            }

            foreach (Elemental elemental in info.caster.poisonedByPoisonCloud)
                if (elemental != null)
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
                if (target != null)
                    target.ToggleSlowed(false);
    }
    private void Sparkle(EffectInfo info)
    {
        if (info.occurance == 0)
        {
            info.targets[0].DealDamage(2, info.caster, true);

            executionCore.AddRoundStartDelayedEffect(1, info);
            executionCore.AddNextRoundEndDelayedEffect(2, info);
        }
        else if (info.targets[0] == null)
            return;
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
                if (target != null)
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
            if (info.targets[0] != null)
                info.targets[0].ToggleGem(true);
            if (info.caster != null)
                info.caster.ToggleWearied(true);
        }
        else if (info.caster != null) // 2
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