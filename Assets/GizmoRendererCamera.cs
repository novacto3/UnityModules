using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoRendererCamera : MonoBehaviour, IRuntimeGizmoRenderer {

  private Material _wireMaterial;
  private Material _filledMaterial;

  public void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
    _wireMaterial.SetPass(0);
    Graphics.DrawMeshNow(mesh, matrix);
  }

  public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
    _filledMaterial.SetPass(0);
    Graphics.DrawMeshNow(mesh, matrix);
  }

  public void DrawLine(Vector3 a, Vector3 b) {
    _wireMaterial.SetPass(0);
    GL.Begin(GL.LINES);
    GL.Vertex(a);
    GL.Vertex(b);
    GL.End();
  }

  public void SetColor(Color color) {
    _wireMaterial.color = color;
    _filledMaterial.color = color;
  }
}
