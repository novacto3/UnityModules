using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Encoding {

  [Serializable]
  public class CurlHand : IByteEncodable<Hand> {

    #region Data

    public bool isLeft;
    public Pose palmPose;

    [SerializeField]
    private byte[] _backingCurlBytes;
    private byte[] _curlBytes {
      get {
        if (_backingCurlBytes == null || _backingCurlBytes.Length != numBytesRequired) {
          _backingCurlBytes = new byte[numBytesRequired];
        }
        return _curlBytes;
      }
    }

    #endregion

    #region Constructors

    public CurlHand() { }

    /// <summary>
    /// Constructs a CurlHand representation from a Leap hand. This allocates a byte
    /// array for the encoded hand data.
    /// 
    /// Use a pooling strategy to avoid unnecessary allocation in runtime contexts.
    /// </summary>
    public CurlHand(Hand hand) : this() {
      Encode(hand);
    }

    #endregion

    #region IEncodeable<Hand>

    public void Decode(Hand intoHand) {

    }

    public void Encode(Hand fromHand) {

    }

    #endregion

    #region IByteEncodable<Hand>

    public int numBytesRequired {
      get {
        throw new System.NotImplementedException();
      }
    }

    public void FillBytes(byte[] bytesToFill, ref int offset) {
      throw new System.NotImplementedException();
    }

    public void ReadBytes(byte[] bytes, ref int offset) {
      throw new System.NotImplementedException();
    }

    #endregion

  }

}
