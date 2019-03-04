using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Klak.Sensel;

public class CameraController : MonoBehaviour
{
    #region Serialized fields

    [SerializeField, Range(0, 30)]
    float _lerpSpeed = 10f;
        
    #endregion

    #region Private fields

    #endregion

    #region Unity events

    private void Update()
    {
        var accelData = SenselMaster.Acceleration;
        var accel = new Vector3((int)accelData.x, (int)accelData.y, (int)accelData.z);

        if (accel == Vector3.zero) return;

        accel = accel.normalized;
        accel = Quaternion.Euler(90, 0, 0) * accel;

        // var angles = (Quaternion.Euler(90, 0, 0) * Quaternion.LookRotation(accel, Vector3.up)).eulerAngles;
        // var xAngleTarget = angles.x;
        // xAngleTarget = Mathf.Clamp(xAngleTarget, -80, 90);

        // _xAngle = Mathf.SmoothDamp(_xAngle, xAngleTarget, ref _xVel, _damp, 90f);

        var targetRot = Quaternion.FromToRotation(Vector3.forward, accel);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _lerpSpeed * Time.deltaTime);

        // transform.rotation = Quaternion.FromToRotation(Vector3.forward, accel);
        // transform.rotation = Quaternion.Euler(_xAngle, 0, 0);
    }
        
    #endregion
}
