using System;
using UnityEngine;
using UnityEngine.UI;
using Leap.Unity;
using Leap;
using Leap.Unity.RuntimeGizmos;

public class CameraPositionOverride : MonoBehaviour, IRuntimeGizmoComponent
{
  public LeapServiceProvider LeapProvider;
  //public Text latencyText;
  public int ExtrapolationAmount = 0;
  public int BounceAmount = 0;
  [Range(0.005f, 0.08f)]
  public float adjustment = 0.045f;
  public bool shouldInterpolate = true;

  protected Frame _odometryFrame = new Frame();
  //protected LeapVRTemporalWarping warping;
  protected SmoothedFloat _smoothedUpdateToPrecullLatency = new SmoothedFloat();

  [HideInInspector]
  public Vector3 rawPosition;
  [HideInInspector]
  public Quaternion rawRotation;
  private Vector3 positionalDrift = Vector3.zero;
  bool updated = false;

  private TimeDelay delay1 = new TimeDelay();
  private bool useOculus = true;
  private bool drawTrajectory = false;

  public RingBuffer<Vector3> positions = new RingBuffer<Vector3>(500);
  public RingBuffer<Quaternion> rotations = new RingBuffer<Quaternion>(500);
  int stationaryOffset = 0;

  void OnEnable()
  {
    LeapVRCameraControl.OnPreCullEvent += onPreCull;
    _smoothedUpdateToPrecullLatency.value = 1000;
    _smoothedUpdateToPrecullLatency.SetBlend(0.99f, 0.0111f);
    //warping = GetComponentInChildren<LeapVRTemporalWarping>();
    LeapProvider.GetLeapController().headPoseChange += onHeadPoseChange;
  }

  void OnDisable()
  {
    LeapVRCameraControl.OnPreCullEvent -= onPreCull;
    LeapProvider.GetLeapController().headPoseChange -= onHeadPoseChange;
  }

  void onHeadPoseChange(object sender, HeadPoseEventArgs args)
  {
    /*rawPosition = args.headPosition.ToVector3()/1000f;
    rawPosition = new Vector3(-rawPosition.x, -rawPosition.z, rawPosition.y);

    rawRotation =  Quaternion.LookRotation(Vector3.up, -Vector3.forward) *
                      args.headOrientation.ToQuaternion() *
                   //Quaternion.LookRotation(args.headOrientation.m3.ToVector3(), 
                   //                        args.headOrientation.m2.ToVector3()) *
    Quaternion.Inverse(Quaternion.LookRotation(Vector3.up, -Vector3.forward));*/


    updated = true;

    //Debug.Log(args.time);
  }

  public bool shouldOverride = true;
  void Update()
  {
    if (shouldOverride) {
      transform.position = rawPosition;
      transform.rotation = rawRotation;
    }

    if (Input.GetKeyDown(KeyCode.Space)) {
      shouldOverride = !shouldOverride;
      //warping.autoUpdateHistory = !shouldOverride;
    }

    if (Input.GetKeyDown(KeyCode.R)) {
      positionalDrift = rawPosition + positionalDrift - transform.parent.position;
    }

    if (Input.GetKeyDown(KeyCode.E)) {
      if (ExtrapolationAmount == 0) {
        ExtrapolationAmount = 15;
      } else {
        ExtrapolationAmount = 0;
      }
    }
    if (Input.GetKeyDown(KeyCode.O)) {
      useOculus = !useOculus;
    }
    if (Input.GetKeyDown(KeyCode.V)) {
      drawTrajectory = !drawTrajectory;
    }

    if (Input.GetKey(KeyCode.RightArrow)) {
      adjustment += 0.0001f;
      adjustment = Mathf.Clamp(adjustment, 0.005f, 0.08f);
      //latencyText.text = "Latency Compensation Amount: " + String.Format("{0:0.0}", adjustment *1000f) + "ms";
    }
    if (Input.GetKey(KeyCode.LeftArrow)) {
      adjustment -= 0.0001f;
      adjustment = Mathf.Clamp(adjustment, 0.005f, 0.08f);
      //latencyText.text = "Latency Compensation Amount: " + String.Format("{0:0.0}", adjustment * 1000f) + "ms";
    }
  }

