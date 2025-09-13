using UnityEngine;

public class Segment : MonoBehaviour
{
    [SerializeField]
    private Light _light;
    [SerializeField]
    private float _range = 1f;
    private void Awake()
    {
        if (_light == null)
        {
            _light = GetComponentInChildren<Light>();
        }
        _light.range = _range;
    }

    public Color CurrentColor
    {
        get { return _light != null ? _light.color : Color.black; }
        set { if (_light != null) _light.color = value; }
    }

    public void ChangeLightColor(Color color)
    {
        CurrentColor = color;
    }
    public void SetIntensity(float intensity)
    {
        if (_light != null)
        {
            _light.intensity = intensity;
        }

    }

}
