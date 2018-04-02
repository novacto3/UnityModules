using UnityEngine;
using Leap.Unity;
using Leap.Unity.RuntimeGizmos;

public class QuickGrapher : MonoBehaviour, IRuntimeGizmoComponent {
  public RingBuffer<float> sampleBuffer = new RingBuffer<float>(100);
  public RingBuffer<float> timeBuffer = new RingBuffer<float>(100);
  float xScale = 2f;
	
	public void UpdateSample(float value, float time) {
    sampleBuffer.Add(value);
    timeBuffer.Add(time);
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (sampleBuffer.Count > 2 && timeBuffer.Count > 2) {
      float beginningTime = timeBuffer.GetOldest();
      for (int i = sampleBuffer.Count - 1; i > 1; i--) {
        Debug.DrawLine(new Vector3(xScale * (timeBuffer[i] - beginningTime), sampleBuffer[i]+4f, 0f),
                       new Vector3(xScale * (timeBuffer[i - 1] - beginningTime), sampleBuffer[i - 1]+4f, 0f));
      }
    }
  }
}
