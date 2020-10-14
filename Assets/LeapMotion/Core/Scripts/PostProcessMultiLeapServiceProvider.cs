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
      inputFrame.Hands.Clear();
      if (alignment != null && alignment.virtualHands != null && alignment.virtualHands.Count > 0)
      {
        foreach (Hand hand in alignment.virtualHands)
        {
          inputFrame.Hands.Add(hand);
          //Debug.Log(hand.WristPosition);
        }
      }
    }
  }

}
