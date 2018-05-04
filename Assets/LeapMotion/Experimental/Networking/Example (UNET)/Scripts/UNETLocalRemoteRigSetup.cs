using UnityEngine;
using UnityEngine.Networking;

namespace Leap.Unity.Networking.Examples {

  public class UNETLocalRemoteRigSetup : NetworkBehaviour {
    
    public HandModelManager handModelManager;
    public Camera rigCamera;
    
    public Renderer[] disableRenderersIfLocal;

    private void Start() {
      if (isLocalPlayer) {
        setupRigLocal();
      }
      else {
        setupRigRemote();
      }
    }

    private void setupRigLocal() {
      // To set up a local rig, just add a standard XR service provider to the camera
      // and attach it to the Hand Model Manager.

      // If we had other objects that expected a LeapProvider, we could also set up those
      // references here.

      var provider = rigCamera.gameObject.AddComponent<LeapXRServiceProvider>();
      handModelManager.leapProvider = provider;

      // We also disable some local renderers on this prefab so that the player doesn't
      // see their own head!
      foreach (var renderer in disableRenderersIfLocal) {
        renderer.enabled = false;
      }
    }

    private void setupRigRemote() {


      //var provider = rigCamera.gameObject.AddComponent<LeapUNETStreamProvider>
    }

  }


}
