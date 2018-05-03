using UnityEngine;
using UnityEngine.Networking;

namespace Leap.Unity.Networking.Examples {

  /// <summary>
  /// A basic networking utility implementation for discovering peers across the network.
  /// </summary>
  public class LeapNetworkDiscovery : NetworkDiscovery {

    bool isServer = false; // TODO: DELETEME
    public int port = 7777;
    NetworkManager networkManager;
    
    private void Start() {
      isServer = ServerState.isServer;
      networkManager = NetworkManager.singleton;

      Initialize();

      if (!isServer) {
        StartAsClient();
        // See OnReceivedBroadcast for the StartClient() call.
      } else {
        StartAsServer();
        networkManager.StartHost();
      }
      if (base.isServer) {
        enabled = false;
      }
    }

    public override void OnReceivedBroadcast(string fromAddress, string data) {
      base.OnReceivedBroadcast(fromAddress, data);

      if (NetworkManager.singleton != null && NetworkManager.singleton.client == null) {
        Debug.Log(fromAddress + "/" + data);

        NetworkManager.singleton.networkAddress = fromAddress.Remove(0, 7);
        NetworkManager.singleton.networkPort = port;// Convert.ToInt32(data);
        NetworkManager.singleton.StartClient();
        
        if (!base.isServer) {
          enabled = false;
        }
      }
    }
  }
}