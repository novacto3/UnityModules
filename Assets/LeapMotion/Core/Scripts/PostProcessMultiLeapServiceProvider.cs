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
  using Attributes;
  using System.Collections.Generic;

  /// <summary>
  /// The LeapServiceProvider provides tracked Leap Hand data and images from the device
  /// via the Leap service running on the client machine.
  /// </summary>
  public class PostProcessMultiLeapServiceProvider : PostProcessProvider {

    public MultideviceAlignment alignment;

    public override void ProcessFrame(ref Frame inputFrame)
    {
      if (alignment != null)
      {
        Hand left = inputFrame.Get(Chirality.Left);
        alignment.ComputeCenterHandPrecise2(ref left);
        Hand right = inputFrame.Get(Chirality.Right);
        alignment.ComputeCenterHandPrecise2(ref right);
      }
    }
  }

}
