using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [SerializeField, Range(0, 10)]
    float _decayTime = 1;

    #endregion

    #region Other Fields
        
    ForceMap _forceMap;
    public ForceMap forceMap
    {
        get { return _forceMap; }
    }

    public Texture RawInputTexture {
        get { return _forceMap.RawInputTexture; }
    }

    public Texture FilteredInputTexture {
        get { return _forceMap.FilteredInputTexture; }
    }

    public Texture TotalInputTexture {
        get { return _forceMap.TotalInputTexture; }
    }

    RenderTexture _convolvedInput;
    public Texture ConvolvedInputTexture {
        get { return _convolvedInput; }
    }

    Material _convolutionFilter;
    int _idInterpolant;

    #endregion

    private void Awake()
    {
        _idInterpolant = Shader.PropertyToID("_Interpolant");

        _convolutionFilter = new Material(Shader.Find("Hidden/Sensel/Convolve"));

        _forceMap = new ForceMap();
        _convolvedInput = new RenderTexture(RawInputTexture.width, RawInputTexture.height, 0, RenderTextureFormat.RFloat);
        _convolvedInput.wrapMode = TextureWrapMode.Clamp;

        _convolutionFilter.SetTexture("_RawInputTex", FilteredInputTexture);
    }

    private void Start()
    {
        Graphics.Blit(_convolvedInput, _convolvedInput, _convolutionFilter, 0);
    }

    private void Update()
    {
        _forceMap.Update();

        _convolutionFilter.SetFloat(_idInterpolant, Mathf.Exp(-_decayTime));

        Graphics.Blit(_convolvedInput, _convolvedInput, _convolutionFilter, 1);
    }

    private void OnDestroy()
    {
        _forceMap.Dispose();
    }
}
