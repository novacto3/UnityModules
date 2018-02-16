using UnityEngine;

[AddComponentMenu("")]
public class HiddenGizmoRendererComponent : MonoBehaviour {

  private GizmoBuffer _buffer = new GizmoBuffer();
  private GizmoRenderer _renderer = new GizmoRenderer();

  public GizmoBuffer buffer {
    get { return _buffer; }
  }

  private void Awake() {
    //hideFlags = HideFlags.HideAndDontSave;
  }

  private void Update() {
    //hideFlags = HideFlags.HideAndDontSave;
    _buffer.Clear();
  }

  private void OnDrawGizmos() {
    _buffer.Replay(_renderer);
  }

  private class GizmoRenderer : IRuntimeGizmoRenderer {
    public void SetTarget(MonoBehaviour target) { }

    public void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
      var prevMatrix = Gizmos.matrix;
      Gizmos.matrix = matrix;
      Gizmos.DrawWireMesh(mesh);
      Gizmos.matrix = prevMatrix;
    }

    public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
      var prevMatrix = Gizmos.matrix;
      Gizmos.matrix = matrix;
      Gizmos.DrawMesh(mesh);
      Gizmos.matrix = prevMatrix;
    }

    public void DrawLine(Vector3 a, Vector3 b) {
      Gizmos.DrawLine(a, b);
    }

    public void SetColor(Color color) {
      Gizmos.color = color;
    }
  }
}

public class GizmoRendererUnity : IRuntimeGizmoRenderer {

  private HiddenGizmoRendererComponent _currComponent;

  public void SetTarget(MonoBehaviour target) {
    _currComponent = target.GetComponent<HiddenGizmoRendererComponent>();
    if (_currComponent == null) {
      _currComponent = target.gameObject.AddComponent<HiddenGizmoRendererComponent>();
    }

    _currComponent.buffer.Clear();
  }

  public void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
    _currComponent.buffer.DrawWireMesh(mesh, matrix);
  }

  public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
    _currComponent.buffer.DrawMesh(mesh, matrix);
  }

  public void DrawLine(Vector3 a, Vector3 b) {
    _currComponent.buffer.DrawLine(a, b);
  }

  public void SetColor(Color color) {
    _currComponent.buffer.SetColor(color);
  }
}
