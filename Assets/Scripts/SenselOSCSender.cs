using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using OscJack;
using Klak.Sensel;

public class SenselOSCSender : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    OSC _osc;

    // [SerializeField]
    // string _ipAddress = "127.0.0.1";

    // [SerializeField]
    // int _udpPort = 9000;

    [SerializeField]
    string _oscAddress = "/unity";

    [SerializeField, Range(0, 1)]
    float _sensitivity = 0.2f;

    [SerializeField, Range(0.01f, 5f)]
    float _power = 1f;

    [SerializeField, Range(0.01f, 0.1f)]
    float _sendInterval = 0.05f;

    #endregion

    #region Private fields

    Thread _oscThread;

    // OscClient _client;
    float[] _left;
    float[] _right;
    bool _stop;

    #endregion

    #region Unity events

    private void Start()
    {
        // UpdateSettings();
        _oscThread = new Thread(SendThread);
        _oscThread.Start();
    }

    private void Update()
    {
    }

    private void OnDestroy()
    {
        if (_oscThread != null)
        {
            _stop = true;
            _oscThread.Join();
            _oscThread = null;
        }
    }

    OscMessage _message = new OscMessage();

    private void SendThread()
    {
        while (!_stop)
        {
            if (!SenselMaster.IsAvailable) return;

            int cols = SenselMaster.SensorInfo.num_cols;
            int rows = SenselMaster.SensorInfo.num_rows;

            if (_left == null || _left.Length != rows)
            {
                _left = new float[rows];
                _right = new float[rows];
            }
            for (int i = 0; i < rows; i++)
            {
                _left[i] = 0f;
                _right[i] = 0f;
            }
            int index = 0;
            var force = SenselMaster.ForceArray;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    float data = force[index];
                    data = Mathf.Pow(data * _sensitivity, _power);
                    float pan = Mathf.InverseLerp(0, cols - 1, j);
                    _left[rows - i - 1] += data * (1 - pan);
                    _right[rows - i - 1] += data * pan;

                    index++;
                }
            }

            _message.address = _oscAddress + "/left";
            _message.values.Capacity = rows;
            _message.values.Clear();
            _message.values.AddRange(_left);
            _osc.Send(_message);
            // _client.Send(_oscAddress, _data);

            _message.address = _oscAddress + "/right";
            _message.values.Clear();
            _message.values.AddRange(_right);
            _osc.Send(_message);

            Thread.Sleep((int)(_sendInterval * 1000));
        }
    }

    #endregion

    #region Private methods

    void UpdateSettings()
    {
        // _client = OscMaster.GetSharedClient(_ipAddress, _udpPort);
    }
        
    #endregion
}
