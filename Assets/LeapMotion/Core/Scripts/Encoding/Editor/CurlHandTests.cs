/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Linq;
using NUnit.Framework;

namespace Leap.Unity.Tests {
  using Encoding;

  public class CurlHandTests {

    [Test]
    public void EncodeDecodeTest() {
      const float TOLERANCE = 0.01f; //1 cm for all positions

      Frame frame = TestHandFactory.MakeTestFrame(0, includeLeftHand: true,
        includeRightHand: true, unitType: TestHandFactory.UnitType.UnityUnits);

      foreach (var hand in frame.Hands) {

        byte[] bytes;
        {
          CurlHand cHand = new CurlHand();
          bytes = new byte[cHand.numBytesRequired];

          //Encode the hand into the vHand representation
          cHand.Encode(hand);

          //Then convert the vHand into a binary representation
          cHand.FillBytes(bytes);
        }

        Hand result;
        {
          CurlHand cHand = new CurlHand();

          //Convert the binary representation back into a vHand
          int offset = 0;
          cHand.ReadBytes(bytes, ref offset);

          //Decode the vHand back into a normal Leap Hand
          result = new Hand();
          cHand.Decode(result);
        }

        Assert.That(result.IsLeft, Is.EqualTo(hand.IsLeft));
        Assert.That((result.PalmPosition - hand.PalmPosition).Magnitude,
          Is.LessThan(TOLERANCE));

        foreach (var resultFinger in result.Fingers) {
          var finger = hand.Fingers.Single(f => f.Type == resultFinger.Type);

          for (int i = 0; i < 4; i++) {
            Bone resultBone = resultFinger.bones[i];
            Bone bone = finger.bones[i];

            Assert.That((resultBone.NextJoint - bone.NextJoint).Magnitude,
              Is.LessThan(TOLERANCE));
            Assert.That((resultBone.PrevJoint - bone.PrevJoint).Magnitude,
              Is.LessThan(TOLERANCE));
            Assert.That((resultBone.Center - bone.Center).Magnitude,
              Is.LessThan(TOLERANCE));
          }
        }
      }
    }

  }
}
