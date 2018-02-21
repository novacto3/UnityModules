using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Leap.Unity.RuntimeGizmos.Generation {
  using Internal;
  using Leap.Unity.Generation;

  [CreateAssetMenu(menuName = "Generator/RuntimeGizmos", order = 900)]
  public class RuntimeGizmosExtensionGenerator : GeneratorBase {
    private const string INSERT_MARKER = "//INSERT";
    private const string SRC_NAMESPACE = "Leap.Unity.RuntimeGizmos.Generation";
    private const string DST_NAMESPACE = "Leap.Unity.RuntimeGizmos";

    public TextAsset template;
    public AssetFolder targetFolder;
    public string targetFilename;

    public override void Generate() {
      StringBuilder builder = new StringBuilder();

      using (var reader = new StringReader(template.text)) {
        while (true) {
          string line = reader.ReadLine();
          if (line == null) {
            break;
          }

          line = line.Replace(SRC_NAMESPACE, DST_NAMESPACE);

          if (line.Contains(INSERT_MARKER)) {
            string indent = new string(line.TakeWhile(char.IsWhiteSpace).ToArray());
            reflectProperties(builder, indent);
            reflectMethods(builder, indent);
          } else {
            builder.AppendLine(line);
          }
        }
      }

      File.WriteAllText(Path.Combine(targetFolder.Path, targetFilename), builder.ToString());
    }

    private void reflectProperties(StringBuilder builder, string indent) {
      var properties = typeof(RuntimeGizmoDrawer).
                       GetProperties(BindingFlags.Public | BindingFlags.Instance).
                       Where(p => p.GetCustomAttributes(typeof(CreateExtensionAttribute), inherit: true).Length > 0);
      foreach (var property in properties) {
        builder.Append(indent);
        writeMethodPrefix(builder, "Set" + Utils.Capitalize(property.Name));
        builder.Append(", " + property.PropertyType.Name + " value) {");
        builder.AppendLine();

        builder.Append(indent + "  ");
        builder.Append("drawer(target)." + property.Name + " = value;");
        builder.AppendLine();

        builder.Append(indent);
        builder.Append("}");
        builder.AppendLine();

        builder.AppendLine();
      }
    }

    private void reflectMethods(StringBuilder builder, string indent) {
      var methods = typeof(RuntimeGizmoDrawer).
                    GetMethods(BindingFlags.Public | BindingFlags.Instance).
                    Where(p => p.GetCustomAttributes(typeof(CreateExtensionAttribute), inherit: true).Length > 0);
      foreach (var method in methods) {
        var args = method.GetParameters();

        builder.Append(indent);
        writeMethodPrefix(builder, method.Name);
        builder.Append(string.Concat(args.Select(getParamString).ToArray()));
        builder.Append(") {");
        builder.AppendLine();

        builder.Append(indent + "  ");
        builder.Append("drawer(target)." + method.Name + "(");
        builder.Append(string.Join(", ", method.GetParameters().Select(p => p.Name).ToArray()));
        builder.Append(");");
        builder.AppendLine();

        builder.Append(indent);
        builder.Append("}");
        builder.AppendLine();

        builder.AppendLine();
      }
    }

    private void writeMethodPrefix(StringBuilder builder, string methodName) {
      builder.Append("public static void " + methodName + "(this MonoBehaviour target");
    }

    private string getParamString(ParameterInfo param) {
      string result = param.ParameterType.Name + " " + param.Name;
      if (param.RawDefaultValue != DBNull.Value) {
        result = result + " = ";
        if (param.RawDefaultValue == null) {
          result = result + "null";
        } else if (param.RawDefaultValue is float || param.RawDefaultValue is int) {
          result = result + param.RawDefaultValue.ToString();
        } else if (param.RawDefaultValue is bool) {
          result = result + (((bool)param.RawDefaultValue) ? "true" : "false");
        } else {
          throw new InvalidOperationException("Cannot ToString default value of type " + param.RawDefaultValue);
        }
      }
      return ", " + result;
    }
  }
}
