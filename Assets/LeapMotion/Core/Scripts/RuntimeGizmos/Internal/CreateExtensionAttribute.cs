using System;

namespace Leap.Unity.RuntimeGizmos.Internal {

  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
  public class CreateExtensionAttribute : Attribute { }
}
