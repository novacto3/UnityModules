using UnityEngine;
using Leap.Unity;
public class TimeDelay {
  RingBuffer<TransformData> history = new RingBuffer<TransformData>(128);

  public void UpdateDelay(Vector3 curPos, Quaternion curRot, long timestamp, long delay, out Vector3 delayedPos, out Quaternion delayedRot) {
    //Store current Transform in History
    TransformData currentTransform =
      new TransformData() {
        time = timestamp,
        position = curPos,
        rotation = curRot,
      };

    history.Add(currentTransform);

    //Calculate delayed Transform
    TransformData desiredTransform = TransformData.GetTransformAtTime(history, timestamp - delay);

    //Apply delayed Transform
    delayedPos = desiredTransform.position;
    delayedRot = desiredTransform.rotation;
  }

  public void UpdateDelay(Quaternion curRot, float timestamp, float delay, out Quaternion delayedRot) {
    //Store current Transform in History
    TransformData currentTransform =
      new TransformData() {
        time = timestamp,
        position = Vector3.zero,
        rotation = curRot,
      };

    history.Add(currentTransform);

    //Calculate delayed Transform
    TransformData desiredTransform = TransformData.GetTransformAtTime(history, timestamp - delay);

    //Apply delayed Transform
    delayedRot = desiredTransform.rotation;
  }

  protected struct TransformData {
    public float time; // microseconds
    public Vector3 position; //meters
    public Quaternion rotation; //magic

    public static TransformData Lerp(TransformData from, TransformData to, float time) {
      if (from.time == to.time) {
        return from;
      }
      float fraction = (time - from.time) / (to.time - from.time);
      return new TransformData() {
        time = time,
        position = Vector3.Lerp(from.position, to.position, fraction),
        rotation = Quaternion.Slerp(from.rotation, to.rotation, fraction)
      };
    }

    public static TransformData GetTransformAtTime(RingBuffer<TransformData> history, float desiredTime) {
      for (int i = history.Count - 1; i > 0; i--) {
        if (history.Get(i).time >= desiredTime && history.Get(i - 1).time < desiredTime) {
          return Lerp(history.Get(i - 1), history.Get(i), desiredTime);
        }
      }
      return history.GetLatest();
    }
  }
}