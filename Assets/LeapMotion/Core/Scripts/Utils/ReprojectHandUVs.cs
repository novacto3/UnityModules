/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity {
  public class ReprojectHandUVs : MonoBehaviour {
    public LeapImageRetriever imageRetriever;
    public LeapXRServiceProvider provider;
    public enum DeviceType { Peripheral, Rigel, Custom };
    public DeviceType device = DeviceType.Peripheral;
    public float customBaseline = 0.064f;
    public Vector2 customResolution = new Vector2(800, 800);
    Mesh _mesh;
    List<Vector3> _vertices;
    //List<Vector3> _normals;
    List<Vector2> _uvs;
    SkinnedMeshRenderer _skin;
    GameObject _rightCamera;

    void Start() {
      _mesh = new Mesh();
      _mesh.MarkDynamic();
      _skin = GetComponent<SkinnedMeshRenderer>();
      _skin.BakeMesh(_mesh);
      _vertices = new List<Vector3>(_mesh.vertexCount);
      _mesh.GetVertices(_vertices);
      _uvs = new List<Vector2>(_mesh.vertexCount);
      _mesh.GetUVs(0, _uvs);
      _rightCamera = new GameObject("RightCamera");
      _rightCamera.transform.SetParent(provider.transform);
    }

    void Update() {
      float halfBaseline;
      if (provider.GetLeapController() != null && 
          provider.GetLeapController().Devices != null && 
          provider.GetLeapController().Devices.Count > 0 && 
          device != DeviceType.Custom) {
        halfBaseline = provider.GetLeapController().Devices[0].Baseline * 0.0005f;
      } else {
        halfBaseline = (device == DeviceType.Peripheral ? 0.02f : (device == DeviceType.Rigel ? 0.032f : customBaseline * 0.5f));
      }
      _rightCamera.transform.localPosition = new Vector3(halfBaseline, provider.deviceOffsetYAxis, provider.deviceOffsetZAxis);
      _rightCamera.transform.localRotation = Quaternion.Euler(provider.deviceTiltXAxis - 3, 0f, 0f);

      _skin.BakeMesh(_mesh);
      _mesh.GetVertices(_vertices);
      //_mesh.GetNormals(_normals);
      LeapInternal.Connection connection = LeapInternal.Connection.GetConnection();
      if (connection != null) {
        for (int i = 0; i < _uvs.Count; i++) {
          //if (Vector3.Dot(_provider.transform.TransformDirection(normals[i]), LeftCamera.forward) < 0.7f) {
              Vector3 CameraToPointRay = _rightCamera.transform.InverseTransformPoint(transform.TransformPoint(_vertices[i]));
              CameraToPointRay /= CameraToPointRay.z;
              Vector ImagePoint = Image.RectilinearToPixel(Image.CameraType.RIGHT, new Vector(CameraToPointRay.x, CameraToPointRay.y, 1f), connection);

              if (device != DeviceType.Custom) {
                ImagePoint = new Vector(ImagePoint.x / imageRetriever.TextureData.TextureData.CombinedTexture.width,
                                        ImagePoint.y / (imageRetriever.TextureData.TextureData.CombinedTexture.height * 0.5f), 0f);
              } else {
                ImagePoint = new Vector(ImagePoint.x / (device == DeviceType.Peripheral ? 640f : (device == DeviceType.Rigel ? 384f : customResolution.x)),
                                        ImagePoint.y / (device == DeviceType.Peripheral ? 240f : (device == DeviceType.Rigel ? 384f : customResolution.y)), 0f);
              }

              _uvs[i] = new Vector2(ImagePoint.x, 1f - (ImagePoint.y / 2f));
          //} else {
          //    _uvs[i] = new Vector2((0f, 0f);
          //}
        }
      }
      _skin.sharedMesh.SetUVs(0, _uvs);
    }
  }
}
