using UnityEngine;
using Leap.Unity;

public class RawTextureVisualizer : MonoBehaviour {

  public LeapImageRetriever imageRetriever;
	void Update () {
    var renderer = GetComponent<Renderer>();
    if (renderer != null) {
      if (imageRetriever.TextureData != null && renderer.sharedMaterial.mainTexture != imageRetriever.TextureData.TextureData.CombinedTexture) {
        GetComponent<Renderer>().sharedMaterial.mainTexture = imageRetriever.TextureData.TextureData.CombinedTexture;
      }
    }
    
  }

}
