using System;
using UnityEngine;
using Klak.Sensel;

public class SenselVisualizer : MonoBehaviour
{
    [SerializeField]
    Material _material;

    [System.Serializable]
    public enum InputMode
    {
        Raw,
        Filtered,
        Total
    };

    [SerializeField]
    InputMode _mode;

    [SerializeField]
    Vector3 _accel;

    InputMode _prevMode;
    ForceMap _forceMap;

    private void Awake()
    {
        if (_material == null) {
            var renderer = GetComponent<Renderer>();
            if (renderer != null) {
                _material = renderer.material;
            }
        }
        _forceMap = new ForceMap();
        _prevMode = _mode;
        _material.SetTexture("_MainTex", GetTexture());
    }

    private void Update()
    {
        if (_prevMode != _mode)
        {
            _prevMode = _mode;
            _material.SetTexture("_MainTex", GetTexture());
        }
        _forceMap.Update();

        var accel = SenselMaster.Acceleration;
        _accel = new Vector3((int)accel.x, (int)accel.y, (int)accel.z).normalized;

        if (_accel != Vector3.zero) {
            transform.rotation = Quaternion.Euler(90, 0, 0) * Quaternion.LookRotation(_accel);
        }
    }

    private void OnDestroy()
    {
        _forceMap.Dispose();
    }

    private Texture GetTexture()
    {
        switch (_mode) {
            case InputMode.Raw:
                return _forceMap.RawInputTexture;
            case InputMode.Filtered:
                return _forceMap.FilteredInputTexture;
            case InputMode.Total:
                return _forceMap.TotalInputTexture;
            default:
                return null;
        }
    }
}
