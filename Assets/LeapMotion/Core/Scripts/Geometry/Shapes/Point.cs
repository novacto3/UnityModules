/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {

  [System.Serializable]
  public struct Point {

    [SerializeField]
    public Transform transform;

    [SerializeField]
    private Vector3 _position;
    public Vector3 position {
      get {
        if (transform == null) return _position;
        else return transform.TransformPoint(_position);
      }
      set {
        if (transform == null) _position = value;
        else _position = transform.InverseTransformPoint(value);
      }
    }

    public Point(Component transformSource = null)
      : this(default(Vector3), transformSource) { }

    public Point(Vector3 position = default(Vector3), Component transformSource = null) {
      this.transform = transformSource.transform;
      _position = Vector3.zero;
    }

    public static implicit operator Vector3(Point point) {
      return point.position;
    }

  }

  public static class PointExtensions {



  }

}
