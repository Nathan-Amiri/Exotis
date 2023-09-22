using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticLibrary
{
    public static Dictionary<string, Color> elementColors = new()
    {
        { "water", new(35, 182, 255, 255) },
        { "flame", new(255, 122, 0, 255) },
        { "earth", new(180, 119, 53, 255) },
        { "wind", new(205, 205, 255, 255) },
        { "lightning", new(255, 236, 0, 255) },
        { "frost", new(140, 228, 232, 255) },
        { "shadow", new(101, 94, 81, 255) },
        { "venom", new(23, 195, 0, 255) },
        { "jewel", new(255, 132, 230, 255) }
    };
}