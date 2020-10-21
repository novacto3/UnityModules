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
      if (alignment != null && alignment.getMergeHands())
      {
        //ProcessHand(ref inputFrame, Chirality.Left);
        ProcessHand(ref inputFrame, Chirality.Right);
      }
    }

    private void ProcessHand(ref Frame inputFrame, Chirality chirality)
    {
      bool artificital = false;
      Hand hand = inputFrame.Get(chirality);
      if (hand == null)
      {
        hand = new Hand
        {
          IsLeft = chirality == Chirality.Left,
          FrameId = inputFrame.Id
        };
        hand.Id = hand.IsLeft ? 1 : 2;
        artificital = true;
      }

      alignment.MergeHands(ref hand, chirality);

      if (artificital && hand != null)
      {
        inputFrame.Hands.Add(hand);
      }
      if (!artificital && hand == null)
      {
        inputFrame.Hands.Remove(inputFrame.Get(chirality));
      }
    }
  }
}
