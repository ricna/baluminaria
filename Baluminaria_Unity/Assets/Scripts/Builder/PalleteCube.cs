using UnityEngine;

namespace BaluminariaBuilder
{
    [RequireComponent(typeof(Collider), typeof(Renderer))]
    public class PaletteCube : MonoBehaviour
    {
        public Material GetMaterial()
        {
            // Retorna o material compartilhado (ou o instanciado se preferir)
            return GetComponent<Renderer>().sharedMaterial;
        }
        public Color GetColor()
        {
            // Tenta URP (_BaseColor) ou Standard (_Color)
            Renderer rend = GetComponent<Renderer>();
            if (rend.sharedMaterial.HasProperty("_BaseColor"))
                return rend.sharedMaterial.GetColor("_BaseColor");
            else
                return rend.sharedMaterial.color;
        }
    }
}