using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Internal;
using System.Linq;

namespace Leap.Unity {
  public class MultideviceAlignment : MonoBehaviour {
    [Serializable]
    public struct MultideviceCalibrationInfo {
      public LeapProvider deviceProvider;
      [NonSerialized]
      public List<Vector3> handPoints;
      [NonSerialized]
      public Hand currentHand;
    }
    public MultideviceCalibrationInfo[] devices;

    public HandModelManager virtualHands;

    public KeyCode autocalibrateKey;
    public KeyCode changeMergeHandsKey;

    private bool autoSamplingEnabled = false;
    private bool mergeHands = false;


    // Use this for initialization
    void Start() {}


    // Update is called once per frame
    void Update() {
      if (devices.Length > 1) {
        // Add the set of joints to device-specific lists
        if (autoSamplingEnabled) {
          if (devices[0].handPoints != null)
          {
            Debug.Log(devices[0].handPoints.Count);
          }
          AddMeasurement();
        }

        if (Input.GetKeyUp(autocalibrateKey) || (devices[0].handPoints != null && devices[0].handPoints.Count >= 50000))
        {
          if (!autoSamplingEnabled)
          {
            autoSamplingEnabled = true;
          } else
          {
            autoSamplingEnabled = false;
            ComputeRotation();
          }
        }

        if (Input.GetKeyUp(changeMergeHandsKey))
        {
          if (!mergeHands)
          {
            mergeHands = true;
            for (int i = 0; i < devices.Length; i++)
            {
              object child = devices[i].deviceProvider.GetComponentInChildren(typeof(HandModelManager));
              if (child != null)
              {
                ((HandModelManager)child).DisableGroup("Graphics_Hands");
                ((HandModelManager)child).DisableGroup("Physics_Hands");
              }
            }

            if (virtualHands != null)
            {
              virtualHands.EnableGroup("Graphics_Hands");
              virtualHands.EnableGroup("Physics_Hands");
            }
          }
          else
          {
            mergeHands = false;
            for (int i = 0; i < devices.Length; i++)
            {
              object child = devices[i].deviceProvider.GetComponentInChildren(typeof(HandModelManager));
              if (child != null)
              {
                ((HandModelManager)child).EnableGroup("Graphics_Hands");
                ((HandModelManager)child).EnableGroup("Physics_Hands");
              }
            }

            if (virtualHands != null)
            {
              virtualHands.DisableGroup("Graphics_Hands");
              virtualHands.DisableGroup("Physics_Hands");
            }
          }
        }
      }
    }

    private void AddMeasurement()
    {
      bool handInAllFrames = true;
      for (int i = 0; i < devices.Length; i++)
      {
        Hand rightHand = devices[i].deviceProvider.CurrentFrame.Get(Chirality.Right);
        if (rightHand != null)
        {
          devices[i].currentHand = rightHand;
        }
        else
        {
          handInAllFrames = false;
        }
      }

      if (handInAllFrames)
      {
        for (int i = 0; i < devices.Length; i++)
        {
          if (devices[i].handPoints == null)
          {
            devices[i].handPoints = new List<Vector3>();
          }
          for (int j = 0; j < 5; j++)
          {
            for (int k = 0; k < 4; k++)
            {
              devices[i].handPoints.Add(devices[i].currentHand.Fingers[j].bones[k].Center.ToVector3());
            }
          }
        }
      }
    }

    private void ComputeRotation()
    {
      if (devices[0].handPoints.Count > 3)
      {
        KabschSolver solver = new KabschSolver();
        for (uint i = 1; i < devices.Length; i++)
        {
          List<Vector3> refValues = new List<Vector3>(devices[0].handPoints);

          Matrix4x4 deviceToOriginDeviceMatrix =
            solver.SolveKabsch(devices[i].handPoints, refValues, 200);

          devices[i].deviceProvider.transform.Transform(deviceToOriginDeviceMatrix);
        }
        devices[0].handPoints.Clear();
      }
    }

