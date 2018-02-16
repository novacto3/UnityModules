using System.Collections.Generic;
using UnityEngine;

public static class MonoBehaviourGizmoExtensions {

  public static void SetColor(this MonoBehaviour target, Color color) {
    drawer(target).color = color;
  }

  public static void SetMatrix(this MonoBehaviour target, Matrix4x4 matrix) {
    drawer(target).matrix = matrix;
  }

  public static void RelativeTo(this MonoBehaviour target, Transform transform) {
    drawer(target).RelativeTo(transform);
  }

  public static void PushMatrix(this MonoBehaviour target) {
    drawer(target).PushMatrix();
  }

  public static void PopMatrix(this MonoBehaviour target) {
    drawer(target).PopMatrix();
  }

  public static void DrawMesh(this MonoBehaviour target, Mesh mesh, Matrix4x4 matrix) {
    drawer(target).DrawMesh(mesh, matrix);
  }

  public static void DrawMesh(this MonoBehaviour target, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
    drawer(target).DrawMesh(mesh, position, rotation, scale);
  }

  public static void DrawMesh(this MonoBehaviour target, Mesh mesh, Vector3 position, Quaternion rotation) {
    drawer(target).DrawMesh(mesh, position, rotation);
  }

  public static void DrawMesh(this MonoBehaviour target, Mesh mesh, Vector3 position) {
    drawer(target).DrawMesh(mesh, position);
  }

  public static void DrawWireMesh(this MonoBehaviour target, Mesh mesh, Matrix4x4 matrix) {
    drawer(target).DrawWireMesh(mesh, matrix);
  }

  public static void DrawWireMesh(this MonoBehaviour target, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
    drawer(target).DrawWireMesh(mesh, position, rotation, scale);
  }

  public static void DrawWireMesh(this MonoBehaviour target, Mesh mesh, Vector3 position, Quaternion rotation) {
    drawer(target).DrawWireMesh(mesh, position, rotation);
  }

  public static void DrawWireMesh(this MonoBehaviour target, Mesh mesh, Vector3 position) {
    drawer(target).DrawWireMesh(mesh, position);
  }

  public static void DrawLine(this MonoBehaviour target, Vector3 a, Vector3 b) {
    drawer(target).DrawLine(a, b);
  }

  public static void DrawCube(this MonoBehaviour target, Vector3 position, Vector3 size) {
    drawer(target).DrawCube(position, size);
  }

  public static void DrawWireCube(this MonoBehaviour target, Vector3 position, Vector3 size) {
    drawer(target).DrawWireCube(position, size);
  }

  public static void DrawSphere(this MonoBehaviour target, Vector3 center, float radius) {
    drawer(target).DrawSphere(center, radius);
  }

  public static void DrawWireSphere(this MonoBehaviour target, Vector3 center, float radius) {
    drawer(target).DrawWireSphere(center, radius);
  }

  public static void DrawWireCircle(this MonoBehaviour target, Vector3 center, Vector3 direction, float radius) {
    drawer(target).DrawWireCircle(center, direction, radius);
  }

  public static void DrawWireCapsule(this MonoBehaviour target, Vector3 start, Vector3 end, float radius) {
    drawer(target).DrawWireCapsule(start, end, radius);
  }

  public static void DrawWireArc(this MonoBehaviour target, Vector3 center, Vector3 normal, Vector3 radialStartDirection, float radius, float fractionOfCircleToDraw, int numCircleSegments = 16) {
    drawer(target).DrawWireArc(center, normal, radialStartDirection, radius, fractionOfCircleToDraw, numCircleSegments);
  }

  public static void DrawColliders(this MonoBehaviour target,
                                        GameObject gameObject,
                                        bool useWireframe = true,
                                        bool traverseHierarchy = true,
                                        bool drawTriggers = false) {
    drawer(target).DrawColliders(gameObject, useWireframe, traverseHierarchy, drawTriggers);
  }

  public static void DrawPosition(this MonoBehaviour target, Vector3 pos, Color lerpColor, float lerpCoeff) {
    drawer(target).DrawPosition(pos, lerpColor, lerpCoeff);
  }

  public static void DrawPosition(this MonoBehaviour target, Vector3 pos) {
    drawer(target).DrawPosition(pos);
  }

  //Short method for making code smaller
  private static RuntimeGizmoDrawer drawer(MonoBehaviour target) {
    return RuntimeGizmoManager.instance.GetDrawer(target).drawer;
  }
}
