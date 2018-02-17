using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoRendererCamera : MonoBehaviour, IRuntimeGizmoRenderer {
  private const int UNLIT_SOLID_PASS = 0;
  private const int UNLIT_TRANSPARENT_PASS = 1;
  private const int SHADED_SOLID_PASS = 2;
  private const int SHADED_TRANSPARENT_PASS = 3;

  private Material _gizmoMat;

  public GizmoRendererCamera(Shader gizmoShader) {
    _gizmoMat = new Material(gizmoShader);
  }

  public void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
    if (_gizmoMat.color.a < 1) {
      _gizmoMat.SetPass(UNLIT_TRANSPARENT_PASS);
    } else {
      _gizmoMat.SetPass(UNLIT_SOLID_PASS);
    }

    Graphics.DrawMeshNow(mesh, matrix);
  }

  public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
    if (_gizmoMat.color.a < 1) {
      _gizmoMat.SetPass(SHADED_TRANSPARENT_PASS);
    } else {
      _gizmoMat.SetPass(SHADED_SOLID_PASS);
    }

    Graphics.DrawMeshNow(mesh, matrix);
  }

  public void DrawLine(Vector3 a, Vector3 b) {
    if (_gizmoMat.color.a < 1) {
      _gizmoMat.SetPass(UNLIT_TRANSPARENT_PASS);
    } else {
      _gizmoMat.SetPass(UNLIT_SOLID_PASS);
    }

    GL.Begin(GL.LINES);
    GL.Vertex(a);
    GL.Vertex(b);
    GL.End();
  }

  public void SetColor(Color color) {
    _gizmoMat.color = color;
  }
}
