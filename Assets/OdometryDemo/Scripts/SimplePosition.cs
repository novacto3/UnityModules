using UnityEngine;
using Leap.Unity;
using Leap;

public class SimplePosition : MonoBehaviour {
  public LeapServiceProvider LeapProvider;
  public bool shouldInterpolate = true;

  protected Frame _odometryFrame = new Frame();
  protected SmoothedFloat _smoothedUpdateToPrecullLatency = new SmoothedFloat();

  [HideInInspector]
  public Vector3 rawPosition;
  [HideInInspector]
  public Quaternion rawRotation;
  private Vector3 positionalDrift = Vector3.zero;
  private Quaternion rotationalDrift = Quaternion.identity;

  void OnEnable() {
    _smoothedUpdateToPrecullLatency.value = 1000;
    _smoothedUpdateToPrecullLatency.SetBlend(0.99f, 0.0111f);
  }

  public bool shouldOverride = true;
  void Update() {
    if (Input.GetKeyDown(KeyCode.R) || Input.touchCount > 0 || Input.GetMouseButtonDown(0)) {
      positionalDrift = Quaternion.Inverse(rawRotation) * rawPosition;
      rotationalDrift = rawRotation;
      GetComponent<TrailRenderer>().Clear();
    }
  }

  void LateUpdate() {
    _smoothedUpdateToPrecullLatency.value = Mathf.Min(_smoothedUpdateToPrecullLatency.value, 10000f);
    _smoothedUpdateToPrecullLatency.Update((LeapProvider.GetLeapController().Now() - LeapProvider.leaptime), Time.deltaTime);

    //LeapProvider.GetLeapController().Frame(_odometryFrame);
    //LeapProvider.GetLeapController().GetInterpolatedFrameFromTime(_odometryFrame, LeapProvider.CalculateInterpolationTime() + (ExtrapolationAmount * 1000), LeapProvider.CalculateInterpolationTime() - (BounceAmount * 1000));
    //LeapProvider.GetLeapController().GetInterpolatedFrame(_odometryFrame, LeapProvider.GetLeapController().Now());
    //LeapProvider.GetLeapController().GetInterpolatedFrame(_odometryFrame, LeapProvider.GetLeapController().Now() - (long)_smoothedTrackingLatency.value);
    //if (shouldInterpolate) {
      LeapInternal.LEAP_HEAD_POSE_EVENT headPoseEvent = new LeapInternal.LEAP_HEAD_POSE_EVENT();
      LeapProvider.GetLeapController().GetInterpolatedHeadPose(ref headPoseEvent, LeapProvider.CurrentFrame.Timestamp/* + (long)_smoothedUpdateToPrecullLatency.value*/); //This value is baaaasically 1000 all the time

      rawPosition = headPoseEvent.head_position.ToVector3() / 1000f;
      rawPosition = new Vector3(-rawPosition.x, -rawPosition.z, rawPosition.y);

      rawRotation = Quaternion.LookRotation(Vector3.up, -Vector3.forward) *
                             headPoseEvent.head_orientation.ToQuaternion() *
  Quaternion.Inverse(Quaternion.LookRotation(Vector3.up, -Vector3.forward));
    //}// else {
      //LeapProvider.GetLeapController().Frame(_odometryFrame);
    //}

    if (rawPosition == Vector3.zero) {
      rawPosition = (new Vector3(Mathf.Sin(Time.time), Mathf.Cos(Time.time), Mathf.Cos(Time.time * 2f))*0.1f);
      rawRotation = Quaternion.LookRotation(new Vector3(0f, 0.5f, 0.5f)-rawPosition.normalized);
    }

    transform.position = (Quaternion.Inverse(rotationalDrift) * rawPosition) - positionalDrift;
    transform.rotation = Quaternion.Inverse(rotationalDrift) * rawRotation;
  }
}