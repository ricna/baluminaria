using UnityEngine;

namespace BaluminariaBuilder
{
    [RequireComponent(typeof(Renderer), typeof(Collider))]
    public class BuilderSegment : MonoBehaviour
    {
        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        public void SetMaterial(Material mat)
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            _renderer.material = mat; // Aplica o material completo
        }

        public Color GetColor()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            // Tenta pegar a cor principal do material
            return _renderer.sharedMaterial.HasProperty("_BaseColor") ?
                   _renderer.sharedMaterial.GetColor("_BaseColor") :
                   _renderer.sharedMaterial.color;
        }

        public Material GetMaterial() => _renderer.sharedMaterial;
    }
}