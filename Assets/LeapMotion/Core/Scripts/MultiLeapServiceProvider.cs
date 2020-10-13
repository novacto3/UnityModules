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
  using System.Collections.Generic;

  /// <summary>
  /// The LeapServiceProvider provides tracked Leap Hand data and images from the device
  /// via the Leap service running on the client machine.
  /// </summary>
  public class MultiLeapServiceProvider : BaseLeapServiceProvider {

    protected override void Update()
    {
      if (_workerThreadProfiling)
      {
        LeapProfiling.Update();
      }

      if (!checkConnectionIntegrity()) { return; }

#if UNITY_EDITOR
      if (UnityEditor.EditorApplication.isCompiling)
      {
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.LogWarning("Unity hot reloading not currently supported. Stopping Editor Playback.");
        return;
      }
#endif

      _fixedOffset.Update(Time.time - Time.fixedTime, Time.deltaTime);

      if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate)
      {
        DispatchUpdateFrameEvent(_transformedFixedFrame);
        return;
      }

      _leapController.Frame(untransformedUpdateFrame);

      if (untransformedUpdateFrame != null)
      {
        transformFrame(untransformedUpdateFrame, _transformedUpdateFrame);

        DispatchUpdateFrameEvent(_transformedUpdateFrame);
      }
    }

    protected override void FixedUpdate()
    {

      if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics)
      {
        DispatchFixedFrameEvent(_transformedUpdateFrame);
        return;
      }

      _leapController.Frame(untransformedFixedFrame);

      if (untransformedFixedFrame != null)
      {
        transformFrame(untransformedFixedFrame, _transformedFixedFrame);

        DispatchFixedFrameEvent(_transformedFixedFrame);
      }
    }

    #region Unity Events

    protected override void Awake() {
      base.Awake();
      useInterpolation = false;
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

      _leapController = new Controller(1, true);
      _leapController.Device += (s, e) => {
        _onDeviceSafe?.Invoke(e.Device);
      };


      _onDeviceSafe += (d) => {
        _leapController.SubscribeToDeviceEvents(d);
      };

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
