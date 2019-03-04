using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleRenderer : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    ComputeShader _particleKernel = null;

    [SerializeField]
    Material _particleMaterial = null;

    [SerializeField, Range(0, 1000000)]
    int _maxParticles = 1000;

    [SerializeField, MinMax(0, 60)]
    Vector2 _lifeRange = new Vector2(1, 5);

    [SerializeField, Range(0, 1)]
    float _speed = 1f;

    [SerializeField]
    float _scale = 1f;

    [SerializeField, Range(0,1)]
    float _throttle = 1f;

    [SerializeField]
    int _randomSeed = 0;

    #endregion

    #region Fields

    ForceMapManager _forceMap;

    RenderTexture _velocity;
    ComputeBuffer _positionBuffer;

    int _idPositionBuffer;
    int _idRWVelocityTex;
    int _idVelocityTex;
    int _idInputTex;

    int _idMaxParticles;
    int _idLifeParams;
    int _idResolution;
    int _idBounds;
    int _idSpeed;
    int _idConfig;

    int _kernelInitParticle;
    int _kernelUpdateParticle;
    int _kernelComputeVelocity;

    const int width = 128;
    const int height = 72;
    const float aspect = ((float)width) / height;

    #endregion

    #region Unity events

    private void Awake()
    {
        _forceMap = ForceMapManager.Instance;
        _forceMap.update = false;

        _idPositionBuffer = Shader.PropertyToID("_PositionBuffer");
        _idRWVelocityTex = Shader.PropertyToID("_RWVelocityTex");
        _idVelocityTex = Shader.PropertyToID("_VelocityTex");
        _idInputTex = Shader.PropertyToID("_InputTex");

        _idMaxParticles = Shader.PropertyToID("_MaxParticles");
        _idLifeParams = Shader.PropertyToID("_LifeParams");
        _idResolution = Shader.PropertyToID("_Resolution");
        _idBounds = Shader.PropertyToID("_Bounds");
        _idSpeed = Shader.PropertyToID("_Speed");
        _idConfig = Shader.PropertyToID("_Config");

        _kernelInitParticle = _particleKernel.FindKernel("InitParticle");
        _kernelUpdateParticle = _particleKernel.FindKernel("UpdateParticle");
        _kernelComputeVelocity = _particleKernel.FindKernel("ComputeVelocity");
    }

    private void OnEnable()
    {
        _velocity = new RenderTexture(width/2, height/2, 0, RenderTextureFormat.RGFloat);
        _velocity.enableRandomWrite = true;
        _velocity.wrapMode = TextureWrapMode.Repeat;
        _velocity.filterMode = FilterMode.Trilinear;
        _velocity.Create();
        _positionBuffer = new ComputeBuffer(_maxParticles, sizeof(float) * 4);
    }

    private void OnDisable()
    {
        _velocity.Release();
        _positionBuffer.Release();
    }

    private void Start()
    {
        const int groupSize = 512;
        int groupsX = (_positionBuffer.count + groupSize - 1) / groupSize;

        var bounds = new Vector4(aspect * _scale, _scale, 1f / aspect / _scale, 1f / _scale);

        _particleKernel.SetInt(_idMaxParticles, _maxParticles);
        _particleKernel.SetInts(_idResolution, width, height);
        _particleKernel.SetVector(_idBounds, bounds);
        _particleKernel.SetVector(_idConfig, new Vector4(_throttle, _randomSeed, Time.deltaTime, Time.time));

        _particleKernel.SetBuffer(_kernelInitParticle, _idPositionBuffer, _positionBuffer);
        _particleKernel.Dispatch(_kernelInitParticle, groupsX, 1, 1);

        _particleKernel.SetTexture(_kernelComputeVelocity, _idRWVelocityTex, _velocity);
        _particleKernel.SetTexture(_kernelComputeVelocity, _idInputTex, _forceMap.ConvolvedInputTexture);

        _particleKernel.SetBuffer(_kernelUpdateParticle, _idPositionBuffer, _positionBuffer);
        _particleKernel.SetTexture(_kernelUpdateParticle, _idVelocityTex, _velocity);

        _particleMaterial.SetBuffer(_idPositionBuffer, _positionBuffer);
        _particleMaterial.SetTexture(_idVelocityTex, _velocity);
        _particleMaterial.SetVector(_idBounds, bounds);
    }

    private void OnRenderObject()
    {
        _particleMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, 3, _maxParticles);
        // Graphics.DrawProcedural(MeshTopology.Points, 1, _maxParticles);
    }

    private void Update()
    {
        _forceMap.UpdateForce();

        // compute velocity
        {
            const int groupDimX = 8;
            const int groupDimY = 8;

            int groupsX = (_velocity.width + groupDimX - 1) / groupDimX;
            int groupsY = (_velocity.height + groupDimY - 1) / groupDimY;

            _particleKernel.Dispatch(_kernelComputeVelocity, groupsX, groupsY, 1);
        }

        // update particle
        {
            const int groupSize = 512;

            int groupsX = (_positionBuffer.count + groupSize - 1) / groupSize;

            _particleKernel.SetFloat(_idSpeed, _speed);
            _particleKernel.SetVector(_idConfig, new Vector4(_throttle, _randomSeed, Time.deltaTime, Time.time));
            var invLifeMax = 1.0f / Mathf.Max(_lifeRange.y, 0.01f);
            var invLifeMin = 1.0f / Mathf.Max(_lifeRange.x, 0.01f);
            _particleKernel.SetVector(_idLifeParams, new Vector2(invLifeMin, invLifeMax));
            _particleKernel.Dispatch(_kernelUpdateParticle, groupsX, 1, 1);
        }
    }

    #endregion
}