  private void onPreCull(LeapVRCameraControl control)
  {
    if (shouldOverride) {
      _smoothedUpdateToPrecullLatency.value = Mathf.Min(_smoothedUpdateToPrecullLatency.value, 10000f);
      _smoothedUpdateToPrecullLatency.Update((LeapProvider.GetLeapController().Now() - LeapProvider.leaptime), Time.deltaTime);

      //We'll get the interpolated API working at some point
      //LeapProvider.GetLeapController().Frame(_odometryFrame);
      //LeapProvider.GetLeapController().GetInterpolatedFrameFromTime(_odometryFrame, LeapProvider.CalculateInterpolationTime() + (ExtrapolationAmount * 1000), LeapProvider.CalculateInterpolationTime() - (BounceAmount * 1000));
      //LeapProvider.GetLeapController().GetInterpolatedFrame(_odometryFrame, LeapProvider.GetLeapController().Now());
      //LeapProvider.GetLeapController().GetInterpolatedFrame(_odometryFrame, LeapProvider.GetLeapController().Now() - (long)_smoothedTrackingLatency.value);
      //LeapProvider.GetLeapController().GetInterpolatedFrame(_odometryFrame, LeapProvider.CurrentFrame.Timestamp + (long)_smoothedUpdateToPrecullLatency.value); //This value is baaaasically 1000 all the time

      //if (shouldInterpolate) {
      LeapInternal.LEAP_HEAD_POSE_EVENT headPoseEvent = new LeapInternal.LEAP_HEAD_POSE_EVENT();
      LeapProvider.GetLeapController().GetInterpolatedHeadPose(ref headPoseEvent, LeapProvider.GetLeapController().Now());//LeapProvider.CurrentFrame.Timestamp/* + (long)_smoothedUpdateToPrecullLatency.value*/); //This value is baaaasically 1000 all the time

      rawPosition = headPoseEvent.head_position.ToVector3() / 1000f;
      rawPosition = new Vector3(-rawPosition.x, -rawPosition.z, rawPosition.y);

      rawRotation = Quaternion.LookRotation(Vector3.up, -Vector3.forward) *
                             headPoseEvent.head_orientation.ToQuaternion() *
  Quaternion.Inverse(Quaternion.LookRotation(Vector3.up, -Vector3.forward));
      //Debug.Log("Event Timestamp: "+headEvent.timestamp+" "+ LeapProvider.GetLeapController().Now());
      //} else {
      //  LeapProvider.GetLeapController().Frame(_odometryFrame);
      //}

      /*rawPosition = _odometryFrame.HeadPosition.ToVector3();// / 1000f;
      rawPosition = new Vector3(-rawPosition.x, -rawPosition.z, rawPosition.y);

      rawRotation = Quaternion.LookRotation(Vector3.up, -Vector3.forward) *
                            _odometryFrame.HeadOrientation.ToQuaternion() *
 Quaternion.Inverse(Quaternion.LookRotation(Vector3.up, -Vector3.forward));*/

      /*if (rawPosition == Vector3.zero) {
        rawPosition = new Vector3(Mathf.Sin(Time.time), Mathf.Cos(Time.time), Mathf.Cos(Time.time*2f));
        rawRotation = Quaternion.LookRotation(-rawPosition.normalized);
      }*/

      Quaternion OculusRotation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.CenterEye);
      Quaternion delayedOculusRotation1;
      delay1.UpdateDelay(OculusRotation, Time.time, adjustment, out delayedOculusRotation1);

      /*if (updated) {
        rawPosition += rawRotation * Vector3.back * 0.11f;
        rawPosition -= positionalDrift;
        updated = false;
      }*/

      //Quaternion deltaRotation = Quaternion.Inverse(delayedOculusRotation1) * OculusRotation;
      //if (useOculus) {
      //  rawRotation *= deltaRotation;
      //}

      //warping.ManuallyUpdateTemporalWarping(rawPosition, rawRotation);

      positions.Add(rawPosition);
      rotations.Add(rawRotation);

      stationaryOffset--;
      if (stationaryOffset == -1) {
        stationaryOffset = 19;
      }
      //control.SetCameraTransform(rawPosition, rawRotation);
      transform.SetPositionAndRotation(rawPosition, rawRotation);
    }
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer)
  {
    if (drawTrajectory) {
      for (int i = 0; i < positions.Count - 1; i++) {
        drawer.DrawLine(positions.Get(i), positions.Get(i + 1));
      }

      drawer.color = Color.red;
      for (int i = stationaryOffset; i < rotations.Count - 1; i += 20) {
        drawer.DrawLine(positions.Get(i), positions.Get(i) + (rotations.Get(i + 1) * Vector3.right * 0.1f));
      }

      drawer.color = Color.green;
      for (int i = stationaryOffset; i < rotations.Count - 1; i += 20) {
        drawer.DrawLine(positions.Get(i), positions.Get(i) + (rotations.Get(i + 1) * Vector3.up * 0.1f));
      }

      drawer.color = Color.blue;
      for (int i = stationaryOffset; i < rotations.Count - 1; i += 20) {
        drawer.DrawLine(positions.Get(i), positions.Get(i) + (rotations.Get(i + 1) * Vector3.forward * 0.1f));
      }
    }
  }
}