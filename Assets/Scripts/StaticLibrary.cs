using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticLibrary
{
    // Standard pixel size = 5 square units, pixeloid pixels match at font size 45

    // CONSOLE BUTTON SCALE 20 for ">>", 30 for "Pass"

    public static Dictionary<string, Color32> gameColors = new()
    {
        { "water", new(35, 182, 255, 255) }, // 23B6FF
        { "flame", new(255, 122, 0, 255) }, // FF7A00
        { "earth", new(180, 119, 53, 255) }, // B47735
        { "wind", new(205, 205, 255, 255) }, // D9CDFF
        { "lightning", new(255, 236, 0, 255) }, // FFEC00
        { "frost", new(140, 228, 232, 255) }, // 8CE4E8
        { "shadow", new(101, 94, 81, 255) }, // 655E51
        { "venom", new(23, 195, 0, 255) }, // 17C300
        { "jewel", new(255, 132, 230, 255) }, // FF84E8

        { "fastHealthBack", Color.green }, // 00FF00
        { "mediumHealthBack", Color.yellow }, // FFEB04
        { "slowHealthBack", new(190, 0, 190, 255) }, // BE00BE

        // Game scene colors:
        { "gray", new(185, 185, 185, 255) }, // B9B9B9
        { "pink", new(255, 0, 150, 255) }, // FF0096
        { "blue", new(125, 190, 255, 255) }, // 7DBEFF

        { "allyOutline", new(62, 165, 168, 255) }, // 3EA5A8
        { "enemyOutline", new(138, 46, 92, 255) }, // 8A2E5C

        { "allyText", new(0, 100, 255, 255) }, // 0064FF
        { "enemyText", new(160, 0, 120, 255) }, // A00078
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
}