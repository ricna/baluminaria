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

        // Usado pelo Manager ao aplicar padrões ou clicar com material selecionado
        public void SetMaterial(Material mat)
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            _renderer.material = mat;
        }

        // Usado pelo Manager ao carregar do JSON (muda apenas a cor do material atual)
        public void SetColor(Color color)
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();

            // Tenta URP (_BaseColor) ou Standard (_Color)
            if (_renderer.material.HasProperty("_BaseColor"))
                _renderer.material.SetColor("_BaseColor", color);
            else
                _renderer.material.color = color;
        }

        public Color GetColor()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();

            if (_renderer.sharedMaterial.HasProperty("_BaseColor"))
                return _renderer.sharedMaterial.GetColor("_BaseColor");

            return _renderer.sharedMaterial.color;
        }

        // ESSENCIAL: Mantido para o Picker da CameraController funcionar
        public Material GetMaterial()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            return _renderer.sharedMaterial;
        }
    }
}