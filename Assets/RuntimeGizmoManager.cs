using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class RuntimeGizmoManager : MonoBehaviour {

  private static RuntimeGizmoManager _cachedInstance;
  public static RuntimeGizmoManager instance {
    get {
      //TODO:
      return null;
    }
  }

  //Mesh data
  public static Mesh wireCubeMesh { get; private set; }
  public static Mesh filledCubeMesh { get; private set; }
  public static Mesh wireSphereMesh { get; private set; }
  public static Mesh filledSphereMesh { get; private set; }

  private Stack<GizmoBuffer> _bufferPool = new Stack<GizmoBuffer>();
  private BufferGroup _currBufferGroup;
  private BufferGroup _prevBufferGroup;

  private RuntimeGizmoDrawer _drawer;

  public RuntimeGizmoDrawer GetDrawer(MonoBehaviour target) {
    GizmoBuffer buffer;
    if (_currBufferGroup.contextMap.TryGetValue(target, out buffer)) {
      if (_bufferPool.Count > 0) {
        buffer = _bufferPool.Pop();
      } else {
        buffer = new GizmoBuffer();
      }
      _currBufferGroup.contextMap[target] = buffer;
    }

    _drawer.buffer = buffer;
    return _drawer;
  }

  private class BufferGroup {
    public Dictionary<MonoBehaviour, GizmoBuffer> contextMap = new Dictionary<MonoBehaviour, GizmoBuffer>();
    public GizmoBuffer nullBuffer;

    public BufferGroup() {
      nullBuffer = new GizmoBuffer();
    }

    public void Render(IRuntimeGizmoRenderer renderer) {
      renderer.SetTarget(null);
      nullBuffer.Replay(renderer);

      foreach (var pair in contextMap) {
        renderer.SetTarget(pair.Key);
        pair.Value.Replay(renderer);
      }
    }
  }
}
