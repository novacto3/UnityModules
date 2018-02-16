using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoBuffer {
  private List<Command> _commands = new List<Command>();
  private List<Matrix4x4> _matrices = new List<Matrix4x4>();
  private List<Color> _colors = new List<Color>();
  private List<Mesh> _meshes = new List<Mesh>();
  private List<Line> _lines = new List<Line>();

  public void Clear() {
    _commands.Clear();
    _matrices.Clear();
    _colors.Clear();
    _meshes.Clear();
    _lines.Clear();
  }

  public void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
    _commands.Add(Command.DrawWireMesh);
    _meshes.Add(mesh);
    _matrices.Add(matrix);
  }

  public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
    _commands.Add(Command.DrawMesh);
    _meshes.Add(mesh);
    _matrices.Add(matrix);
  }

  public void DrawLine(Vector3 a, Vector3 b) {
    _commands.Add(Command.DrawLine);
    _lines.Add(new Line() {
      a = a,
      b = b
    });
  }

  public void SetColor(Color color) {
    _commands.Add(Command.SetColor);
    _colors.Add(color);
  }

  public void Replay(IRuntimeGizmoRenderer drawer) {
    int currMatrix = 0;
    int currColor = 0;
    int currMesh = 0;
    int currLine = 0;

    foreach (var command in _commands) {
      switch (command) {
        case Command.DrawWireMesh:
          drawer.DrawWireMesh(_meshes[currMesh++], _matrices[currMatrix++]);
          break;
        case Command.DrawMesh:
          drawer.DrawMesh(_meshes[currMesh++], _matrices[currMatrix++]);
          break;
        case Command.DrawLine:
          var line = _lines[currLine++];
          drawer.DrawLine(line.a, line.b);
          break;
        case Command.SetColor:
          drawer.SetColor(_colors[currColor++]);
          break;
      }
    }
  }

  public enum Command {
    DrawWireMesh,
    DrawMesh,
    DrawLine,
    SetColor
  }

  private struct Line {
    public Vector3 a, b;
  }
}

public interface IRuntimeGizmoRenderer {
  void SetTarget(MonoBehaviour target);
  void SetColor(Color color);
  void DrawWireMesh(Mesh mesh, Matrix4x4 matrix);
  void DrawMesh(Mesh mesh, Matrix4x4 matrix);
  void DrawLine(Vector3 a, Vector3 b);
}
