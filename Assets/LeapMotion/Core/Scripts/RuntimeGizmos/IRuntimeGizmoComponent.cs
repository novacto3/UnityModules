
namespace Leap.Unity.RuntimeGizmos {

  /// <summary>
  /// Have your MonoBehaviour implement this interface to be able to draw runtime gizmos.
  /// You must also have a RuntimeGizmoManager component in the scene to recieve callbacks.
  /// </summary>
  public interface IRuntimeGizmoComponent {
    void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer);
  }
}
