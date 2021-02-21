/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace LeapInternal
{
  using System;
  using System.Collections.Generic;
  using System.Runtime.InteropServices;
  using System.Threading;

  using Leap;
  using MultiLeapWrapper;

  public class Connection
  {
    static Connection connection = null;

    Wrapper wrapper;
    bool isConnected = false;

    static Connection()
    {

    }

    public static Connection GetConnection()
    {
      if (connection == null)
      {
        connection = new Connection();
      }
      return connection;
    }

    public CircularObjectBuffer<Frame> Frames { get; set; }

    private DeviceList _devices = new DeviceList();
    private FailedDeviceList _failedDevices;

    private DistortionData _currentLeftDistortionData = new DistortionData();
    private DistortionData _currentRightDistortionData = new DistortionData();
    private int _frameBufferLength = 60; //TODO, surface this value in LeapC,
                                         //currently hardcoded!

    //Policy and enabled features
    private UInt64 _requestedPolicies = 0;
    private UInt64 _activePolicies = 0;

    //Config change status
    private Dictionary<uint, string> _configRequests =
      new Dictionary<uint, string>();

    //Connection events
    public SynchronizationContext EventContext { get; set; }

    private EventHandler<LeapEventArgs> _leapInit;
    public event EventHandler<LeapEventArgs> LeapInit
    {
      add
      {
        _leapInit += value;
      }
      remove { _leapInit -= value; }
    }

    private EventHandler<ConnectionEventArgs> _leapConnectionEvent;
    public event EventHandler<ConnectionEventArgs> LeapConnection
    {
      add
      {
        _leapConnectionEvent += value;
        if (IsServiceConnected)
          value(this, new ConnectionEventArgs());
      }
      remove { _leapConnectionEvent -= value; }
    }
    public EventHandler<ConnectionLostEventArgs> LeapConnectionLost;
    public EventHandler<DeviceEventArgs> LeapDevice;
    public EventHandler<DeviceEventArgs> LeapDeviceLost;
    public EventHandler<DeviceFailureEventArgs> LeapDeviceFailure;
    public EventHandler<PolicyEventArgs> LeapPolicyChange;
    public EventHandler<FrameEventArgs> LeapFrame;
    public EventHandler<InternalFrameEventArgs> LeapInternalFrame;
    public EventHandler<LogEventArgs> LeapLogEvent;
    public EventHandler<SetConfigResponseEventArgs> LeapConfigResponse;
    public EventHandler<ConfigChangeEventArgs> LeapConfigChange;
    public EventHandler<DistortionEventArgs> LeapDistortionChange;
    public EventHandler<DroppedFrameEventArgs> LeapDroppedFrame;
    public EventHandler<ImageEventArgs> LeapImage;
    public EventHandler<PointMappingChangeEventArgs> LeapPointMappingChange;
    public EventHandler<HeadPoseEventArgs> LeapHeadPoseChange;

    public Action<BeginProfilingForThreadArgs> LeapBeginProfilingForThread;
    public Action<EndProfilingForThreadArgs> LeapEndProfilingForThread;
    public Action<BeginProfilingBlockArgs> LeapBeginProfilingBlock;
    public Action<EndProfilingBlockArgs> LeapEndProfilingBlock;

    private bool _disposed = false;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
      if (_disposed)
        return;

      if (disposing)
      {
      }

      Stop();

      _disposed = true;
    }

    ~Connection()
    {
      Dispose(false);
    }

    private Connection()
    {
      Frames = new CircularObjectBuffer<Frame>(_frameBufferLength);
    }

    public void Start()
    {
      if (!isConnected)
      {
        try
        {
          wrapper = new Wrapper(OnConnection, OnConnectionLost, OnDevice, OnLostDevice,
            OnFailedDevice, OnFrame, OnLogMessage, OnCalibrationSample);
          UnityEngine.Debug.Log("MultiLeap initialization successful.");
          isConnected = true;
          Thread.Sleep(2000);
          int a = Wrapper.Test();
          UnityEngine.Debug.Log(Wrapper.Test());
          Device[] devices = null;
          int[] ids = null;
          wrapper.GetDevices(ref devices, ref ids);
          UnityEngine.Debug.Log("Found " + devices.Length + " devices.");
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
          return;
        }
        AppDomain.CurrentDomain.DomainUnload += (arg1, arg2) => Dispose(true);
      }
    }

    public void Stop()
    {
      wrapper.Dispose();
      isConnected = false;
    }

    private void OnFrame(Frame frame)
    {
      Frames.Put(ref frame);

      if (LeapFrame != null)
      {
        LeapFrame.DispatchOnContext(this, EventContext, new FrameEventArgs(frame));
      }
    }

    public UInt64 GetInterpolatedFrameSize(uint deviceId, Int64 time)
    {
      return (ulong)wrapper.GetInterpolatedFrameSize(deviceId, time);
    }

    public void GetInterpolatedFrame(uint deviceId, Frame toFill, Int64 time)
    {
      toFill.CopyFrom(wrapper.GetInterpolatedFrame(deviceId, time));
    }

    public void GetInterpolatedFrameFromTime(uint deviceId, Frame toFill, Int64 time,
                                             Int64 sourceTime)
    {
      toFill.CopyFrom(wrapper.GetInterpolatedFrameFromTime(deviceId, time, sourceTime));
    }

    public Frame GetInterpolatedFrame(uint deviceId, Int64 time)
    {
      Frame frame = new Frame();
      GetInterpolatedFrame(deviceId, frame, time);
      return frame;
    }

    public void GetInterpolatedHeadPose(ref LEAP_HEAD_POSE_EVENT toFill,
                                        Int64 time)
    {
      throw new NotImplementedException();

      /*eLeapRS result = LeapC.InterpolateHeadPose(_leapConnection, time, ref toFill);
      reportAbnormalResults("LeapC get interpolated head pose call was ", result);*/
    }

    public LEAP_HEAD_POSE_EVENT GetInterpolatedHeadPose(Int64 time)
    {
      LEAP_HEAD_POSE_EVENT headPoseEvent = new LEAP_HEAD_POSE_EVENT();
      GetInterpolatedHeadPose(ref headPoseEvent, time);
      return headPoseEvent;
    }

    public void GetInterpolatedEyePositions(ref LEAP_EYE_EVENT toFill, Int64 time)
    {
      throw new NotImplementedException();
      /*eLeapRS result = LeapC.InterpolateEyePositions(_leapConnection, time,
        ref toFill);
      reportAbnormalResults("LeapC get interpolated eye positions call was ",
        result);*/
    }

    public void GetInterpolatedLeftRightTransform(Int64 time,
                                                  Int64 sourceTime,
                                                  Int64 leftId,
                                                  Int64 rightId,
                                              out LeapTransform leftTransform,
                                              out LeapTransform rightTransform)
    {
      throw new NotImplementedException();

      /*leftTransform = LeapTransform.Identity;
      rightTransform = LeapTransform.Identity;

      UInt64 size = GetInterpolatedFrameSize(time);
      IntPtr trackingBuffer = Marshal.AllocHGlobal((Int32)size);
      eLeapRS result = LeapC.InterpolateFrameFromTime(_leapConnection, time,
        sourceTime, trackingBuffer, size);
      reportAbnormalResults("LeapC get interpolated frame from time call was ",
        result);

      if (result == eLeapRS.eLeapRS_Success)
      {
        LEAP_TRACKING_EVENT tracking_evt;
        StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(trackingBuffer,
          out tracking_evt);

        int id;
        LEAP_VECTOR position;
        LEAP_QUATERNION orientation;

        long handPtr = tracking_evt.pHands.ToInt64();
        long idPtr = handPtr + _handIdOffset;
        long posPtr = handPtr + _handPositionOffset;
        long rotPtr = handPtr + _handOrientationOffset;
        int stride = StructMarshal<LEAP_HAND>.Size;

        for (uint i = tracking_evt.nHands; i-- != 0;
             idPtr += stride, posPtr += stride, rotPtr += stride)
        {
          id = Marshal.ReadInt32(new IntPtr(idPtr));
          StructMarshal<LEAP_VECTOR>.PtrToStruct(new IntPtr(posPtr),
            out position);
          StructMarshal<LEAP_QUATERNION>.PtrToStruct(new IntPtr(rotPtr),
            out orientation);

          LeapTransform transform = new LeapTransform(position.ToLeapVector(),
            orientation.ToLeapQuaternion());
          if (id == leftId)
          {
            leftTransform = transform;
          }
          else if (id == rightId)
          {
            rightTransform = transform;
          }
        }
      }

      Marshal.FreeHGlobal(trackingBuffer);*/
    }

    private void OnConnection()
    {
      if (_leapConnectionEvent != null)
      {
        _leapConnectionEvent.DispatchOnContext(this, EventContext,
          new ConnectionEventArgs());
      }
    }

    private void OnConnectionLost()
    {
      if (LeapConnectionLost != null)
      {
        LeapConnectionLost.DispatchOnContext(this, EventContext,
          new ConnectionLostEventArgs());
      }
    }

    private void OnDevice(Device device, uint id)
    {
      _devices.AddOrUpdate(id, device);

      if (LeapDevice != null)
      {
        LeapDevice.DispatchOnContext(this, EventContext,
          new DeviceEventArgs(device));
      }
    }

    private void OnLostDevice(string sn)
    {
      KeyValuePair<uint, Device>? lost = _devices.FindDeviceBySerialNumber(sn);
      if (lost != null)
      {
        _devices.Remove(lost.Value.Key);
        UnityEngine.Debug.Log("Lost a device.");

        if (LeapDeviceLost != null)
        {
          LeapDeviceLost.DispatchOnContext(this, EventContext,
            new DeviceEventArgs(lost.Value.Value));
        }
      }
    }

    private void OnFailedDevice(LEAP_DEVICE_FAILURE_EVENT deviceMsg)
    {
      string failureMessage;
      string failedSerialNumber = "Unavailable";
      switch (deviceMsg.status)
      {
        case eLeapDeviceStatus.eLeapDeviceStatus_BadCalibration:
          failureMessage = "Bad Calibration. Device failed because of a bad " +
            "calibration record.";
          break;
        case eLeapDeviceStatus.eLeapDeviceStatus_BadControl:
          failureMessage = "Bad Control Interface. Device failed because of a " +
            "USB control interface error.";
          break;
        case eLeapDeviceStatus.eLeapDeviceStatus_BadFirmware:
          failureMessage = "Bad Firmware. Device failed because of a firmware "
            + "error.";
          break;
        case eLeapDeviceStatus.eLeapDeviceStatus_BadTransport:
          failureMessage = "Bad Transport. Device failed because of a USB "
            + "communication error.";
          break;
        default:
          failureMessage = "Device failed for an unknown reason";
          break;
      }
      KeyValuePair<uint, Device>? failed = _devices.FindDeviceByHandle(deviceMsg.hDevice);
      if (failed.HasValue)
      {
        _devices.Remove(failed.Value.Key);
        UnityEngine.Debug.Log("Removed a failed device.");
      }

      if (LeapDeviceFailure != null)
      {
        LeapDeviceFailure.DispatchOnContext(this, EventContext,
          new DeviceFailureEventArgs((uint)deviceMsg.status, failureMessage,
            failedSerialNumber));
      }
    }

    private void OnConfigChange(ref LEAP_CONFIG_CHANGE_EVENT configEvent)
    {
      string config_key = "";
      _configRequests.TryGetValue(configEvent.requestId, out config_key);
      if (config_key != null)
        _configRequests.Remove(configEvent.requestId);
      if (LeapConfigChange != null)
      {
        LeapConfigChange.DispatchOnContext(this, EventContext,
          new ConfigChangeEventArgs(config_key, configEvent.status != false,
            configEvent.requestId));
      }
    }

    private void OnConfigResponse(ref LEAP_CONNECTION_MESSAGE configMsg)
    {
      LEAP_CONFIG_RESPONSE_EVENT config_response_evt;
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(
        configMsg.eventStructPtr, out config_response_evt);
      string config_key = "";
      _configRequests.TryGetValue(config_response_evt.requestId, out config_key);
      if (config_key != null)
        _configRequests.Remove(config_response_evt.requestId);

      Config.ValueType dataType;
      object value;
      uint requestId = config_response_evt.requestId;
      if (config_response_evt.value.type != eLeapValueType.eLeapValueType_String)
      {
        switch (config_response_evt.value.type)
        {
          case eLeapValueType.eLeapValueType_Boolean:
            dataType = Config.ValueType.TYPE_BOOLEAN;
            value = config_response_evt.value.boolValue;
            break;
          case eLeapValueType.eLeapValueType_Int32:
            dataType = Config.ValueType.TYPE_INT32;
            value = config_response_evt.value.intValue;
            break;
          case eLeapValueType.eLeapValueType_Float:
            dataType = Config.ValueType.TYPE_FLOAT;
            value = config_response_evt.value.floatValue;
            break;
          default:
            dataType = Config.ValueType.TYPE_UNKNOWN;
            value = new object();
            break;
        }
      }
      else
      {
        LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE config_ref_value;
        StructMarshal<LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE>.PtrToStruct(
          configMsg.eventStructPtr, out config_ref_value);
        dataType = Config.ValueType.TYPE_STRING;
        value = config_ref_value.value.stringValue;
      }
      SetConfigResponseEventArgs args = new SetConfigResponseEventArgs(
        config_key, dataType, value, requestId);

      if (LeapConfigResponse != null)
      {
        LeapConfigResponse.DispatchOnContext(this, EventContext, args);
      }
    }
    private void OnLogMessage(eLeapLogSeverity severity, long timestamp, string message)
    {
      {
        UnityEngine.Debug.Log("Log message " + message);
      }
      if (LeapLogEvent != null)
      {
        LeapLogEvent.DispatchOnContext(this, EventContext, new LogEventArgs(
          publicSeverity(severity), timestamp, message));
      }
    }

    private static void OnCalibrationSample(bool status, int sampleCount, string failedDevices)
    {
      if (status)
      {
        UnityEngine.Debug.Log("" + sampleCount + " samples taken.");
      }
      else
      {
        UnityEngine.Debug.Log("Failed to take samples. Devices \"" + failedDevices + "\" do not see the hand.");
      }
    }

    private MessageSeverity publicSeverity(eLeapLogSeverity leapCSeverity)
    {
      switch (leapCSeverity)
      {
        case eLeapLogSeverity.eLeapLogSeverity_Unknown:
          return MessageSeverity.MESSAGE_UNKNOWN;
        case eLeapLogSeverity.eLeapLogSeverity_Information:
          return MessageSeverity.MESSAGE_INFORMATION;
        case eLeapLogSeverity.eLeapLogSeverity_Warning:
          return MessageSeverity.MESSAGE_WARNING;
        case eLeapLogSeverity.eLeapLogSeverity_Critical:
          return MessageSeverity.MESSAGE_CRITICAL;
        default:
          return MessageSeverity.MESSAGE_UNKNOWN;
      }
    }

    private void OnPointMappingChange(
                 ref LEAP_POINT_MAPPING_CHANGE_EVENT pointMapping)
    {
      if (LeapPointMappingChange != null)
      {
        LeapPointMappingChange.DispatchOnContext(this, EventContext,
          new PointMappingChangeEventArgs(pointMapping.frame_id,
            pointMapping.timestamp, pointMapping.nPoints));
      }
    }

    private void OnDroppedFrame(ref LEAP_DROPPED_FRAME_EVENT droppedFrame)
    {
      if (LeapDroppedFrame != null)
      {
        LeapDroppedFrame.DispatchOnContext(this, EventContext,
          new DroppedFrameEventArgs(droppedFrame.frame_id, droppedFrame.reason));
      }
    }

    private void OnHeadPoseChange(ref LEAP_HEAD_POSE_EVENT headPose)
    {
      if (LeapHeadPoseChange != null)
      {
        LeapHeadPoseChange.DispatchOnContext(this, EventContext,
          new HeadPoseEventArgs(headPose.head_position, headPose.head_orientation));
      }
    }

    private DistortionData createDistortionData(LEAP_IMAGE image,
                                                Image.CameraType camera)
    {
      DistortionData distortionData = new DistortionData();
      distortionData.Version = image.matrix_version;
      distortionData.Width = LeapC.DistortionSize; //fixed value for now
      distortionData.Height = LeapC.DistortionSize; //fixed value for now

      //Visit LeapC.h for more details.  We need to marshal the float data manually
      //since the distortion struct cannot be represented safely in c#
      distortionData.Data = new float[(int)(distortionData.Width *
        distortionData.Height * 2)]; //2 float values per map point
      Marshal.Copy(image.distortionMatrix, distortionData.Data, 0,
        distortionData.Data.Length);

      if (LeapDistortionChange != null)
      {
        LeapDistortionChange.DispatchOnContext(this, EventContext,
          new DistortionEventArgs(distortionData, camera));
      }
      return distortionData;
    }

    private void OnImage(ref LEAP_IMAGE_EVENT imageMsg, UInt32 deviceID)
    {
      if (LeapImage != null)
      {
        //Update distortion data, if changed
        if ((_currentLeftDistortionData.Version !=
              imageMsg.leftImage.matrix_version) ||
            !_currentLeftDistortionData.IsValid)
        {
          _currentLeftDistortionData = createDistortionData(imageMsg.leftImage,
            Image.CameraType.LEFT);
        }
        if ((_currentRightDistortionData.Version !=
              imageMsg.rightImage.matrix_version) ||
            !_currentRightDistortionData.IsValid)
        {
          _currentRightDistortionData = createDistortionData(imageMsg.rightImage,
            Image.CameraType.RIGHT);
        }
        ImageData leftImage = new ImageData(Image.CameraType.LEFT,
          imageMsg.leftImage, _currentLeftDistortionData);
        ImageData rightImage = new ImageData(Image.CameraType.RIGHT,
          imageMsg.rightImage, _currentRightDistortionData);
        Image stereoImage = new Image(
          imageMsg.info.frame_id,
          imageMsg.info.timestamp,
          leftImage,
          rightImage,
          deviceID);
        LeapImage.DispatchOnContext(this, EventContext,
          new ImageEventArgs(stereoImage));
      }
    }

    private void OnPolicyChange(ref LEAP_POLICY_EVENT policyMsg)
    {
      if (LeapPolicyChange != null)
      {
        LeapPolicyChange.DispatchOnContext(this, EventContext,
          new PolicyEventArgs(policyMsg.current_policy, _activePolicies));
      }

      _activePolicies = policyMsg.current_policy;

      if (_activePolicies != _requestedPolicies)
      {
        // This could happen when config is turned off, or
        // this is the policy change event from the last SetPolicy, after that, the user called SetPolicy again
        //TODO handle failure to set desired policy -- maybe a PolicyDenied event
      }
    }

    public void SetPolicy(Controller.PolicyFlag policy)
    {
      throw new NotImplementedException();
      /*UInt64 setFlags = (ulong)flagForPolicy(policy);
      _requestedPolicies = _requestedPolicies | setFlags;
      setFlags = _requestedPolicies;
      UInt64 clearFlags = ~_requestedPolicies; //inverse of desired policies

      eLeapRS result = LeapC.SetPolicyFlags(_leapConnection, setFlags,
        clearFlags);
      reportAbnormalResults("LeapC SetPolicyFlags call was ", result);*/
    }

    public void ClearPolicy(Controller.PolicyFlag policy)
    {
      throw new NotImplementedException();

      /* UInt64 clearFlags = (ulong)flagForPolicy(policy);
       _requestedPolicies = _requestedPolicies & ~clearFlags;
       eLeapRS result = LeapC.SetPolicyFlags(_leapConnection, 0, clearFlags);
       reportAbnormalResults("LeapC SetPolicyFlags call was ", result);*/
    }

    private eLeapPolicyFlag flagForPolicy(Controller.PolicyFlag singlePolicy)
    {
      switch (singlePolicy)
      {
        case Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES:
          return eLeapPolicyFlag.eLeapPolicyFlag_BackgroundFrames;
        case Controller.PolicyFlag.POLICY_IMAGES:
          return eLeapPolicyFlag.eLeapPolicyFlag_Images;
        case Controller.PolicyFlag.POLICY_OPTIMIZE_HMD:
          return eLeapPolicyFlag.eLeapPolicyFlag_OptimizeHMD;
        case Controller.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME:
          return eLeapPolicyFlag.eLeapPolicyFlag_AllowPauseResume;
        case Controller.PolicyFlag.POLICY_MAP_POINTS:
          return eLeapPolicyFlag.eLeapPolicyFlag_MapPoints;
        case Controller.PolicyFlag.POLICY_DEFAULT:
          return 0;
        default:
          return 0;
      }
    }

    /// <summary>
    /// Gets the active setting for a specific policy.
    ///
    /// Keep in mind that setting a policy flag is asynchronous, so changes are
    /// not effective immediately after calling setPolicyFlag(). In addition, a
    /// policy request can be declined by the user. You should always set the
    /// policy flags required by your application at startup and check that the
    /// policy change request was successful after an appropriate interval.
    ///
    /// If the controller object is not connected to the Leap Motion software, then the default
    /// state for the selected policy is returned.
    ///
    ///
    /// @since 2.1.6
    /// </summary>
    public bool IsPolicySet(Controller.PolicyFlag policy)
    {
      UInt64 policyToCheck = (ulong)flagForPolicy(policy);
      return (_activePolicies & policyToCheck) == policyToCheck;
    }

    public uint GetConfigValue(string config_key)
    {
      throw new NotImplementedException();
      /*
      uint requestId = 0;
      eLeapRS result = LeapC.RequestConfigValue(_leapConnection, config_key,
        out requestId);
      reportAbnormalResults("LeapC RequestConfigValue call was ", result);
      _configRequests[requestId] = config_key;
      return requestId;*/
    }

    public uint SetConfigValue<T>(string config_key, T value) where T : IConvertible
    {
      throw new NotImplementedException();

      /*uint requestId = 0;
      eLeapRS result;
      Type dataType = value.GetType();
      if (dataType == typeof(bool))
      {
        result = LeapC.SaveConfigValue(_leapConnection, config_key,
          Convert.ToBoolean(value), out requestId);
      }
      else if (dataType == typeof(Int32))
      {
        result = LeapC.SaveConfigValue(_leapConnection, config_key,
          Convert.ToInt32(value), out requestId);
      }
      else if (dataType == typeof(float))
      {
        result = LeapC.SaveConfigValue(_leapConnection, config_key,
          Convert.ToSingle(value), out requestId);
      }
      else if (dataType == typeof(string))
      {
        result = LeapC.SaveConfigValue(_leapConnection, config_key,
          Convert.ToString(value), out requestId);
      }
      else
      {
        throw new ArgumentException("Only boolean, Int32, float, and string " +
          "types are supported.");
      }
      reportAbnormalResults("LeapC SaveConfigValue call was ", result);
      _configRequests[requestId] = config_key;
      return requestId;*/
    }

    /// <summary>
    /// Reports whether your application has a connection to the Leap Motion
    /// daemon/service. Can be true even if the Leap Motion hardware is not
    /// available.
    /// @since 1.2
    /// </summary>
    public bool IsServiceConnected
    {
      get
      {
        return isConnected;
      }
    }

    /// <summary>
    /// The list of currently attached and recognized Leap Motion controller
    /// devices.
    ///
    /// The Device objects in the list describe information such as the range and
    /// tracking volume.
    ///
    /// Currently, the Leap Motion Controller only allows a single active device
    /// at a time, however there may be multiple devices physically attached and
    /// listed here.  Any active device(s) are guaranteed to be listed first,
    /// however order is not determined beyond that.
    ///
    /// @since 1.0
    /// </summary>
    public DeviceList Devices
    {
      get
      {
        if (_devices == null)
        {
          _devices = new DeviceList();
        }

        return _devices;
      }
    }

    public FailedDeviceList FailedDevices
    {
      get
      {
        if (_failedDevices == null)
        {
          _failedDevices = new FailedDeviceList();
        }

        return _failedDevices;
      }
    }


    /// <summary>
    /// Subscribes to the events coming from an individual device
    /// 
    /// If this is not called, only the primary device will be subscribed.
    /// Will automatically unsubscribe the primary device if this is called 
    /// on a secondary device, but not a primary one.  
    /// 
    /// @since 4.1
    /// </summary>
    public void SubscribeToDeviceEvents(uint deviceId)
    {
      wrapper.SetDeviceStatus(deviceId, true);
    }

    /// <summary>
    /// Unsubscribes from the events coming from an individual device
    /// 
    /// This can be called safely, even if the device has not been subscribed.
    /// 
    /// @since 4.1
    /// </summary>
    public void UnsubscribeFromDeviceEvents(uint deviceId)
    {
      wrapper.SetDeviceStatus(deviceId, false);
    }

    public Device GetDevice(uint deviceId)
    {
      Device device = null;
      wrapper.GetDevice(deviceId, ref device);
      return device;
    }

    public uint GetDeviceId(string sn)
    {
      KeyValuePair<uint, Device>? device = _devices.FindDeviceBySerialNumber(sn);
      return device.HasValue ? device.Value.Key : 0;
    }

    /// <summary>
    /// Converts from image-space pixel coordinates to camera-space rectilinear coordinates
    /// </summary>
    public Vector PixelToRectilinear(Image.CameraType camera, Vector pixel)
    {
      throw new NotImplementedException();
      /*LEAP_VECTOR pixelStruct = new LEAP_VECTOR(pixel);
      LEAP_VECTOR ray = LeapC.LeapPixelToRectilinear(_leapConnection,
             (camera == Image.CameraType.LEFT ?
             eLeapPerspectiveType.eLeapPerspectiveType_stereo_left :
             eLeapPerspectiveType.eLeapPerspectiveType_stereo_right),
             pixelStruct);
      return new Vector(ray.x, ray.y, ray.z);*/
    }

    /// <summary>
    /// Converts from image-space pixel coordinates to camera-space rectilinear coordinates
    /// 
    /// Also allows specifying a specific device handle and calibration type.
    /// 
    /// @since 4.1
    /// </summary>
    public Vector PixelToRectilinearEx(IntPtr deviceHandle,
                                       Image.CameraType camera, Image.CalibrationType calibType, Vector pixel)
    {
      throw new NotImplementedException();
      /*LEAP_VECTOR pixelStruct = new LEAP_VECTOR(pixel);
      LEAP_VECTOR ray = LeapC.LeapPixelToRectilinearEx(_leapConnection,
             deviceHandle,
             (camera == Image.CameraType.LEFT ?
             eLeapPerspectiveType.eLeapPerspectiveType_stereo_left :
             eLeapPerspectiveType.eLeapPerspectiveType_stereo_right),
             (calibType == Image.CalibrationType.INFRARED ?
             eLeapCameraCalibrationType.eLeapCameraCalibrationType_infrared :
             eLeapCameraCalibrationType.eLeapCameraCalibrationType_visual),
             pixelStruct);
      return new Vector(ray.x, ray.y, ray.z);*/
    }

    /// <summary>
    /// Converts from camera-space rectilinear coordinates to image-space pixel coordinates
    /// </summary>
    public Vector RectilinearToPixel(Image.CameraType camera, Vector ray)
    {
      throw new NotImplementedException();
      /*LEAP_VECTOR rayStruct = new LEAP_VECTOR(ray);
      LEAP_VECTOR pixel = LeapC.LeapRectilinearToPixel(_leapConnection,
             (camera == Image.CameraType.LEFT ?
             eLeapPerspectiveType.eLeapPerspectiveType_stereo_left :
             eLeapPerspectiveType.eLeapPerspectiveType_stereo_right),
             rayStruct);
      return new Vector(pixel.x, pixel.y, pixel.z);*/
    }

    public void TelemetryProfiling(ref LEAP_TELEMETRY_DATA telemetryData)
    {
      throw new NotImplementedException();
      /*eLeapRS result = LeapC.LeapTelemetryProfiling(_leapConnection, ref telemetryData);
      reportAbnormalResults("LeapC TelemetryProfiling call was ", result);*/
    }

    public void GetPointMapping(ref PointMapping pm)
    {
      throw new NotImplementedException();
      /*UInt64 size = 0;
      IntPtr buffer = IntPtr.Zero;
      while (true)
      {
        eLeapRS result = LeapC.GetPointMapping(_leapConnection, buffer, ref size);
        if (result == eLeapRS.eLeapRS_InsufficientBuffer)
        {
          if (buffer != IntPtr.Zero)
            Marshal.FreeHGlobal(buffer);
          buffer = Marshal.AllocHGlobal((Int32)size);
          continue;
        }
        reportAbnormalResults("LeapC get point mapping call was ", result);
        if (result != eLeapRS.eLeapRS_Success)
        {
          pm.points = null;
          pm.ids = null;
          return;
        }
        break;
      }
      LEAP_POINT_MAPPING pmi;
      StructMarshal<LEAP_POINT_MAPPING>.PtrToStruct(buffer, out pmi);
      Int32 nPoints = (Int32)pmi.nPoints;

      pm.frameId = pmi.frame_id;
      pm.timestamp = pmi.timestamp;
      pm.points = new Vector[nPoints];
      pm.ids = new UInt32[nPoints];

      float[] points = new float[3 * nPoints];
      Int32[] ids = new Int32[nPoints];
      Marshal.Copy(pmi.points, points, 0, 3 * nPoints);
      Marshal.Copy(pmi.ids, ids, 0, nPoints);

      int j = 0;
      for (int i = 0; i < nPoints; i++)
      {
        pm.points[i].x = points[j++];
        pm.points[i].y = points[j++];
        pm.points[i].z = points[j++];
        pm.ids[i] = unchecked((UInt32)ids[i]);
      }
      Marshal.FreeHGlobal(buffer);*/
    }

    private eLeapRS _lastResult; //Used to avoid repeating the same log message, ie. for events like time out
    private void reportAbnormalResults(string context, eLeapRS result)
    {
      if (result != eLeapRS.eLeapRS_Success &&
         result != _lastResult)
      {
        string msg = context + " " + result;
        if (LeapLogEvent != null)
        {
          LeapLogEvent.DispatchOnContext(this, EventContext,
            new LogEventArgs(MessageSeverity.MESSAGE_CRITICAL,
                LeapC.GetNow(),
                msg));
        }
      }
      _lastResult = result;
    }
  }
}
