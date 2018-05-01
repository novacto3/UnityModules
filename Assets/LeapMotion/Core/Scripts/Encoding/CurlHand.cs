using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Encoding {

  [Serializable]
  public class CurlHand : IByteEncodable<Hand> {

    public int numBytesRequired {
      get {
        throw new System.NotImplementedException();
      }
    }

    public void Decode(Hand intoHand) {
      throw new System.NotImplementedException();
    }

    public void Encode(Hand fromHand) {
      throw new System.NotImplementedException();
    }

    public void FillBytes(byte[] bytesToFill, ref int offset) {
      throw new System.NotImplementedException();
    }

    public void ReadBytes(byte[] bytes, ref int offset) {
      throw new System.NotImplementedException();
    }

  }

}
