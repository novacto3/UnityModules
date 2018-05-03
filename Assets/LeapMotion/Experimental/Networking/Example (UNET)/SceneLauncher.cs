using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity.Networking.Examples {

  /// <summary>
  /// MonoBehaviour enabling a launcher scene to attempt to begin a network session as
  /// the server or as the client.
  /// </summary>
  public class SceneLauncher : MonoBehaviour {

    public string SceneName = "Leap UNET Example Multiplayer Scene";

    public void StartServer() {
      ServerState.isServer = true;
      SceneManager.LoadScene(SceneName);
    }

    public void StartClient() {
      ServerState.isServer = false;
      SceneManager.LoadScene(SceneName);
    }

  }

  public static class ServerState {
    public static bool isServer = true;
  }

}
