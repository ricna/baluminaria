using UnityEngine;

public class Baluminaria : MonoBehaviour
{

    [SerializeField]
    private Segment[] _prefabSegments; // Array to hold segment our 7 prefabs

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        for (int i = 0; i < 16; i++)
        {
            foreach (var segment in _prefabSegments)
            {
                var newSegment = Instantiate(segment, transform);
                newSegment.transform.Rotate(Vector3.up, i * 22.5f);
            }
        }
    }
    public void ChangeSegmentLightColor(Color color)
    {
        foreach (var segment in _prefabSegments)
        {
            segment.ChangeLightColor(color);
        }
    }
}