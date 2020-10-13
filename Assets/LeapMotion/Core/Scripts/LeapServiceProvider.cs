/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {
  using Attributes;

  /// <summary>
  /// The LeapServiceProvider provides tracked Leap Hand data and images from the device
  /// via the Leap service running on the client machine.
  /// </summary>
  public class LeapServiceProvider : BaseLeapServiceProvider {

    #region Inspector
    public enum MultipleDeviceMode {
      Disabled,
      All,
      Specific
    }

    [Tooltip("When set to `All`, provider will receive data from all connected devices.")]
    [EditTimeOnly]
    [SerializeField]
    protected MultipleDeviceMode _multipleDeviceMode = MultipleDeviceMode.Disabled;

    [Tooltip("When Multiple Device Mode is set to `Specific`, the provider will " +
      "receive data from only the devices that contain this in their serial number.  "+
      "If the serial number is unknown, simply specify which DeviceID to " +
      "sample from (0 is invalid, 1 and above are valid).")]
    [EditTimeOnly]
    [SerializeField]
    protected string _specificSerialNumber;


    #endregion


    #region Unity Events

    protected override void Awake() {
      base.Awake();
      useInterpolation = _multipleDeviceMode.Equals(MultipleDeviceMode.All) ?
        false : useInterpolation;
    }

    #endregion

    #region Public API


    /// <summary>
    /// Creates an instance of a Controller, initializing its policy flags and
    /// subscribing to its connection event.
    /// </summary>
    protected override void createController() {
      if (_leapController != null) {
        return;
      }

      _leapController = new Controller(_specificSerialNumber.GetHashCode(),  
        _multipleDeviceMode != MultipleDeviceMode.Disabled);
      _leapController.Device += (s, e) => {
        if (_onDeviceSafe != null) {
          _onDeviceSafe(e.Device);
        }
      };

      if (_multipleDeviceMode == MultipleDeviceMode.All) {
        _onDeviceSafe += (d) => {
          _leapController.SubscribeToDeviceEvents(d);
        };
      } else if (_multipleDeviceMode == MultipleDeviceMode.Specific) {
        _onDeviceSafe += (d) => {
          int DeviceID = 0;
          _numDevicesSeen++;
          if ((int.TryParse(_specificSerialNumber, out DeviceID) &&
              _numDevicesSeen == (uint)DeviceID) ||
             (_specificSerialNumber.Length > 1 &&
              d.SerialNumber.Contains(_specificSerialNumber))) {
            _leapController.SubscribeToDeviceEvents(d);
          }
        };
      }

      if (_leapController.IsConnected) {
        initializeFlags();
      } else {
        _leapController.Device += onHandControllerConnect;
      }

      if (_workerThreadProfiling) {
        //A controller will report profiling statistics for the duration of it's lifetime
        //so these events will never be unsubscribed from.
        _leapController.EndProfilingBlock += LeapProfiling.EndProfilingBlock;
        _leapController.BeginProfilingBlock += LeapProfiling.BeginProfilingBlock;

        _leapController.EndProfilingForThread += LeapProfiling.EndProfilingForThread;
        _leapController.BeginProfilingForThread += LeapProfiling.BeginProfilingForThread;
      }
    }

    #endregion

  }

}
