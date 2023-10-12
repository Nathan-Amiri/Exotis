using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticLibrary
{
    //standard pixel size = 5 square units, pixeloid pixels match at font size 45

    //POISON STATUS NEEDS TO BE RAISED UP 1 UNIT
    //RETREAT BUTTON B4B4B4 ENEMY, FF0096 ALLY

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

        { "offwhite", new(240, 240, 240, 255) }, //F0F0F0
        { "gray", new(185, 185, 185, 255) }, //B9B9B9

        { "allyOutline", new(62, 165, 168, 255) }, //3EA5A8
        { "enemyOutline", new(138, 46, 92, 255) }, //8A2E5C

        { "fastHealthBack", Color.green }, //00FF00
        { "mediumHealthBack", Color.yellow }, //FFEB04
        //{ "slowHealthBack", Color.red } //FF0000
        { "slowHealthBack", new(190, 0, 190, 255) } //BE00BE
    };
}