    public void MergeHands(ref Hand virtualHand, Chirality chirality)
    {
      List<Vector> palmPositions = new List<Vector>();
      List<Vector> stabilizedPalmPositions = new List<Vector>();
      List<Vector> palmVelocities = new List<Vector>();
      List<Vector> palmNormals = new List<Vector>();
      List<Vector> directions = new List<Vector>();
      List<Vector> wristPositions = new List<Vector>();
      int handsCount = 0;


      virtualHand.Confidence = 0;
      virtualHand.GrabStrength = 0;
      virtualHand.GrabAngle = 0;
      virtualHand.PinchStrength = 0;
      virtualHand.PinchDistance = 0;
      virtualHand.PalmWidth = 0;
      virtualHand.TimeVisible = 0;
      virtualHand.Rotation = new LeapQuaternion(0,0,0,0);

      for (int i = 0; i < devices.Length; i++)
      {
        Hand hand = devices[i].deviceProvider.CurrentFrame.Get(chirality);
        if (hand == null)
        {
          continue;
        }
        handsCount++;
        float angle = hand.PalmNormal.AngleTo(-devices[i].deviceProvider.transform.up.ToVector());

        hand.Confidence = (0.283699f * angle * angle) - (0.891268f * angle) + 1;

        virtualHand.Confidence += hand.Confidence;
        virtualHand.GrabStrength += hand.GrabStrength * hand.Confidence;
        virtualHand.GrabAngle += hand.GrabAngle * hand.Confidence;
        virtualHand.PinchStrength += hand.PinchStrength * hand.Confidence;
        virtualHand.PinchDistance += hand.PinchDistance * hand.Confidence;
        virtualHand.PalmWidth += hand.PalmWidth * hand.Confidence;
        virtualHand.TimeVisible += hand.TimeVisible * hand.Confidence;
        palmPositions.Add(hand.PalmPosition * hand.Confidence);
        stabilizedPalmPositions.Add(hand.StabilizedPalmPosition * hand.Confidence);
        palmVelocities.Add(hand.PalmVelocity * hand.Confidence);
        palmNormals.Add(hand.PalmNormal * hand.Confidence);

        ProcessQuaternionRotation(ref hand.Rotation);
        virtualHand.Rotation.x += hand.Rotation.x * hand.Confidence;
        virtualHand.Rotation.y += hand.Rotation.y * hand.Confidence;
        virtualHand.Rotation.z += hand.Rotation.z * hand.Confidence;
        virtualHand.Rotation.w += hand.Rotation.w * hand.Confidence;

        directions.Add(hand.Direction * hand.Confidence);
        wristPositions.Add(hand.WristPosition * hand.Confidence);
      }
      if (handsCount == 0 || (virtualHand.Confidence / handsCount) < 0.1)
      {
         virtualHand = null;
         return;
      }

      DivideQuaternion(ref virtualHand.Rotation, virtualHand.Confidence);
      virtualHand.Rotation = virtualHand.Rotation.Normalized;

      virtualHand.GrabStrength /= virtualHand.Confidence;
      virtualHand.GrabAngle /= virtualHand.Confidence;
      virtualHand.PinchStrength /= virtualHand.Confidence;
      virtualHand.PinchDistance /= virtualHand.Confidence;
      virtualHand.PalmWidth /= virtualHand.Confidence;
      virtualHand.TimeVisible /= virtualHand.Confidence;
      virtualHand.PalmPosition = CenterOfVectors(palmPositions, virtualHand.Confidence);
      virtualHand.StabilizedPalmPosition = CenterOfVectors(stabilizedPalmPositions, virtualHand.Confidence);
      virtualHand.PalmVelocity = CenterOfVectors(palmVelocities, virtualHand.Confidence);
      virtualHand.PalmNormal = CenterOfVectors(palmNormals, virtualHand.Confidence);
      virtualHand.Direction = CenterOfVectors(directions, virtualHand.Confidence);
      virtualHand.WristPosition = CenterOfVectors(wristPositions, virtualHand.Confidence);

      ComputeArm(virtualHand.Confidence, ref virtualHand, chirality);
      ComputeFingers(virtualHand.Confidence, ref virtualHand, chirality);
      virtualHand.Confidence /= handsCount;
    }

    private void ComputeArm(float overallConfidence, ref Hand virtualHand, Chirality chirality)
    {
      List<Vector> elbows = new List<Vector>();
      List<Vector> wrists = new List<Vector>();
      List<Vector> centers = new List<Vector>();
      List<Vector> directions = new List<Vector>();
      float length = 0;
      float width = 0;
      LeapQuaternion armRotation = new LeapQuaternion(0, 0, 0, 0);

      for (int i = 0; i < devices.Length; i++)
      {
        Hand hand = devices[i].deviceProvider.CurrentFrame.Get(chirality);
        if (hand == null)
        {
          continue;
        }
        elbows.Add(hand.Arm.ElbowPosition * hand.Confidence);
        wrists.Add(hand.Arm.WristPosition * hand.Confidence);
        centers.Add(hand.Arm.Center * hand.Confidence);
        directions.Add(hand.Arm.Direction * hand.Confidence);
        length += hand.Arm.Length * hand.Confidence;
        width += hand.Arm.Width * hand.Confidence;

        ProcessQuaternionRotation(ref hand.Arm.Rotation);
        armRotation.x += hand.Arm.Rotation.x * hand.Confidence;
        armRotation.y += hand.Arm.Rotation.y * hand.Confidence;
        armRotation.z += hand.Arm.Rotation.z * hand.Confidence;
        armRotation.w += hand.Arm.Rotation.w * hand.Confidence;
      }
      DivideQuaternion(ref armRotation, overallConfidence);
      virtualHand.Arm = new Arm(
        CenterOfVectors(elbows, overallConfidence),
        CenterOfVectors(wrists, overallConfidence),
        CenterOfVectors(centers, overallConfidence),
        CenterOfVectors(directions, overallConfidence),
        length / overallConfidence,
        width / overallConfidence,
        armRotation.Normalized
      );
    }
    private void ComputeFingers(float overallConfidence, ref Hand newHand, Chirality chirality)
    {
      newHand.Fingers = new List<Finger>();
      float timeVisible = 0;
      List<Vector> tipPositions = new List<Vector>();
      List<Vector> directions = new List<Vector>();
      float width = 0;
      float length = 0;
      int extendedCount = 0;

      for (int j = 0; j < 5; j++)
      {
        for (int i = 0; i < devices.Length; i++)
        {
          Hand hand = devices[i].deviceProvider.CurrentFrame.Get(chirality);
          if (hand == null)
          {
            continue;
          }
          timeVisible += hand.Fingers[j].TimeVisible * hand.Confidence;
          tipPositions.Add(hand.Fingers[j].TipPosition * hand.Confidence);
          directions.Add(hand.Fingers[j].Direction * hand.Confidence);
          width += hand.Fingers[j].Width * hand.Confidence;
          length += hand.Fingers[j].Length * hand.Confidence;
          extendedCount += hand.Fingers[j].IsExtended ? 1 : 0;
        }
        newHand.Fingers.Add(new Finger(
          newHand.FrameId,
          newHand.Id,
          j,
          timeVisible / overallConfidence,
          CenterOfVectors(tipPositions, overallConfidence),
          CenterOfVectors(directions, overallConfidence),
          width / overallConfidence,
          length / overallConfidence,
          extendedCount >= overallConfidence,
          (Finger.FingerType)j,
          ComputeFingerBone(overallConfidence, j, 0, chirality),
          ComputeFingerBone(overallConfidence, j, 1, chirality),
          ComputeFingerBone(overallConfidence, j, 2, chirality),
          ComputeFingerBone(overallConfidence, j, 3, chirality)
          )
        );
      }
    }

