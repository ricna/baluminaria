using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class SegmentClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Segment _segment;

    private void Awake()
    {
        _segment = GetComponent<Segment>();
        if (_segment == null)
        {
            // tenta encontrar no pai caso o script esteja num filho do prefab
            _segment = GetComponentInParent<Segment>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // somente age se o PersonalizationManager estiver ativo e no modo de edição
        if (PersonalizationManager.Instance == null) return;
        
        Debug.Log($"SegmentClickHandler: Segmento {_segment.name} clicado.");
        PersonalizationManager.Instance.OnSegmentClicked(_segment);
    }
}
