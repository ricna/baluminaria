using UnityEngine;

namespace BaluminariaBuilder
{
    [System.Serializable]
    public class ColorData
    {
        public float r, g, b, a;
        public ColorData(Color c) { r = c.r; g = c.g; b = c.b; a = c.a; }
        public Color ToColor() => new Color(r, g, b, a);
    }
}