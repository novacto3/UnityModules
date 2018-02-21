using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using UnityEngine.SceneManagement;

namespace Leap.Unity.RuntimeGizmos {
  using Internal;

  [ExecuteInEditMode]
  public class RuntimeGizmoManager : MonoBehaviour {
    public const string DEFAULT_SHADER_NAME = "Hidden/Runtime Gizmos";

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
    protected bool _enabledForBuild = true;

    [SerializeField]
    private Mesh _sphereMesh;

    [SerializeField]
    private Shader _gizmoShader;

    private Stack<GizmoBuffer> _bufferPool = new Stack<GizmoBuffer>();

    private bool _canSwap = true;
    private BufferGroup _currBufferGroup = new BufferGroup();
    private BufferGroup _prevBufferGroup = new BufferGroup();

    private GizmoMeshes _cachedMeshes;
    private GizmoMeshes _meshes {
      get {
        if (_cachedMeshes == null) {
          _cachedMeshes = new GizmoMeshes(_sphereMesh);
        }
        return _cachedMeshes;
      }
    }

    private RuntimeGizmoDrawer _cachedDrawer;
    private RuntimeGizmoDrawer _drawer {
      get {
        if (_cachedDrawer == null) {
          _cachedDrawer = new RuntimeGizmoDrawer(_meshes);
        }
        return _cachedDrawer;
      }
    }

    private GizmoRendererCamera _cachedCameraRenderer;
    private GizmoRendererCamera _cameraRenderer {
      get {
        if (_cachedCameraRenderer == null) {
          if (_gizmoShader == null) {
            _gizmoShader = Shader.Find(DEFAULT_SHADER_NAME);
          }

          _cachedCameraRenderer = new GizmoRendererCamera(_gizmoShader);
        }
        return _cachedCameraRenderer;
      }
    }

    public RuntimeGizmoDrawer GetDrawer(MonoBehaviour target) {
      if (target == null) {
        target = this;
      }

      GizmoBuffer buffer;
      if (!_currBufferGroup.contextMap.TryGetValue(target, out buffer)) {
        if (_bufferPool.Count > 0) {
          buffer = _bufferPool.Pop();
        } else {
          buffer = new GizmoBuffer();
        }
        _currBufferGroup.contextMap[target] = buffer;
      }

      _drawer.buffer = buffer;
      _drawer.Reset();
      return _drawer;
    }

    private void OnEnable() {
#if !UNITY_EDITOR
      if (!_enabledForBuild) {
        enabled = false;
      }
#endif

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
          obj.GetComponentsInChildren(includeInactive: false, results: _gizmoList);
          foreach (var gizmoComponent in _gizmoList) {
            var drawer = GetDrawer(gizmoComponent as MonoBehaviour);

            try {
              gizmoComponent.OnDrawRuntimeGizmos(drawer);
            } catch (Exception e) {
              Debug.LogException(e);
            }
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

      if ((camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR) &&
          _displayInGame) {
        _prevBufferGroup.Render(_cameraRenderer);
      }
    }

    private class BufferGroup {
      public Dictionary<MonoBehaviour, GizmoBuffer> contextMap = new Dictionary<MonoBehaviour, GizmoBuffer>();

      public void Clear(Stack<GizmoBuffer> pool) {
        foreach (var pair in contextMap) {
          pair.Value.Clear();
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
}
