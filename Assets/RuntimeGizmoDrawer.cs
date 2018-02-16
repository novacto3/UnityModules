using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeGizmoDrawer {

  private GizmoBuffer _buffer;

  private Color _color;
  private Matrix4x4 _matrix;

  private Stack<Matrix4x4> _matrixStack = new Stack<Matrix4x4>();

  public GizmoBuffer buffer {
    set {
      _buffer = value;
    }
  }

  public Color color {
    get {
      return _color;
    }
    set {
      _color = value;
      _buffer.SetColor(_color);
    }
  }

  public Matrix4x4 matrix {
    get {
      return _matrix;
    }
    set {
      _matrix = value;
    }
  }

  public void Reset() {
    _color = Color.white;
    _matrix = Matrix4x4.identity;
    _matrixStack.Clear();
  }

  public void RelativeTo(Transform transform) {
    _matrix = transform.localToWorldMatrix;
  }

  public void PushMatrix() {
    _matrixStack.Push(_matrix);
  }

  public void PopMatrix() {
    _matrix = _matrixStack.Pop();
  }

  public void ResetMatrixAndColorState() {
    _matrix = Matrix4x4.identity;
    _color = Color.white;
  }

  public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
    _buffer.DrawMesh(mesh, matrix * _matrix);
  }

  public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
    DrawMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
  }

  public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation) {
    DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one));
  }

  public void DrawMesh(Mesh mesh, Vector3 position) {
    DrawMesh(mesh, Matrix4x4.Translate(position));
  }

  public void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
    _buffer.DrawWireMesh(mesh, matrix * _matrix);
  }

  public void DrawWireMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
    DrawWireMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
  }

  public void DrawWireMesh(Mesh mesh, Vector3 position, Quaternion rotation) {
    DrawWireMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one));
  }

  public void DrawWireMesh(Mesh mesh, Vector3 position) {
    DrawWireMesh(mesh, Matrix4x4.Translate(position));
  }

  public void DrawLine(Vector3 a, Vector3 b) {
    _buffer.DrawLine(a, b);
  }

  public void DrawCube(Vector3 position, Vector3 size) {
    //TODO
  }

  public void DrawWireCube(Vector3 position, Vector3 size) {
    //TODO
  }

  public void DrawSphere(Vector3 center, float radius) {
    //TODO
  }

  public void DrawWireSphere(Vector3 center, float radius) {
    //TODO
  }

  public void DrawWireCircle(Vector3 center, Vector3 direction, float radius) {
    //TODO
  }

  public void DrawWireCapsule(Vector3 start, Vector3 end, float radius) {
    //TODO
  }

  public void DrawWireArc(Vector3 center, Vector3 normal, Vector3 radialStartDirection, float radius, float fractionOfCircleToDraw, int numCircleSegments = 16) {
    //TODO
  }

  public void DrawColliders(GameObject gameObject, bool useWireframe = true,
                                                   bool traverseHierarchy = true,
                                                   bool drawTriggers = false) {
    //TODO
  }

  public void DrawPosition(Vector3 pos, Color lerpColor, float lerpCoeff) {
    //TODO
  }

  public void DrawPosition(Vector3 pos) {
    //TODO
  }

}
