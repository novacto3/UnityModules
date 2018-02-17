using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using UnityEngine.SceneManagement;

public class RuntimeGizmoManager : MonoBehaviour {
  public const string DEFAULT_SHADER_NAME = "Hidden/Runtime Gizmos";
  public const int CIRCLE_RESOLUTION = 32;

  private static RuntimeGizmoManager _cachedInstance;
  public static RuntimeGizmoManager instance {
    get {
      if (_cachedInstance == null) {
        _cachedInstance = FindObjectOfType<RuntimeGizmoManager>();
        if (_cachedInstance == null) {
          _cachedInstance = new GameObject("__RuntimeGizmoManager__").AddComponent<RuntimeGizmoManager>();
        }
      }

      return _cachedInstance;
    }
  }

  [SerializeField]
  private bool _displayInGame = true;

  [SerializeField]
  private bool _enabledForBuild = true;

  [SerializeField]
  private Mesh _sphereMesh;

  [SerializeField]
  private Shader _gizmoShader;

  //Public rendering data
  public Mesh wireCubeMesh { get; private set; }
  public Mesh filledCubeMesh { get; private set; }
  public Mesh wireSphereMesh { get; private set; }
  public Mesh filledSphereMesh { get; private set; }

  private Stack<GizmoBuffer> _bufferPool = new Stack<GizmoBuffer>();

  private bool _canSwap = true;
  private BufferGroup _currBufferGroup;
  private BufferGroup _prevBufferGroup;

  private RuntimeGizmoDrawer _drawer;

  private GizmoRendererCamera _cameraRenderer;

