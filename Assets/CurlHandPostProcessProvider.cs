using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;
using Leap.Unity.Query;

public class CurlHandPostProcessProvider : PostProcessProvider {

  CurlHand lCurlHand = new CurlHand();
  CurlHand rCurlHand = new CurlHand();

  public override void ProcessFrame(ref Frame inputFrame) {
    var leftHand = inputFrame.Hands.Query().FirstOrDefault(h => h.IsLeft);
    var rightHand = inputFrame.Hands.Query().FirstOrDefault(h => !h.IsLeft);

    if (leftHand != null) {
      lCurlHand.Encode(leftHand);
      lCurlHand.Decode(leftHand);
    }

    if (rightHand != null) {
      rCurlHand.Encode(rightHand);
      rCurlHand.Decode(rightHand);
    }
  }

}
