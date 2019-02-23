using System;
using UnityEngine;
using Klak.Sensel;

public class SenselVisualizer : MonoBehaviour
{
    [SerializeField]
    ForceMapManager _manager;

    [SerializeField]
    Material _material;

    [System.Serializable]
    public enum InputMode
    {
        Raw,
        Filtered,
        Total,
        Convolved,
    };

    [SerializeField]
    InputMode _mode = InputMode.Raw;

    [SerializeField]
    Vector3 _accel;

    InputMode _prevMode;

    private void Awake()
    {
        if (_material == null)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                _material = renderer.material;
            }
        }
        if (_manager == null)
        {
            _manager = ForceMapManager.Instance;
        }
        _prevMode = _mode;
    }

    private void Start()
    {
        _material.SetTexture("_MainTex", GetTexture());
    }

    private void Update()
    {
        if (_prevMode != _mode)
        {
            _prevMode = _mode;
            _material.SetTexture("_MainTex", GetTexture());
        }

        var accel = SenselMaster.Acceleration;
        _accel = new Vector3((int)accel.x, (int)accel.y, (int)accel.z).normalized;
    }

    private Texture GetTexture()
    {
        switch (_mode)
        {
            case InputMode.Raw:
                return _manager.RawInputTexture;
            case InputMode.Filtered:
                return _manager.FilteredInputTexture;
            case InputMode.Total:
                return _manager.TotalInputTexture;
            case InputMode.Convolved:
                return _manager.ConvolvedInputTexture;
            default:
                return null;
        }
    }
}
