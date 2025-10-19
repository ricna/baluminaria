using System;
using UnityEngine;

[Serializable]
public class ColorData
{
    public float r;
    public float g;
    public float b;
    public float a;

    public ColorData()
    {
        r = 0f;
        g = 0f;
        b = 0f;
        a = 1f;
    }

    public ColorData(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }

    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }

    public static ColorData FromColor(Color color)
    {
        return new ColorData(color);
    }
}

[Serializable]
public class BaluminariaData
{
    public string nomeBaluminaria;
    public ColorData[] segmentColors; // deve ter 112 itens
    public string selectedMusicPath;
    public string author;
    public string timestamp;

    public BaluminariaData()
    {
        nomeBaluminaria = "SemNome";
        segmentColors = new ColorData[112];
        for (int i = 0; i < segmentColors.Length; i++)
        {
            segmentColors[i] = new ColorData(Color.black);
        }
        selectedMusicPath = "";
        author = "";
        timestamp = DateTime.UtcNow.ToString("o");
    }
}
