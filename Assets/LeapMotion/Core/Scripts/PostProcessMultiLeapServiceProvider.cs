/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/


namespace Leap.Unity {

  /// <summary>
  /// The LeapServiceProvider provides tracked Leap Hand data and images from the device
  /// via the Leap service running on the client machine.
  /// </summary>
  public class PostProcessMultiLeapServiceProvider : PostProcessProvider {

    public MultideviceAlignment alignment;

    public override void ProcessFrame(ref Frame inputFrame)
    {
      if (alignment != null && alignment.computeHand)
      {
        Hand left = inputFrame.Get(Chirality.Left);
        alignment.MergeHands(ref left);
        Hand right = inputFrame.Get(Chirality.Right);
        alignment.MergeHands(ref right);
      }
    }
  }

}