  public RuntimeGizmoDrawer GetDrawer(MonoBehaviour target) {
    if (target == null) {
      target = this;
    }

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

  private void Awake() {
    if (_gizmoShader == null) {
      _gizmoShader = Shader.Find(DEFAULT_SHADER_NAME);
    }

    generateMeshes();

    _cameraRenderer = new GizmoRendererCamera(_gizmoShader);
  }

  private void OnEnable() {
    Camera.onPostRender += onPostRender;
  }

  private void OnDisable() {
    Camera.onPostRender -= onPostRender;
  }

  private List<GameObject> _objList = new List<GameObject>();
  private List<IRuntimeGizmoComponent> _gizmoList = new List<IRuntimeGizmoComponent>();
  private void LateUpdate() {
    _canSwap = true;

    for (int i = 0; i < SceneManager.sceneCount; i++) {
      var scene = SceneManager.GetSceneAt(i);
      scene.GetRootGameObjects(_objList);
      foreach (var obj in _objList) {
        obj.GetComponentsInChildren(includeInactive: false, _gizmoList);
        foreach (var gizmoComponent in _gizmoList) {

        }
      }
    }
  }

  private void onPostRender(Camera camera) {
    //Completely ignore preview and reflection cameras
    if (camera.cameraType == CameraType.Preview ||
        camera.cameraType == CameraType.Reflection) {
      return;
    }

    if (_canSwap) {
      //Swap curr for prev
      Utils.Swap(ref _currBufferGroup, ref _prevBufferGroup);
      _canSwap = false;

      //Clear out the curr pool so it can start being drawn to right away
      _currBufferGroup.Clear(_bufferPool);

#if UNITY_EDITOR
      //Create and assign the hidden renderers
      foreach (var pair in _prevBufferGroup.contextMap) {
        var hiddenRenderer = pair.Key.GetComponent<HiddenGizmoRendererComponent>();
        if (hiddenRenderer == null) {
          hiddenRenderer = pair.Key.gameObject.AddComponent<HiddenGizmoRendererComponent>();
        }

        hiddenRenderer.buffer = pair.Value;
      }
#endif
    }

    if (camera.cameraType == CameraType.Game ||
        camera.cameraType == CameraType.VR) {
      _prevBufferGroup.Render(_cameraRenderer);
    }
  }

  #region MESH GENERATION
  private void generateMeshes() {
    filledCubeMesh = new Mesh();
    filledCubeMesh.name = "RuntimeGizmoCube";
    filledCubeMesh.hideFlags = HideFlags.HideAndDontSave;

    List<Vector3> verts = new List<Vector3>();
    List<int> indexes = new List<int>();

    Vector3[] faces = new Vector3[] { Vector3.forward, Vector3.right, Vector3.up };
    for (int i = 0; i < 3; i++) {
      addQuad(verts, indexes, faces[(i + 0) % 3], -faces[(i + 1) % 3], faces[(i + 2) % 3]);
      addQuad(verts, indexes, -faces[(i + 0) % 3], faces[(i + 1) % 3], faces[(i + 2) % 3]);
    }

    filledCubeMesh.SetVertices(verts);
    filledCubeMesh.SetIndices(indexes.ToArray(), MeshTopology.Quads, 0);
    filledCubeMesh.RecalculateNormals();
    filledCubeMesh.RecalculateBounds();
    filledCubeMesh.UploadMeshData(true);

    wireCubeMesh = new Mesh();
    wireCubeMesh.name = "RuntimeWireCubeMesh";
    wireCubeMesh.hideFlags = HideFlags.HideAndDontSave;

    verts.Clear();
    indexes.Clear();

    for (int dx = 1; dx >= -1; dx -= 2) {
      for (int dy = 1; dy >= -1; dy -= 2) {
        for (int dz = 1; dz >= -1; dz -= 2) {
          verts.Add(0.5f * new Vector3(dx, dy, dz));
        }
      }
    }

    addCorner(indexes, 0, 1, 2, 4);
    addCorner(indexes, 3, 1, 2, 7);
    addCorner(indexes, 5, 1, 4, 7);
    addCorner(indexes, 6, 2, 4, 7);

    wireCubeMesh.SetVertices(verts);
    wireCubeMesh.SetIndices(indexes.ToArray(), MeshTopology.Lines, 0);
    wireCubeMesh.RecalculateBounds();
    wireCubeMesh.UploadMeshData(true);

    wireSphereMesh = new Mesh();
    wireSphereMesh.name = "RuntimeWireSphereMesh";
    wireSphereMesh.hideFlags = HideFlags.HideAndDontSave;

    verts.Clear();
    indexes.Clear();

    int totalVerts = CIRCLE_RESOLUTION * 3;
    for (int i = 0; i < CIRCLE_RESOLUTION; i++) {
      float angle = Mathf.PI * 2 * i / CIRCLE_RESOLUTION;
      float dx = 0.5f * Mathf.Cos(angle);
      float dy = 0.5f * Mathf.Sin(angle);

      for (int j = 0; j < 3; j++) {
        indexes.Add((i * 3 + j + 0) % totalVerts);
        indexes.Add((i * 3 + j + 3) % totalVerts);
      }

      verts.Add(new Vector3(dx, dy, 0));
      verts.Add(new Vector3(0, dx, dy));
      verts.Add(new Vector3(dx, 0, dy));
    }

    wireSphereMesh.SetVertices(verts);
    wireSphereMesh.SetIndices(indexes.ToArray(), MeshTopology.Lines, 0);
    wireSphereMesh.RecalculateBounds();
    wireSphereMesh.UploadMeshData(true);
  }

  private void addQuad(List<Vector3> verts, List<int> indexes, Vector3 normal, Vector3 axis1, Vector3 axis2) {
    indexes.Add(verts.Count + 0);
    indexes.Add(verts.Count + 1);
    indexes.Add(verts.Count + 2);
    indexes.Add(verts.Count + 3);

    verts.Add(0.5f * (normal + axis1 + axis2));
    verts.Add(0.5f * (normal + axis1 - axis2));
    verts.Add(0.5f * (normal - axis1 - axis2));
    verts.Add(0.5f * (normal - axis1 + axis2));
  }

  private void addCorner(List<int> indexes, int a, int b, int c, int d) {
    indexes.Add(a); indexes.Add(b);
    indexes.Add(a); indexes.Add(c);
    indexes.Add(a); indexes.Add(d);
  }
  #endregion

  private class BufferGroup {
    public Dictionary<MonoBehaviour, GizmoBuffer> contextMap = new Dictionary<MonoBehaviour, GizmoBuffer>();

    public void Clear(Stack<GizmoBuffer> pool) {
      foreach (var pair in contextMap) {
        pool.Push(pair.Value);
      }

      contextMap.Clear();
    }

    public void Render(IRuntimeGizmoRenderer renderer) {
      foreach (var pair in contextMap) {
        pair.Value.Replay(renderer);
      }
    }
  }
}
