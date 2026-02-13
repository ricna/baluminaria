using UnityEngine;

namespace BaluminariaBuilder
{
    public enum ColorPattern
    {
        Solid,
        VerticalStripes,
        HorizontalStripes,
        DiagonalStripes
    }

    [System.Serializable]
    public class ColorData
    {
        public float r, g, b, a;
        public ColorData(Color c) { r = c.r; g = c.g; b = c.b; a = c.a; }
        public Color ToColor() => new Color(r, g, b, a);
    }

    [System.Serializable]
    public class BaluminariaPreset
    {
        public string pilotName = "Piloto_Sem_Nome";
        public ColorData[] segmentColors = new ColorData[112];

        // Cores das partes estáticas
        public ColorData basketColor = new ColorData(Color.white);
        public ColorData ropesColor = new ColorData(Color.white);
        public ColorData socketColor = new ColorData(Color.white);
        public ColorData mouthColor = new ColorData(Color.white);
        public ColorData[] ringsColors = new ColorData[7];
    }
}