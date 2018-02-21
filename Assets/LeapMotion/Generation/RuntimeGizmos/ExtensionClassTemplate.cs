using System;
using UnityEngine;

namespace Leap.Unity.RuntimeGizmos.Generation {

  public static class MonoBehaviourGizmoExtensions {
    //INSERT
    //Short method for making code smaller
    private static RuntimeGizmoDrawer drawer(MonoBehaviour target) {
      return RuntimeGizmoManager.instance.GetDrawer(target);
    }
  }
}
