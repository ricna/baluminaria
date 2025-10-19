using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Representa uma cor física de filamento disponível.
/// Este objeto serve como seletor visual.
/// </summary>
[RequireComponent(typeof(Renderer), typeof(Collider))]
public class FilamentColorReference : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string _filamentName = "SemNome";
    [SerializeField] private Color _filamentColor = Color.white;

    public string FilamentName { get { return _filamentName; } }
    public Color FilamentColor { get { return _filamentColor; } }

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        /*if (_renderer != null)
        {
            _renderer.material.color = _filamentColor;
        }*/
        _filamentColor = _renderer.material.color;
        _filamentName = _renderer.material.name;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (PersonalizationManager.Instance == null) return;

        // Informa o manager que essa cor foi selecionada
        PersonalizationManager.Instance.SetSelectedFilament(this);
    }

    public Color GetColor()
    {
        return _filamentColor;
    }
}
