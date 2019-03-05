using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Klak.Sensel;

public class ForceMapManager : MonoBehaviour
{
    #region Singleton

    static ForceMapManager s_instance;
    public static ForceMapManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindObjectOfType<ForceMapManager>();
            }
            return s_instance;
        }
    }

    #endregion

    #region Serialized fields

    [SerializeField]
    Shader _convolveShader = null;

    [SerializeField]
    Material _blitMat = null;

    [SerializeField, Range(0, 10)]
    float _decayTime = 1;

    [SerializeField, Range(0, 10)]
    float _sensitivity = 0.2f;

    [SerializeField, Range(0.01f, 5)]
    float _power = 1;

    [SerializeField]
    bool _update = true;
    public bool update
    {
        get { return _update; }
        set { _update = value; }
    }
    
    [SerializeField]
    bool _smooth = true;

    #endregion

    #region Other Fields

    ForceMap _forceMap;
    public ForceMap forceMap
    {
        get { return _forceMap; }
    }

    public Texture RawInputTexture
    {
        get { return _forceMap.RawInputTexture; }
    }

    public Texture FilteredInputTexture
    {
        get { return _forceMap.FilteredInputTexture; }
    }

    public Texture TotalInputTexture
    {
        get { return _forceMap.TotalInputTexture; }
    }

    RenderTexture _mappedInput;
    public Texture MappedInputTexture
    {
        get { return _mappedInput; }
    }

    RenderTexture _convolvedInput;
    public Texture ConvolvedInputTexture
    {
        get { return _convolvedInput; }
    }

    float _forceSum;
    public float forceSum
    {
        get { return _forceSum; }
    }

    float _forceAverage;
    public float forceAverage
    {
        get { return _forceAverage; }
    }

    // [SerializeField, Range(0, 5)]
    float _smoothedForceAverage;
    public float smoothedForceAverage
    {
        get { return _smoothedForceAverage; }
    }

    Material _convolutionFilter;
    int _idInterpolant;

    #endregion

    private void Awake()
    {
        _idInterpolant = Shader.PropertyToID("_Interpolant");

        _convolutionFilter = new Material(_convolveShader);

        _forceMap = new ForceMap();
        _convolvedInput = new RenderTexture(FilteredInputTexture.width, FilteredInputTexture.height, 0, RenderTextureFormat.RFloat);
        _convolvedInput.wrapMode = TextureWrapMode.Clamp;
        _convolvedInput.filterMode = FilterMode.Trilinear;

        _mappedInput = new RenderTexture(FilteredInputTexture.width, FilteredInputTexture.height, 0, RenderTextureFormat.RFloat);
        _mappedInput.wrapMode = TextureWrapMode.Clamp;
        _mappedInput.filterMode = FilterMode.Trilinear;

        _convolutionFilter.SetTexture("_RawInputTex", FilteredInputTexture);
    }

    private void Start()
    {
        Graphics.Blit(_convolvedInput, _convolvedInput, _convolutionFilter, 0);
        _smoothedForceAverage = 0f;
    }

    private void Update()
    {
        if (_update) UpdateForce();
    }

    public void UpdateForce()
    {
        _forceMap.sensitivity = _sensitivity;
        _forceMap.power = _power;

        _forceMap.Update();
        _forceSum = _forceMap.forceSum;
        _forceAverage = _forceMap.forceAverage;

        // float interpolant = Mathf.Exp(-_decayTime * Time.deltaTime * 60);
        float interpolant = Mathf.Exp(-_decayTime);
        _smoothedForceAverage = Mathf.Lerp(_smoothedForceAverage, _forceAverage, interpolant);

        _convolutionFilter.SetFloat(_idInterpolant, interpolant);

        Graphics.Blit(_convolvedInput, _convolvedInput, _convolutionFilter, _smooth ? 1 : 2);
    }

    private void OnDestroy()
    {
        _forceMap.Dispose();
    }
}
