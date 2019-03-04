using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeRenderer : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    ComputeShader _volumeShader = null;

    [SerializeField]
    Mesh _cubeMesh = null;

    [SerializeField]
    Material _raymarchMaterial = null;

    [SerializeField]
    Vector3Int _resolution = new Vector3Int(128, 72, 64);

    [SerializeField]
    Bounds _bounds = new Bounds(Vector3.zero, new Vector3(12.8f, 20f, 7.2f));

    [SerializeField, MinMax(0, 10)]
    Vector2 _pressureRange = new Vector2(0, 3);

    [SerializeField]
    Vector2 _sweepSpeedRange = new Vector2(0.04f, 0.3f);

    [SerializeField]
    bool _debug = false;

    #endregion

    #region Fields

    RenderTexture _volume;
    public RenderTexture volumeTexture
    {
        get { return _volume; }
    }

    ForceMapManager _forceMap;

    float _zOffset = 0;
    MaterialPropertyBlock _propertyBlock;

    int _idInputTex;
    int _idVolumeTex;
    int _idVolumeTex_Size;
    int _idVolumeTex_InvSize;
    int _idZOffset;

    int _kernelInit;
    int _kernelCopyLayer;

    #endregion

    #region Unity events
        
    private void Awake()
    {
        _forceMap = ForceMapManager.Instance;
        _forceMap.update = false;

        _idInputTex = Shader.PropertyToID("_InputTex");
        _idVolumeTex = Shader.PropertyToID("_VolumeTex");
        _idVolumeTex_Size = Shader.PropertyToID("_VolumeTex_Size");
        _idVolumeTex_InvSize = Shader.PropertyToID("_VolumeTex_InvSize");
        _idZOffset = Shader.PropertyToID("_ZOffset");

        _kernelInit = _volumeShader.FindKernel("Init");
        _kernelCopyLayer = _volumeShader.FindKernel("CopyLayer");

        _propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        _volume = new RenderTexture(_resolution.x, _resolution.y, 0, RenderTextureFormat.RFloat);
        _volume.dimension = TextureDimension.Tex3D;
        _volume.volumeDepth = _resolution.z;
        _volume.filterMode = FilterMode.Trilinear;
        _volume.wrapModeU = _volume.wrapModeV = TextureWrapMode.Clamp;
        _volume.wrapModeW = TextureWrapMode.Repeat;
        _volume.enableRandomWrite = true;
        _volume.Create();

        #if UNITY_EDITOR
        // SceneView.onSceneGUIDelegate += OnSceneGUI;
        #endif
    }

    private void OnDisable()
    {
        _volume.Release();

        #if UNITY_EDITOR
        // SceneView.onSceneGUIDelegate -= OnSceneGUI;
        #endif
    }

    private void Start()
    {
        const int groupSize = 8;

        int groupsX = (_volume.width + groupSize - 1) / groupSize;
        int groupsY = (_volume.height + groupSize - 1) / groupSize;
        int groupsZ = (_volume.volumeDepth + groupSize - 1) / groupSize;

        _volumeShader.SetInts(_idVolumeTex_Size, _volume.width, _volume.height, _volume.volumeDepth);

        _volumeShader.SetTexture(_kernelInit, _idVolumeTex, _volume);
        _volumeShader.Dispatch(_kernelInit, groupsX, groupsY, groupsZ);

        _volumeShader.SetTexture(_kernelCopyLayer, _idInputTex, _forceMap.ConvolvedInputTexture);
        _volumeShader.SetTexture(_kernelCopyLayer, _idVolumeTex, _volume);

        _propertyBlock.SetTexture(_idVolumeTex, _volume);
        _propertyBlock.SetVector(_idVolumeTex_Size, new Vector3(_volume.width, _volume.height, _volume.volumeDepth));
        _propertyBlock.SetVector(_idVolumeTex_InvSize, new Vector3(1f / _volume.width, 1f / _volume.height, 1f / _volume.volumeDepth));
    }

    private void Update()
    {
        _forceMap.UpdateForce();

        {
            // read input tex into current layer volume tex, denoted by _ZOffset

            _volumeShader.SetFloat(_idZOffset, _zOffset);

            const int groupSize = 8;

            int groupsX = (_volume.width + groupSize - 1) / groupSize;
            int groupsY = (_volume.height + groupSize - 1) / groupSize;
            int groupsZ = 1;

            _volumeShader.Dispatch(_kernelCopyLayer, groupsX, groupsY, groupsZ);
        }
        _propertyBlock.SetFloat(_idZOffset, _zOffset);

        var lerp = Mathf.InverseLerp(_pressureRange.x, _pressureRange.y, _forceMap.smoothedForceAverage);
        var speed = Mathf.Lerp(_sweepSpeedRange.x, _sweepSpeedRange.y, lerp);

        // update zoffset by speed
        _zOffset = Mathf.Repeat(_zOffset + speed * Time.deltaTime, 1f);

        // render raymarched cube
        var mat = transform.localToWorldMatrix * Matrix4x4.TRS(_bounds.center, Quaternion.identity, _bounds.size);
        Graphics.DrawMesh(_cubeMesh, mat, _raymarchMaterial, 0, null, 0, _propertyBlock, false, false);
    }

    private void OnDrawGizmos()
    {
        if (_debug) DrawGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        var matrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(_bounds.center, _bounds.size);
        Gizmos.matrix = matrix;
    }

    #endregion
}
