using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.RuntimeGizmos;

[ExecuteInEditMode]
public class TestScript : MonoBehaviour {

  private void Update() {
    this.DrawLine(Vector3.zero, transform.position);
    this.DrawWireSphere(transform.position, 1.0f);
    this.DrawSphere(Vector3.zero, 1.0f);
  }
}
