using System;
using UnityEngine;

[Serializable]
public class JumpFlood {
  public const int PASS_INIT = 0;
  public const int PASS_JUMP = 1;

  public int steps = 12;

  private Material _material;

  private bool tryInitMaterial() {
    if (_material != null) {
      return true;
    }

    var shader = Shader.Find("Hidden/JumpFlood");
    if (shader == null) {
      return false;
    }

    _material = new Material(shader);
    _material.hideFlags = HideFlags.HideAndDontSave;
    _material.name = "Jump Flood Material";
    return true;
  }

  private RenderTexture getTemp(RenderTexture sourceTex) {
    var tex = RenderTexture.GetTemporary(sourceTex.width,
                                         sourceTex.height,
                                         0,
                                         RenderTextureFormat.ARGBFloat,
                                         RenderTextureReadWrite.Linear);
    tex.wrapMode = TextureWrapMode.Clamp;
    return tex;
  }

  public RenderTexture BuildDistanceField(RenderTexture sourceTex) {
    if (!tryInitMaterial()) {
      return null;
    }

    steps = Mathf.Clamp(steps, 0, 32);

    var tex0 = getTemp(sourceTex);
    var tex1 = getTemp(sourceTex);

    Graphics.Blit(sourceTex, tex0, _material, PASS_INIT);

    int step = Mathf.RoundToInt(Mathf.Pow(steps - 1, 2));
    while (step != 0) {
      _material.SetFloat("_Step", step);
      Graphics.Blit(tex0, tex1, _material, PASS_JUMP);

      var tmp = tex0;
      tex0 = tex1;
      tex1 = tmp;

      step /= 2;
    }

    RenderTexture.ReleaseTemporary(tex1);
    return tex0;
  }
}
