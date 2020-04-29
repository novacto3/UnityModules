/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.Packaging {

  public class BuildDefinitionBuildMenuItems { 

    // Text Experiment
    [MenuItem("Build/Text Experiment", priority = 20)]
    public static void Build_541d1627636de0e4f8c7b8f8bedeea93() {
      BuildDefinition.BuildFromGUID("541d1627636de0e4f8c7b8f8bedeea93");
    }
  }
}

