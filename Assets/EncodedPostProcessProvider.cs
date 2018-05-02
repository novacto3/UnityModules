using Leap;
using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.Encoding;
using Leap.Unity.Query;
using UnityEngine;

public class EncodedPostProcessProvider : PostProcessProvider {

  [ImplementsTypeNameDropdown(typeof(IByteEncodable<Hand>))]
  [OnEditorChange("encodingType")]
  [SerializeField]
  private string _encodingType;
  public string encodingType {
    get { return _encodingType; }
    set {
      _encodingType = value;
      _backingLEncodedHand = null;
      _backingREncodedHand = null;
    }
  }

  private IByteEncodable<Hand> _backingLEncodedHand;
  private IByteEncodable<Hand> _lEncodedHand {
    get {
      if (_backingLEncodedHand == null) {
        _backingLEncodedHand = System.Type.GetType(encodingType)
          .GetConstructor(new System.Type[] { }).Invoke(null) as IByteEncodable<Hand>;
      }
      return _backingLEncodedHand;
    }
  }

  private IByteEncodable<Hand> _backingREncodedHand;
  private IByteEncodable<Hand> _rEncodedHand {
    get {
      if (_backingREncodedHand == null) {
        _backingREncodedHand = System.Type.GetType(encodingType)
          .GetConstructor(new System.Type[] { }).Invoke(null) as IByteEncodable<Hand>;
      }
      return _backingREncodedHand;
    }
  }

  public override void ProcessFrame(ref Frame inputFrame) {
    var leftHand = inputFrame.Hands.Query().FirstOrDefault(h => h.IsLeft);
    var rightHand = inputFrame.Hands.Query().FirstOrDefault(h => !h.IsLeft);

    if (leftHand != null) {
      _lEncodedHand.Encode(leftHand);
      _lEncodedHand.Decode(leftHand);
    }

    if (rightHand != null) {
      _rEncodedHand.Encode(rightHand);
      _rEncodedHand.Decode(rightHand);
    }
  }

}
