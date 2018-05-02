using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Encoding {

  /// <summary>
  /// CurlHand is an extremely lightweight encoding for hands, requiring only 17 bytes.
  /// Palm position and rotation are reasonably high resolution, while pose data is
  /// heavily compressed.
  /// </summary>
  [Serializable]
  public class CurlHand : IByteEncodable<Hand> {

    #region Data

    /// <summary>
    /// Hand chirality.
    /// </summary>
    public bool isLeft;

    /// <summary>
    /// Camera-local palm pose.
    /// </summary>
    [Tooltip("Camera-local palm pose.")]
    public Pose palmPose;
    
    private const int NUM_FINGERS = 5;

    [SerializeField]
    [Tooltip("Compressed values indicating how curled each finger is.")]
    private byte[] _fingerCurls = null;
    /// <summary>
    /// Compressed values indicating how curled each finger is.
    /// </summary>
    public byte[] fingerCurls {
      get {
        if (_fingerCurls == null || _fingerCurls.Length != NUM_FINGERS) {
          _fingerCurls = new byte[NUM_FINGERS];
        }
        return _fingerCurls;
      }
    }
    
    /// <summary>
    /// Compressed average of finger spread (ignoring the thumb).
    /// </summary>
    [Tooltip("Compressed average of finger spread (ignoring the thumb).")]
    public byte fingerSpread = 0x00;

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

    public void Encode(Hand fromHand) {
      isLeft = fromHand.IsLeft;
      palmPose = fromHand.GetPalmPose();

      // Thumb curl.
      var thumbCurlAmount01
        = ((Vector3.Dot(fromHand.Basis.xBasis.ToVector3() * (isLeft ? -1f : 1f),
                        fromHand.Fingers[0].Direction.ToVector3())
            + 1f) / 2f);
      // The thumb, specifically, receives a fudge-factor of 0.1 that makes its range
      // match up better because xBasis doesn't quite match the desired 0-degree angle
      // for the thumb. This reduces our resolution slightly. TODO: The calculation can
      // be modified to avoid losing resolution, but it's a pretty small loss.
      fingerCurls[0] = (byte)(Mathf.Clamp01(thumbCurlAmount01 - 0.1f) * 255f);

      // Finger curl, which is per-finger, and spread, which is averaged.
      var spreadAmount = -0.5f;
      for (int i = 1; i < 5; i++) {
        var fingerCurlAmount01
          = ((Vector3.Dot(fromHand.Direction.ToVector3(),
                          fromHand.Fingers[i].Direction.ToVector3())
             + 1f) / 2f);
        fingerCurls[i] = (byte)(Mathf.Clamp01(1f - fingerCurlAmount01) * 255f);

        spreadAmount += Mathf.Abs((Quaternion.Inverse(fromHand.Rotation.ToQuaternion())
                             * fromHand.Fingers[i].Direction.ToVector3()).x);
      }

      fingerSpread = (byte)(Mathf.Clamp01(spreadAmount) * 255f); // Spread byte.
    }

    public void Decode(Hand intoHand) {
      Vector3 prevJoint, nextJoint;
      Quaternion boneRot = Quaternion.identity;

      // Warnin' y'all, we're takin' a trip down to Magic Number Town!
      // These values reflect incremental tweaking to make the output of curl hand 
      // decompression match the input well and look reasonably convincing.
      for (int f = 0; f < NUM_FINGERS; f++) {
        prevJoint = Vector3.zero;
        nextJoint = Vector3.zero;

        for (int j = 0; j < 4; j++) {
          if (j == 0 && f > 0) {
            // These are the four non-thumb metacarpals.
            nextJoint = new Vector3(
              0.043f  + -f * 0.021f + (f == 4 ? 0.005f : 0f),
              (f > 1 ? 0.01f : 0f),
              0.02f - (f > 2 ? 0.007f : 0f));
            prevJoint = new Vector3(
              -f * 0.015f + 0.04f,
              -0.015f,
              -0.05f);
            boneRot = Quaternion.Euler(0f, (f == 0 ? 75 : ((f * -7f + 15f))), 0f);
          }
          else if (f == 0 && j == 0) {
            // "Thumb "Knuckle"" -- TODO: Why is this not just a zero-length bone?
            nextJoint = new Vector3(0.02f, -0.015f, -0.05f);
            prevJoint = new Vector3(0.01f, -0.015f, -0.055f);
            boneRot = Quaternion.Euler(30f, 50, -90f);
          }
          else {
            //Main Fingers
            //Finger Curl
            if (j == 1 && f > 0) {
              boneRot = Quaternion.Euler(
                x: (f == 0 ? 60f : 70f) * ((float)fingerCurls[f]) / 256f,
                y: (j == 1 ?
                     (f == 0 ?
                       75
                     : ((f * -7f + 15f) * (((float)fingerSpread) / 256f) * 3f))
                   : 0f),
                z:  0f);
            }
            else {
              boneRot *= Quaternion.Euler(
                (f == 0 ? 60f : 70f) * ((float)fingerCurls[f]) / 256f, 0f, 0f);
            }

            prevJoint = nextJoint;
            nextJoint = nextJoint
              + (boneRot * new Vector3(0f, 0f, (f == 0 ? 0.055f : 0.045f) / j));
          }

          // TODO: _what_??? ""Fix for Rigged Hands""
          var meshRot = Quaternion.Euler(boneRot.eulerAngles.x,
            (isLeft ? 1f : -1f) * boneRot.eulerAngles.y,
            (!isLeft && (f == 0) ? -1f : 1f) * boneRot.eulerAngles.z);

          // Bone data.
          intoHand.GetBone((f * 4) + j).Fill(
            prevJoint: toWorld(prevJoint, palmPose, isLeft).ToVector(),
            nextJoint: toWorld(nextJoint, palmPose, isLeft).ToVector(),
            center: toWorld((prevJoint + nextJoint) * 0.5f, palmPose, isLeft).ToVector(),
            direction: (nextJoint - prevJoint).normalized.ToVector(),
            length: (nextJoint - prevJoint).magnitude,
            width: 0.01f,
            type: (Bone.BoneType)j,
            rotation: (palmPose.rotation * meshRot).ToLeapQuaternion()
          );
        }
        
        // Finger data.
        intoHand.Fingers[f].Fill(
          frameId: -1,
          handId: (isLeft ? 0 : 1),
          fingerId: f,
          timeVisible: 0f,
          tipPosition: toWorld(nextJoint, palmPose, isLeft).ToVector(),
          direction: (boneRot * Vector3.forward).ToVector(),
          width: 0.01f,
          length: intoHand.Fingers[f].bones.Query()
                    .Select(b => b.Length).Fold((l, acc) => l + acc),
          isExtended: true,
          type: (Finger.FingerType)f
        );
      }

      // Arm data.
      intoHand.Arm.Fill(
        elbow: toWorld(new Vector3(0f, 0f, -0.3f), palmPose, isLeft).ToVector(),
        wrist: toWorld(new Vector3(0f, 0f, -0.055f), palmPose, isLeft).ToVector(),
        center: toWorld(new Vector3(0f, 0f, -0.125f), palmPose, isLeft).ToVector(),
        direction: Vector.Zero,
        length: 0.3f,
        width: 0.05f,
        rotation: (palmPose.rotation).ToLeapQuaternion());

      // Hand data.
      intoHand.Fill(
        frameID: -1,
        id: (isLeft ? 0 : 1),
        confidence: 1f,
        grabStrength: 0.5f,
        grabAngle: 100f,
        pinchStrength: 0.5f,
        pinchDistance: 50f,
        palmWidth: 0.085f,
        isLeft: isLeft,
        timeVisible: 1f,
        fingers: null /* already uploaded finger data */,
        palmPosition: palmPose.position.ToVector(),
        stabilizedPalmPosition: palmPose.position.ToVector(),
        palmVelocity: Vector3.zero.ToVector(),
        palmNormal: (palmPose.rotation * Vector3.down).ToVector(),
        rotation: palmPose.rotation.ToLeapQuaternion(),
        direction: (palmPose.rotation * Vector3.forward).ToVector(),
        wristPosition: toWorld(new Vector3(0f, 0f, -0.055f), palmPose, isLeft).ToVector()
      );
    }
    
    /// <summary>
    /// Converts local-space point p to world space given the pose, with a chirality
    /// flip for the X coordinate if the chirality is not left.
    /// </summary>
    private static Vector3 toWorld(Vector3 p, Pose pose, bool isLeft) {
      return pose.rotation * new Vector3(p.x * (isLeft ? 1f : -1f), p.y, p.z)
        + pose.position;
    }

    #endregion

    #region IByteEncodable<Hand>

    /// <summary>
    /// CurlHand is an extremely lightweight encoding for hands, requiring only 17 bytes.
    /// Palm position and rotation are reasonably high resolution, while pose data is
    /// heavily compressed.
    /// 
    /// 1 for chirality, 6 bytes for camera-local palm position, 4 bytes for camera-local
    /// palm rotation, 5 bytes for the curl of each finger, and 1 byte for finger spread.
    /// </summary>
    public int numBytesRequired {
      get { return 17; }
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
