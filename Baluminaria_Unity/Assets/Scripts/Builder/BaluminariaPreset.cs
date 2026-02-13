using UnityEngine;

namespace BaluminariaBuilder
{
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