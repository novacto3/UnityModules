/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap {
  using System;
  using System.Collections.Generic;

  /// <summary>
  /// The DeviceList class represents a list of Device objects.
  /// 
  /// Get a DeviceList object by calling Controller.Devices().
  /// @since 1.0
  /// </summary>
  public class DeviceList :
    Dictionary<uint, Device> {

    /// <summary>
    /// Constructs an empty list of devices.
    /// @since 1.0
    /// </summary>
    public DeviceList() { }

    /// <summary>
    /// For internal use only.
    /// </summary>
    public KeyValuePair<uint, Device>? FindDeviceByHandle(IntPtr handle)
    {
      foreach (KeyValuePair<uint, Device> device in this)
      {
        if (device.Value.Handle == handle)
          return device;
      }
      return null;
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    public KeyValuePair<uint, Device>? FindDeviceBySerialNumber(string serialNumber) {
      foreach(KeyValuePair<uint,Device> device in this)
      {
        if (device.Value.SerialNumber == serialNumber)
          return device;
      }
      return null;
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    public Device FindDeviceById(uint deviceId)
    {
      if (ContainsKey(deviceId))
      {
        return this[deviceId];
      }
      return null;
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    public void AddOrUpdate(uint id, Device device) {
      Device existingDevice = FindDeviceById(id);
      if (existingDevice != null) {
        existingDevice.Update(device);
      } else {
        Add(id, device);
      }
    }

    /// <summary>
    /// Reports whether the list is empty.
    /// @since 1.0
    /// </summary>
    public bool IsEmpty {
      get { return Count == 0; }
    }
  }
}
