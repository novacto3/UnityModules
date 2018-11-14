using LeapInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;


public class TestGetNow : MonoBehaviour {

  private IntPtr _hConnection = IntPtr.Zero;
  private eLeapRS _result = eLeapRS.eLeapRS_Success;
  
  private void OnEnable() {
    Debug.Log("Testing get now...");
    Debug.Log("Now is " + LeapC.GetNow());

    Debug.Log("Creating connection...");
    //LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
    _result = LeapC.CreateConnection(out _hConnection);
    if (checkResult("CreateConnection")) {
      Debug.Log("CreateConnection, _hConnection: " + _hConnection);
    }
    
    tryOpenConnection();

    var isConnected = getIsConnected();
    Debug.Log("On start, isConnected: " + isConnected);

    // LEAP_DEVICE_REF[] devices = new LEAP_DEVICE_REF[16];
    // uint numDevices;
    // _result = LeapC.GetDeviceList(_hConnection, devices, out numDevices);
    // checkResult("GetDeviceList");
    // for (int i = 0; i < numDevices; i++) {
    //   var device = devices[i];
    //   Debug.Log("Device: " + device.id + " at handle " + device.handle);
    // }

    // Leap.Frame toFill = new Leap.Frame();
    // if (!interpolateFrame(LeapC.GetNow(), ref toFill)) {
    //   _result = LeapC.OpenConnection(_hConnection);
    //   checkResult("OpenConnection");
    // }
    // else {
    //   Debug.Log("Hand count: " + toFill.Hands.Count);
    // }

    // _result = LeapC.CloseConnection(_hConnection);
    // if (checkResult("CloseConnection")) {
    //   Debug.Log("Connection closed.");
    // }

    _pollThreadRunning = true;

    _pollThread = new Thread(new ThreadStart(this._pollUpdate));
    _pollThread.Name = "LeapC Poll Thread";
    _pollThread.IsBackground = true;
    _pollThread.Start();
  }

  private Thread _pollThread;
  private bool _pollThreadRunning = false;

  private void OnDisable() {
    _pollThreadRunning = false;
    _pollThread.Join();
  }

  private void _pollUpdate() {
    while (_pollThreadRunning) {
      Debug.Log("[PollThread] Hi from the poll thread");
      try {
        eLeapRS result;
        LEAP_CONNECTION_MESSAGE _msg = new LEAP_CONNECTION_MESSAGE();
        uint timeout = 1000;
        result = LeapC.PollConnection(_hConnection, timeout, ref _msg);
        checkResult(result, "PollConnection");
      }
      catch (Exception e) {
        Debug.LogError("[PollThread] Caught exception: " + e);
        _pollThreadRunning = false;
      }

      Thread.Sleep(15);
    }
    Debug.Log("[PollThread] Exiting.");
  }

  private float _timer = 0f;

  private void Update() {
    _timer += Time.deltaTime;
    if (_timer > 0.5f) {
      _timer = 0f;
      var isConnected = getIsConnected();
      if (!isConnected) {
        tryOpenConnection();
      }
    }
  }

  private bool tryOpenConnection() {
    _result = LeapC.OpenConnection(_hConnection);
    if (checkResult("OpenConnection")) {
      Debug.Log("OpenConnection, _hConnection: " + _hConnection);
      return true;
    }
    return false;
  }

  private bool getIsConnected() {
    var connectionInfo = new LEAP_CONNECTION_INFO();
    connectionInfo.size = (uint)Marshal.SizeOf(connectionInfo);
    _result = LeapC.GetConnectionInfo(_hConnection, ref connectionInfo);
    if (checkResult("getIsConnected, GetConnectionInfo")) {
      Debug.Log("[getIsConnected] Connection status: " + connectionInfo.status +
        " and size is " + connectionInfo.size);
    }
    return connectionInfo.status ==
      eLeapConnectionStatus.eLeapConnectionStatus_Connected;
  }

  private bool checkResult(string context) {
    if (_result != eLeapRS.eLeapRS_Success) {
      Debug.LogError(context + ": " + _result);
      return false;
    }
    return true;
  }

  private static bool checkResult(eLeapRS result, string context) {
    if (result != eLeapRS.eLeapRS_Success) {
      Debug.LogError(context + ": " + result);
      return false;
    }
    return true;
  }

  private bool interpolateFrame(long time, ref Leap.Frame toFill) {
    UInt64 size = getInterpolatedFrameSize(time);
    IntPtr trackingBuffer = Marshal.AllocHGlobal((Int32)size);
    _result = LeapC.InterpolateFrame(_hConnection, time,
      trackingBuffer, size);
    bool ok = checkResult("interpolateFrame");
    if (_result == eLeapRS.eLeapRS_Success) {
      LEAP_TRACKING_EVENT tracking_evt;
      StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(trackingBuffer,
        out tracking_evt);
      toFill.CopyFrom(ref tracking_evt);
    }
    else {
      UnityEngine.Debug.LogWarning("GetInterpolatedFrame failed with code " +
        _result);
    }
    Marshal.FreeHGlobal(trackingBuffer);
    return ok;
  }

  private UInt64 getInterpolatedFrameSize(Int64 time) {
    UInt64 size = 0;
    _result = LeapC.GetFrameSize(_hConnection, time, out size);
    checkResult("getInterpolatedFrameSize");
    return size;
  }

}
