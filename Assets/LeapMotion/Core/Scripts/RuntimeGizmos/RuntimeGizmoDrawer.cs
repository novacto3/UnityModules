using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.RuntimeGizmos {
  using Internal;

  public class RuntimeGizmoDrawer {

    private GizmoMeshes _meshes;
    private GizmoBuffer _buffer;

    private Color _color;
    private Matrix4x4 _matrix;

    private Stack<Matrix4x4> _matrixStack = new Stack<Matrix4x4>();

    public GizmoBuffer buffer {
      set {
        _buffer = value;
      }
    }

    /// <summary>
    /// Sets or gets the color for the gizmos that will be drawn next.
    /// </summary>
    [CreateExtension]
    public Color color {
      get {
        return _color;
      }
      set {
        _color = value;
        _buffer.SetColor(_color);
      }
    }

    /// <summary>
    /// Sets or gets the matrix used to transform all gizmos.
    /// </summary>
    [CreateExtension]
    public Matrix4x4 matrix {
      get {
        return _matrix;
      }
      set {
        _matrix = value;
      }
    }

    public RuntimeGizmoDrawer(GizmoMeshes meshes) {
      _meshes = meshes;
    }

    public void Reset() {
      _color = Color.white;
      _matrix = Matrix4x4.identity;
      _matrixStack.Clear();

      _buffer.SetColor(_color);
    }

    [CreateExtension]
    public void RelativeTo(Transform transform) {
      _matrix = transform.localToWorldMatrix;
    }

    [CreateExtension]
    public void PushMatrix() {
      _matrixStack.Push(_matrix);
    }

    [CreateExtension]
    public void PopMatrix() {
      _matrix = _matrixStack.Pop();
    }

    public void ResetMatrixAndColorState() {
      _matrix = Matrix4x4.identity;
      _color = Color.white;
    }

    /// <summary>
    /// Draws a filled gizmo mesh at the given transform location.
    /// </summary>
    [CreateExtension]
    public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
      _buffer.DrawMesh(mesh, matrix * _matrix);
    }

    /// <summary>
    /// Draws a filled gizmo mesh at the given transform location.
    /// </summary>
    [CreateExtension]
    public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    /// <summary>
    /// Draws a filled gizmo mesh at the given transform location.
    /// </summary>
    [CreateExtension]
    public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation) {
      DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one));
    }

    /// <summary>
    /// Draws a filled gizmo mesh at the given transform location.
    /// </summary>
    [CreateExtension]
    public void DrawMesh(Mesh mesh, Vector3 position) {
      DrawMesh(mesh, Matrix4x4.Translate(position));
    }

    /// <summary>
    /// Draws a wire gizmo mesh using the given matrix transform.
    /// </summary>
    [CreateExtension]
    public void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
      _buffer.DrawWireMesh(mesh, matrix * _matrix);
    }

    /// <summary>
    /// Draws a wire gizmo mesh using the given matrix transform.
    /// </summary>
    [CreateExtension]
    public void DrawWireMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawWireMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    /// <summary>
    /// Draws a wire gizmo mesh using the given matrix transform.
    /// </summary>
    [CreateExtension]
    public void DrawWireMesh(Mesh mesh, Vector3 position, Quaternion rotation) {
      DrawWireMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one));
    }

    /// <summary>
    /// Draws a wire gizmo mesh at the given transform location.
    /// </summary>
    [CreateExtension]
    public void DrawWireMesh(Mesh mesh, Vector3 position) {
      DrawWireMesh(mesh, Matrix4x4.Translate(position));
    }

    /// <summary>
    /// Draws a gizmo line that connects the two positions.
    /// </summary>
    [CreateExtension]
    public void DrawLine(Vector3 a, Vector3 b) {
      _buffer.DrawLine(_matrix.MultiplyPoint(a), _matrix.MultiplyPoint(b));
    }

    /// <summary>
    /// Draws a filled gizmo cube at the given position with the given size.
    /// </summary>
    [CreateExtension]
    public void DrawCube(Vector3 position, Vector3 size) {
      DrawMesh(_meshes.solid.cube, Matrix4x4.TRS(position, Quaternion.identity, size));
    }

    /// <summary>
    /// Draws a wire gizmo cube at the given position with the given size.
    /// </summary>
    [CreateExtension]
    public void DrawWireCube(Vector3 position, Vector3 size) {
      DrawWireMesh(_meshes.wire.cube, Matrix4x4.TRS(position, Quaternion.identity, size));
    }

    /// <summary>
    /// Draws a filled gizmo sphere at the given position with the given radius.
    /// </summary>
    [CreateExtension]
    public void DrawSphere(Vector3 center, float radius) {
      DrawMesh(_meshes.solid.sphere, Matrix4x4.TRS(center, Quaternion.identity, Vector3.one * radius));
    }

    /// <summary>
    /// Draws a wire gizmo sphere at the given position with the given radius.
    /// </summary>
    [CreateExtension]
    public void DrawWireSphere(Vector3 center, float radius) {
      DrawWireMesh(_meshes.wire.sphere, Matrix4x4.TRS(center, Quaternion.identity, Vector3.one * radius));
    }

    /// <summary>
    /// Draws a wire gizmo circle at the given position, with the given normal and radius.
    /// </summary>
    [CreateExtension]
    public void DrawWireCircle(Vector3 center, Vector3 direction, float radius) {
      DrawWireMesh(_meshes.wire.circle, Matrix4x4.Scale(Vector3.one * radius) * Matrix4x4.LookAt(Vector3.zero, direction, Vector3.up));
    }

    /// <summary>
    /// Draws a wire gizmo capsule at the given position, with the given start and end points and radius.
    /// </summary>
    [CreateExtension]
    public void DrawWireCapsule(Vector3 start, Vector3 end, float radius) {
      Vector3 up = (end - start).normalized * radius;
      Vector3 forward = Vector3.Slerp(up, -up, 0.5F);
      Vector3 right = Vector3.Cross(up, forward).normalized * radius;

      float height = (start - end).magnitude;

      // Radial circles
      DrawLineWireCircle(start, up, radius, 8);
      DrawLineWireCircle(end, -up, radius, 8);

      // Sides
      DrawLine(start + right, end + right);
      DrawLine(start - right, end - right);
      DrawLine(start + forward, end + forward);
      DrawLine(start - forward, end - forward);

      // Endcaps
      DrawWireArc(start, right, forward, radius, 0.5F, 8);
      DrawWireArc(start, forward, -right, radius, 0.5F, 8);
      DrawWireArc(end, right, -forward, radius, 0.5F, 8);
      DrawWireArc(end, forward, right, radius, 0.5F, 8);
    }

    private void DrawLineWireCircle(Vector3 center, Vector3 normal, float radius, int numCircleSegments = 16) {
      DrawWireArc(center, normal, Vector3.Slerp(normal, -normal, 0.5F), radius, 1.0F, numCircleSegments);
    }

    [CreateExtension]
    public void DrawWireArc(Vector3 center, Vector3 normal, Vector3 radialStartDirection, float radius, float fractionOfCircleToDraw, int numCircleSegments = 16) {
      normal = normal.normalized;
      Vector3 radiusVector = radialStartDirection.normalized * radius;
      Vector3 nextVector;
      int numSegmentsToDraw = (int)(numCircleSegments * fractionOfCircleToDraw);
      for (int i = 0; i < numSegmentsToDraw; i++) {
        nextVector = Quaternion.AngleAxis(360F / numCircleSegments, normal) * radiusVector;
        DrawLine(center + radiusVector, center + nextVector);
        radiusVector = nextVector;
      }
    }

    private List<Collider> _colliderList = new List<Collider>();
    [CreateExtension]
    public void DrawColliders(GameObject gameObject, bool useWireframe = true,
                                                     bool traverseHierarchy = true,
                                                     bool drawTriggers = false) {
      PushMatrix();

      if (traverseHierarchy) {
        gameObject.GetComponentsInChildren(_colliderList);
      } else {
        gameObject.GetComponents(_colliderList);
      }

      for (int i = 0; i < _colliderList.Count; i++) {
        Collider collider = _colliderList[i];
        RelativeTo(collider.transform);

        if (collider.isTrigger && !drawTriggers) { continue; }

        if (collider is BoxCollider) {
          BoxCollider box = collider as BoxCollider;
          if (useWireframe) {
            DrawWireCube(box.center, box.size);
          } else {
            DrawCube(box.center, box.size);
          }
        } else if (collider is SphereCollider) {
          SphereCollider sphere = collider as SphereCollider;
          if (useWireframe) {
            DrawWireSphere(sphere.center, sphere.radius);
          } else {
            DrawSphere(sphere.center, sphere.radius);
          }
        } else if (collider is CapsuleCollider) {
          CapsuleCollider capsule = collider as CapsuleCollider;
          if (useWireframe) {
            Vector3 capsuleDir;
            switch (capsule.direction) {
              case 0: capsuleDir = Vector3.right; break;
              case 1: capsuleDir = Vector3.up; break;
              case 2: default: capsuleDir = Vector3.forward; break;
            }
            DrawWireCapsule(capsule.center + capsuleDir * (capsule.height / 2F - capsule.radius),
                            capsule.center - capsuleDir * (capsule.height / 2F - capsule.radius), capsule.radius);
          } else {
            Vector3 size = Vector3.zero;
            size += Vector3.one * capsule.radius * 2;
            size += new Vector3(capsule.direction == 0 ? 1 : 0,
                                capsule.direction == 1 ? 1 : 0,
                                capsule.direction == 2 ? 1 : 0) * (capsule.height - capsule.radius * 2);
            DrawCube(capsule.center, size);
          }
        } else if (collider is MeshCollider) {
          MeshCollider mesh = collider as MeshCollider;
          if (mesh.sharedMesh != null) {
            if (useWireframe) {
              DrawWireMesh(mesh.sharedMesh, Matrix4x4.identity);
            } else {
              DrawMesh(mesh.sharedMesh, Matrix4x4.identity);
            }
          }
        }
      }

      PopMatrix();
    }

    /// <summary>
    /// Draws a simple XYZ-cross position gizmo at the target position, whose size is
    /// scaled relative to the main camera's distance to the target position (for reliable
    /// visibility).
    /// 
    /// You can also provide a color argument and lerp coefficient towards that color from
    /// the axes' default colors (red, green, blue). Colors are lerped in HSV space.
    /// </summary>
    [CreateExtension]
    public void DrawPosition(Vector3 pos, Color lerpColor, float lerpCoeff) {
      float targetScale = 0.06f; // 6 cm at 1m away.

      var mainCam = Camera.main;
      var posWorldSpace = matrix * pos;
      if (mainCam != null) {
        float camDistance = Vector3.Distance(posWorldSpace, mainCam.transform.position);

        targetScale *= camDistance;
      }

      float extent = (targetScale / 2f);

      color = Color.red;
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos - Vector3.right * extent, pos + Vector3.right * extent);

      color = Color.green;
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos - Vector3.up * extent, pos + Vector3.up * extent);

      color = Color.blue;
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos - Vector3.forward * extent, pos + Vector3.forward * extent);
    }

    /// <summary>
    /// Draws a simple XYZ-cross position gizmo at the target position, whose size is
    /// scaled relative to the main camera's distance to the target position (for reliable
    /// visibility).
    /// </summary>
    [CreateExtension]
    public void DrawPosition(Vector3 pos) {
      DrawPosition(pos, Color.white, 0f);
    }
  }
}
