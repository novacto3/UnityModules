/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {
  public class ReprojectHandUVs : MonoBehaviour {
    public LeapImageRetriever imageRetriever;
    public LeapXRServiceProvider provider;
    Mesh _mesh;
    Vector3[] _vertices;
    //Vector3[] normals;
    Vector2[] _uvs;
    SkinnedMeshRenderer _skin;
    GameObject _rightCamera;

    void Start() {
      _mesh = new Mesh();
      _mesh.MarkDynamic();
      _skin = GetComponent<SkinnedMeshRenderer>();
      _skin.BakeMesh(_mesh);
      _vertices = _mesh.vertices;
      _uvs = new Vector2[_vertices.Length];
      _rightCamera = new GameObject("RightCamera");
      _rightCamera.transform.SetParent(provider.transform);
    }

    void Update() {
      _rightCamera.transform.localPosition = new Vector3(0.02f, provider.deviceOffsetYAxis, provider.deviceOffsetZAxis);
      _rightCamera.transform.localRotation = Quaternion.Euler(provider.deviceTiltXAxis - 3, 0f, 0f);

      _skin.BakeMesh(_mesh);
      _vertices = _mesh.vertices;
      //normals = mesh.normals;
      for (int i = 0; i < _uvs.Length; i++) {
        //if (Vector3.Dot(_provider.transform.TransformDirection(normals[i]), LeftCamera.forward) < 0.7f) {
          Vector3 CameraToPointRay = _rightCamera.transform.InverseTransformPoint(transform.TransformPoint(_vertices[i]));
          CameraToPointRay /= CameraToPointRay.z;
          Vector ImagePoint = Image.RectilinearToPixel(Image.CameraType.RIGHT, new Vector(CameraToPointRay.x, CameraToPointRay.y, 1f));
          ImagePoint = new Vector(ImagePoint.x / 640f, ImagePoint.y / 240f, 0f);
          _uvs[i].Set(ImagePoint.x, 1f - (ImagePoint.y / 2f));
        //} else {
        //    uvs[i].Set(0f, 0f);
        //}
      }
      _skin.sharedMesh.uv = _uvs;
    }
  }
}
