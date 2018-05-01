using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Encoding {
  
  public interface IEncodable<T> {

    void Encode(T from);

    void Decode(T into);

  }

  public interface IByteEncodable<T> : IEncodable<T> {

    int numBytesRequired { get; }

    void ReadBytes(byte[] bytes, ref int offset);

    void FillBytes(byte[] bytes, ref int offset);

  }

  public static class EncodableExtensions {

    #region ReadBytes

    /// <summary>
    /// Reads the provided byte array starting from index 0, decoding the data into this
    /// encoded representation.
    /// 
    /// The provided bytes array must have a size equal to or greater than
    /// numBytesRequired.
    /// </summary>
    public static void ReadBytes<T>(this IByteEncodable<T> bEncodable, byte[] bytes) {
      int unusedOffset = 0;
      bEncodable.ReadBytes(bytes, ref unusedOffset);
    }

    /// <summary>
    /// Shortcut for reading a byte representation produced by a FillBytes method from
    /// this encoding type, beginning at index 0, and immediately filling the type
    /// argument object with the decoded data.
    /// 
    /// The provided bytes array must have a size equal to or greater than
    /// numBytesRequired.
    /// </summary>
    public static void ReadBytes<T>(this IByteEncodable<T> bEncodable, byte[] bytes,
                                    T into) {
      bEncodable.ReadBytes(bytes);
      bEncodable.Decode(into);
    }

    /// <summary>
    /// Shortcut for reading a byte representation produced by a FillBytes method from
    /// this encoding type and immediately filling the type argument object with the
    /// decoded data.
    /// 
    /// The provided bytes array must have a size equal to or greater than
    /// numBytesRequired past the offset index.
    /// </summary>
    public static void ReadBytes<T>(this IByteEncodable<T> bEncodable, byte[] bytes,
                                    ref int offset, T into) {
      bEncodable.ReadBytes(bytes, ref offset);
      bEncodable.Decode(into);
    }

    #endregion

    #region FillBytes

    /// <summary>
    /// Fills the provided byte array, starting from index 0, with the data contained
    /// in this encoded representation.
    /// 
    /// Use numBytesRequired to retrieve the required minimum length of the passed byte
    /// array.
    /// </summary>
    public static void FillBytes<T>(this IByteEncodable<T> bEncodable, byte[] bytes) {
      int unusedOffset = 0;
      bEncodable.FillBytes(bytes, ref unusedOffset);
    }

    /// <summary>
    /// Shortcut for encoding an object into this encoding's representation and
    /// converting it immediately into a byte representation, starting at index 0 in the
    /// bytes array.
    /// 
    /// The provided bytes array must have a size equal to or greater than
    /// numBytesRequired.
    /// </summary>
    public static void FillBytes<T>(this IByteEncodable<T> bEncodable, byte[] bytes,
                                    T from) {
      bEncodable.Encode(from);
      bEncodable.FillBytes(bytes);
    }

    /// <summary>
    /// Shortcut for encoding an object into this encoding's representation and
    /// converting it immediately into a byte representation.
    /// 
    /// The provided bytes array must have a size equal to or greater than
    /// numBytesRequired past the offset index.
    /// </summary>
    public static void FillBytes<T>(this IByteEncodable<T> bEncodable, byte[] bytes,
                                    ref int offset, T from) {
      bEncodable.Encode(from);
      bEncodable.FillBytes(bytes, ref offset);
    }

    #endregion

  }

}
