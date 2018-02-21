using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.RuntimeGizmos.Internal {

  public class GizmoMeshes {
    private const int CIRCLE_RESOLUTION = 32;

    public readonly MeshCollectionWire wire;
    public readonly MeshCollectionSolid solid;

    public GizmoMeshes(Mesh solidSphereMesh) {
      wire = new MeshCollectionWire();
      solid = new MeshCollectionSolid(solidSphereMesh);
    }

    public class MeshCollectionWire {
      public readonly Mesh sphere;
      public readonly Mesh cube;
      public readonly Mesh circle;

      public MeshCollectionWire() {
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> lines = new List<int>();

        //Create wire cube
        {
          cube = new Mesh();
          cube.name = "RuntimeGizmos Wire Cube";
          cube.hideFlags = HideFlags.HideAndDontSave;

          for (int dx = 1; dx >= -1; dx -= 2) {
            for (int dy = 1; dy >= -1; dy -= 2) {
              for (int dz = 1; dz >= -1; dz -= 2) {
                verts.Add(0.5f * new Vector3(dx, dy, dz));
                normals.Add(new Vector3(dx, dy, dz).normalized);
              }
            }
          }

          Action<int, int, int, int> addCorner = (a, b, c, d) => {
            lines.Add(a); lines.Add(b);
            lines.Add(a); lines.Add(c);
            lines.Add(a); lines.Add(d);
          };

          addCorner(0, 1, 2, 4);
          addCorner(3, 1, 2, 7);
          addCorner(5, 1, 4, 7);
          addCorner(6, 2, 4, 7);

          cube.SetVertices(verts);
          cube.SetNormals(normals);
          cube.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
          cube.RecalculateBounds();
          cube.UploadMeshData(markNoLogerReadable: true);

          verts.Clear();
          normals.Clear();
          lines.Clear();
        }

        //Create wire sphere
        {
          sphere = new Mesh();
          sphere.name = "RuntimeGizmos Wire Sphere";
          sphere.hideFlags = HideFlags.HideAndDontSave;

          int totalVerts = CIRCLE_RESOLUTION * 3;
          for (int i = 0; i < CIRCLE_RESOLUTION; i++) {
            float angle = Mathf.PI * 2 * i / CIRCLE_RESOLUTION;
            float dx = 0.5f * Mathf.Cos(angle);
            float dy = 0.5f * Mathf.Sin(angle);

            for (int j = 0; j < 3; j++) {
              lines.Add((i * 3 + j + 0) % totalVerts);
              lines.Add((i * 3 + j + 3) % totalVerts);
            }

            verts.Add(new Vector3(dx, dy, 0));
            verts.Add(new Vector3(0, dx, dy));
            verts.Add(new Vector3(dx, 0, dy));

            normals.Add(new Vector3(dx, dy, 0).normalized);
            normals.Add(new Vector3(0, dx, dy).normalized);
            normals.Add(new Vector3(dx, 0, dy).normalized);
          }

          sphere.SetVertices(verts);
          sphere.SetNormals(normals);
          sphere.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
          sphere.RecalculateBounds();
          sphere.UploadMeshData(markNoLogerReadable: true);

          verts.Clear();
          normals.Clear();
          lines.Clear();
        }

        //Create wire circle
        {
          circle = new Mesh();
          circle.name = "RuntimeGizmos Wire Circle";
          circle.hideFlags = HideFlags.HideAndDontSave;

          for (int i = 0; i < CIRCLE_RESOLUTION; i++) {
            float angle = Mathf.PI * 2 * i / CIRCLE_RESOLUTION;
            float dx = 0.5f * Mathf.Cos(angle);
            float dy = 0.5f * Mathf.Sin(angle);

            lines.Add((verts.Count + 0) % CIRCLE_RESOLUTION);
            lines.Add((verts.Count + 1) % CIRCLE_RESOLUTION);

            verts.Add(new Vector3(dx, dy, 0));
            normals.Add(new Vector3(dx, dy, 0).normalized);
          }

          circle.SetVertices(verts);
          circle.SetNormals(normals);
          circle.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
          circle.RecalculateBounds();
          circle.UploadMeshData(markNoLogerReadable: true);

          verts.Clear();
          normals.Clear();
          lines.Clear();
        }
      }
    }

    public class MeshCollectionSolid {
      public readonly Mesh sphere;
      public readonly Mesh cube;

      public MeshCollectionSolid(Mesh sphere) {
        this.sphere = sphere;

        List<Vector3> verts = new List<Vector3>();
        List<int> quads = new List<int>();

        //Create solid cube
        {
          cube = new Mesh();
          cube.name = "RuntimeGizmos Solid Cube";
          cube.hideFlags = HideFlags.HideAndDontSave;

          Vector3[] faces = new Vector3[] { Vector3.forward, Vector3.right, Vector3.up };
          Action<Vector3, Vector3, Vector3> addQuad = (normal, axis1, axis2) => {
            quads.Add(verts.Count + 0);
            quads.Add(verts.Count + 1);
            quads.Add(verts.Count + 2);
            quads.Add(verts.Count + 3);

            verts.Add(0.5f * (normal + axis1 + axis2));
            verts.Add(0.5f * (normal + axis1 - axis2));
            verts.Add(0.5f * (normal - axis1 - axis2));
            verts.Add(0.5f * (normal - axis1 + axis2));
          };

          for (int i = 0; i < 3; i++) {
            addQuad(faces[(i + 0) % 3], -faces[(i + 1) % 3], faces[(i + 2) % 3]);
            addQuad(-faces[(i + 0) % 3], faces[(i + 1) % 3], faces[(i + 2) % 3]);
          }

          cube.SetVertices(verts);
          cube.SetIndices(quads.ToArray(), MeshTopology.Quads, submesh: 0);
          cube.RecalculateNormals();
          cube.RecalculateBounds();
          cube.UploadMeshData(markNoLogerReadable: true);

          verts.Clear();
          quads.Clear();
        }
      }
    }
  }
}