    public Bone ComputeFingerBone(float overallConfidence, int fingerIndex, int bonedIndex, Chirality chirality)
    {
      List<Vector> prevJoints = new List<Vector>();
      List<Vector> nextJoints = new List<Vector>();
      List<Vector> centers = new List<Vector>();
      List<Vector> bonesDirections = new List<Vector>();
      float length = 0;
      float width = 0;
      LeapQuaternion bonesRotation = new LeapQuaternion(0, 0, 0, 0);
      for (int i = 0; i < devices.Length; i++)
      {
        Hand hand = devices[i].deviceProvider.CurrentFrame.Get(chirality);
        if (hand == null)
        {
          continue;
        }
        Bone bone = hand.Fingers[fingerIndex].bones[bonedIndex];
        prevJoints.Add(bone.PrevJoint * hand.Confidence);
        nextJoints.Add(bone.NextJoint * hand.Confidence);
        centers.Add(bone.Center * hand.Confidence);
        bonesDirections.Add(bone.Direction * hand.Confidence);
        length += bone.Length * hand.Confidence;
        width += bone.Width * hand.Confidence;

        ProcessQuaternionRotation(ref bone.Rotation);

        bonesRotation.x += bone.Rotation.x * hand.Confidence;
        bonesRotation.y += bone.Rotation.y * hand.Confidence;
        bonesRotation.z += bone.Rotation.z * hand.Confidence;
        bonesRotation.w += bone.Rotation.w * hand.Confidence;
      }
      DivideQuaternion(ref bonesRotation, overallConfidence);
      bonesRotation = bonesRotation.Normalized;
      return new Bone(
        CenterOfVectors(prevJoints, overallConfidence),
        CenterOfVectors(nextJoints, overallConfidence),
        CenterOfVectors(centers, overallConfidence),
        CenterOfVectors(bonesDirections, overallConfidence),
        length / overallConfidence,
        width / overallConfidence,
        (Bone.BoneType)bonedIndex,
        bonesRotation.Normalized
     );
    }

    private Vector CenterOfVectors(List<Vector> vectors, float confidence)
    {
      Vector sum = new Vector(0,0,0);
      if (vectors == null || vectors.Count == 0)
      {
        return sum;
      }

      foreach (Vector vec in vectors)
      {
        sum += vec;
      }
      return sum / confidence;
    }

    private void DivideQuaternion(ref LeapQuaternion quaternion, float divider)
    {
      quaternion.x /= divider;
      quaternion.y /= divider;
      quaternion.z /= divider;
      quaternion.w /= divider;
    }

    private void ProcessQuaternionRotation(ref LeapQuaternion quaternion)
    {
      if (quaternion.w < 0)
      {
        quaternion.x *= -1;
        quaternion.y *= -1;
        quaternion.z *= -1;
        quaternion.w *= -1;
      }
    }

    public bool GetMergeHands()
    {
      return mergeHands;
    }

    private void OnDrawGizmos() {
      for (int i = 0; i < devices.Length; i++) {
        if (devices[0].handPoints != null) {
          for (int j = 0; j < devices[0].handPoints.Count; j++) {
            Gizmos.DrawSphere(devices[i].handPoints[j], 0.01f);
            if (i > 0) {
              Gizmos.DrawLine(devices[i].handPoints[j], devices[0].handPoints[j]);
            }
          }
        }
      }
    }
  }
}