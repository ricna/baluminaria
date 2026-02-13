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
    }
}