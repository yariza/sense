using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeRenderer : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    Vector3 _resolution = new Vector3(128, 72, 64);
        
    #endregion

    #region Fields

    RenderTexture _volume;
    public RenderTexture volumeTexture
    {
        get { return _volume; }
    }

    ForceMapManager _forceMap;
        
    #endregion

    #region Unity events
        
    private void Awake()
    {
        _forceMap = ForceMapManager.Instance;
        _forceMap.update = false;
    }

    private void Update()
    {
        _forceMap.UpdateForce();

        
    }

    #endregion
}
