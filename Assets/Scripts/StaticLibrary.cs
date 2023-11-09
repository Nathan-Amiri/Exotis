using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticLibrary
{
    //standard pixel size = 5 square units, pixeloid pixels match at font size 45

    //CONSOLE BUTTON SCALE 20 for ">>", 30 for "Pass"

    public static Dictionary<string, Color> gameColors = new()
    {
        { "water", new(35, 182, 255, 255) }, //23B6FF
        { "flame", new(255, 122, 0, 255) }, //FF7A00
        { "earth", new(180, 119, 53, 255) }, //B47735
        { "wind", new(205, 205, 255, 255) }, //D9CDFF
        { "lightning", new(255, 236, 0, 255) }, //FFEC00
        { "frost", new(140, 228, 232, 255) }, //8CE4E8
        { "shadow", new(101, 94, 81, 255) }, //655E51
        { "venom", new(23, 195, 0, 255) }, //17C300
        { "jewel", new(255, 132, 230, 255) }, //FF84E8

        { "fastHealthBack", Color.green }, //00FF00
        { "mediumHealthBack", Color.yellow }, //FFEB04
        { "slowHealthBack", new(190, 0, 190, 255) }, //BE00BE

        //game scene colors:
        { "gray", new(185, 185, 185, 255) }, //B9B9B9
        { "pink", new(255, 0, 150, 255) }, //FF0096
        { "blue", new(125, 190, 255, 255) }, //7DBEFF
        { "allyRetreatButton", new(255, 0, 150, 255) }, //FF0096, standard pink
        { "enemyRetreatButton", new(180, 180, 180, 255) }, //B4B4B4


        { "allyOutline", new(62, 165, 168, 255) }, //3EA5A8
        { "enemyOutline", new(138, 46, 92, 255) }, //8A2E5C

        { "allyText", new(0, 100, 255, 255) }, //0064FF
        { "enemyText", new(160, 0, 120, 255) }, //A00078
    };

    public static List<string> validElementalNames = new()
    {
        { "Will-o'-Wisp" },
        { "Selkie" },
        { "Nymph" },
        { "Kraken" },
        { "Leviathan" },
        { "Hydra" },
        { "Siren" },
        { "Mermaid" },
        { "Salamander" },
        { "Phoenix" },
        { "Dragon" },
        { "Wizard" },
        { "Hellhound" },
        { "Chimera" },
        { "Genie" },
        { "Roc" },
        { "Golem" },
        { "Yeti" },
        { "Ogre" },
        { "Scorpio" },
        { "Dwarf" },
        { "Griffin" },
        { "Ghost" },
        { "Wraith" },
        { "Strix" },
        { "Fairy" },
        { "Thunderbird" },
        { "Gargoyle" },
        { "Abomination" },
        { "Angel" },
        { "Werewolf" },
        { "Wyrm" },
        { "Unicorn" },
        { "Witch" },
        { "Shaman" },
        { "Basilisk" }
    };

    public static List<string> validSpellNames = new()
    {
        { "Flow" },
        { "Cleanse" },
        { "Mirage" },
        { "Tidal Wave" },
        { "Erupt" },
        { "Singe" },
        { "Heat Up" },
        { "Hellfire" },
        { "Empower" },
        { "Fortify" },
        { "Block" },
        { "Landslide" },
        { "Swoop" },
        { "Take Flight" },
        { "Whirlwind" },
        { "Ascend" },
        { "Surge" },
        { "Blink" },
        { "Defuse" },
        { "Recharge" },
        { "Icy Breath" },
        { "Hail" },
        { "Freeze" },
        { "Numbing Cold" },
        { "Enchain" },
        { "Animate" },
        { "Nightmare" },
        { "Hex" },
        { "Fanged Bite" },
        { "Infect" },
        { "Poison Cloud" },
        { "Bile" },
        { "Sparkle" },
        { "Allure" },
        { "Fairy Dust" },
        { "Gift" }
    };

    ////create second dictionar*iesy with flipped keys and values so that the name/number indexes can be searchable in either direction
    //public static void InitializeIndex()
    //{
    //    if (elementalNumberFinder.Count == 0)
    //        foreach (var entry in elementalNameFinder)
    //            elementalNumberFinder.Add(entry.Value, entry.Key);
    //    else
    //        Debug.LogError("Elemental index already initialized!");

    //    if (spellNumberFinder.Count == 0)
    //        foreach (var entry in spellNameFinder)
    //            spellNumberFinder.Add(entry.Value, entry.Key);
    //    else
    //        Debug.LogError("Spell index already initialized!");
    //}

    //public static Dictionary<int, string> elementalNameFinder = new()
    //{
    //    { 0, "Flow" },
    //    { 1, "Cleanse" },
    //    { 2, "Mirage" },
    //    { 3, "Tidal Wave" },
    //    { 4, "Erupt" },
    //    { 5, "Singe" },
    //    { 6, "Heat Up" },
    //    { 7, "Hellfire" },
    //    { 8, "Empower" },
    //    { 9, "Fortify" },
    //    { 10, "Block" },
    //    { 11, "Landslide" },
    //    { 12, "Swoop" },
    //    { 13, "Take Flight" },
    //    { 14, "Whirlwind" },
    //    { 15, "Ascend" },
    //    { 16, "Surge" },
    //    { 17, "Blink" },
    //    { 18, "Defuse" },
    //    { 19, "Recharge" },
    //    { 20, "Icy Breath" },
    //    { 21, "Hail" },
    //    { 22, "Freeze" },
    //    { 23, "Numbing Cold" },
    //    { 24, "Enchain" },
    //    { 25, "Animate" },
    //    { 26, "Nightmare" },
    //    { 27, "Hex" },
    //    { 28, "Fanged Bite" },
    //    { 29, "Infect" },
    //    { 30, "Poison Cloud" },
    //    { 31, "Bile" },
    //    { 32, "Sparkle" },
    //    { 33, "Allure" },
    //    { 34, "Fairy Dust" },
    //    { 35, "Gift" },
    //};

    //public static Dictionary<string, int> elementalNumberFinder = new() { };

    //public static Dictionary<int, string> spellNameFinder = new()
    //{
    //    { 0, "Will-o'-Wisp" },
    //    { 1, "Selkie" },
    //    { 2, "Nymph" },
    //    { 3, "Kraken" },
    //    { 4, "Leviathan" },
    //    { 5, "Hydra" },
    //    { 6, "Siren" },
    //    { 7, "Mermaid" },
    //    { 8, "Salamander" },
    //    { 9, "Phoenix" },
    //    { 10, "Dragon" },
    //    { 11, "Wizard" },
    //    { 12, "Hellhound" },
    //    { 13, "Chimera" },
    //    { 14, "Genie" },
    //    { 15, "Roc" },
    //    { 16, "Golem" },
    //    { 17, "Yeti" },
    //    { 18, "Ogre" },
    //    { 19, "Scorpio" },
    //    { 20, "Dwarf" },
    //    { 21, "Griffin" },
    //    { 22, "Ghost" },
    //    { 23, "Wraith" },
    //    { 24, "Strix" },
    //    { 25, "Fairy" },
    //    { 26, "Thunderbird" },
    //    { 27, "Gargoyle" },
    //    { 28, "Abomination" },
    //    { 29, "Angel" },
    //    { 30, "Werewolf" },
    //    { 31, "Wyrm" },
    //    { 32, "Unicorn" },
    //    { 33, "Witch" },
    //    { 34, "Shaman" },
    //    { 35, "Basilisk" },
    //};

    //public static Dictionary<string, int> spellNumberFinder = new() { };
}