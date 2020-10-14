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

    public KeyCode takeCalibrationSampleKey;
    public KeyCode autocalibrateKey;
    public KeyCode solveForRelativeTransformKey;
    public KeyCode solveForSingleHandKey;

    private bool autoSamplingEnabled = false;
    private bool computeHand = false;


    // Use this for initialization
    void Start() {}


    // Update is called once per frame
    void Update() {
      if (devices.Length > 1) {

        // Add the set of joints to device-specific lists
        if (Input.GetKeyUp(takeCalibrationSampleKey) || autoSamplingEnabled) {
          AddMeasurement();
        }

        if (Input.GetKeyUp(autocalibrateKey) || (devices[0].handPoints != null && devices[0].handPoints.Count >= devices.Count() * 2000))
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

        // Moves subsidiary devices to be in alignment with the device at the 0 index
        if (Input.GetKeyUp(solveForRelativeTransformKey)) {
          ComputeRotation();
        }

        if (Input.GetKeyUp(solveForSingleHandKey))
        {
          if (!computeHand)
          {
            computeHand = true;
            for (int i = 0; i < devices.Length; i++)
            {
              object child = devices[i].deviceProvider.GetComponentInChildren(typeof(HandModelManager));
              if (child != null)
              {
                ((HandModelManager)child).DisableGroup("Graphics_Hands");
                ((HandModelManager)child).DisableGroup("Physics_Hands");
              }
            }
          }

          if (virtualHands != null)
          {
            virtualHands.EnableGroup("Graphics_Hands");
            virtualHands.EnableGroup("Physics_Hands");
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

    public void MergeHands(ref Hand virtualHand)
    {
      if (virtualHand == null)
      {
        return;
      }

      Chirality chirality = virtualHand.IsLeft ? Chirality.Left : Chirality.Right;
      List<Vector> palmPositions = new List<Vector>();
      List<Vector> stabilizedPalmPositions = new List<Vector>();
      List<Vector> palmVelocities = new List<Vector>();
      List<Vector> palmNormals = new List<Vector>();
      List<Vector> directions = new List<Vector>();
      List<Vector> wristPositions = new List<Vector>();
      int handsCount = 0;

      for (int i = 0; i < devices.Length; i++)
      {
        Hand hand = devices[i].deviceProvider.CurrentFrame.Get(chirality);
        if (hand == null)
        {
          continue;
        }
        handsCount++;
        virtualHand.Confidence += hand.Confidence;
        virtualHand.GrabStrength += hand.GrabStrength;
        virtualHand.GrabAngle += hand.GrabAngle;
        virtualHand.PinchStrength += hand.PinchStrength;
        virtualHand.PinchDistance += hand.PinchDistance;
        virtualHand.PalmWidth += hand.PalmWidth;
        virtualHand.TimeVisible += hand.TimeVisible;
        palmPositions.Add(hand.PalmPosition);
        stabilizedPalmPositions.Add(hand.StabilizedPalmPosition);
        palmVelocities.Add(hand.PalmVelocity);
        palmNormals.Add(hand.PalmNormal);
        virtualHand.Rotation.x += hand.Rotation.x;
        virtualHand.Rotation.y += hand.Rotation.y;
        virtualHand.Rotation.z += hand.Rotation.z;
        virtualHand.Rotation.w += hand.Rotation.w;
        directions.Add(hand.Direction);
        wristPositions.Add(hand.WristPosition);
        if (i == 0)
        {
          virtualHand.Fingers = hand.Fingers;
          virtualHand.Arm = hand.Arm;
        }
      }
      if (handsCount == 0)
      {
          return;
      }

      virtualHand.Rotation.x /= handsCount;
      virtualHand.Rotation.y /= handsCount;
      virtualHand.Rotation.z /= handsCount;
      virtualHand.Rotation.w /= handsCount;
      virtualHand.Rotation = virtualHand.Rotation.Normalized;

      virtualHand.FrameId = devices[0].deviceProvider.CurrentFrame.Id;
      virtualHand.Id = chirality == Chirality.Right?1:2;
      virtualHand.Confidence /= handsCount;
      virtualHand.GrabStrength /= handsCount;
      virtualHand.GrabAngle /= handsCount;
      virtualHand.PinchStrength /= handsCount;
      virtualHand.PinchDistance /= handsCount;
      virtualHand.PalmWidth /= handsCount;
      virtualHand.IsLeft = chirality == Chirality.Left;
      virtualHand.TimeVisible /= handsCount;
      virtualHand.PalmPosition = CenterOfVectors(palmPositions);
      virtualHand.StabilizedPalmPosition = CenterOfVectors(stabilizedPalmPositions);
      virtualHand.PalmVelocity = CenterOfVectors(palmVelocities);
      virtualHand.PalmNormal = CenterOfVectors(palmNormals);
      virtualHand.Direction = CenterOfVectors(directions);
      virtualHand.WristPosition = CenterOfVectors(wristPositions);

      ComputeArm(handsCount, ref virtualHand, chirality);
      ComputeFingers(handsCount, ref virtualHand, chirality);

    }

    private void ComputeArm(int handsCount, ref Hand newHand, Chirality chirality)
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
        elbows.Add(hand.Arm.ElbowPosition);
        wrists.Add(hand.Arm.WristPosition);
        centers.Add(hand.Arm.Center);
        directions.Add(hand.Arm.Direction);
        length += hand.Arm.Length;
        width += hand.Arm.Width;
        armRotation.x += hand.Arm.Rotation.x;
        armRotation.y += hand.Arm.Rotation.y;
        armRotation.z += hand.Arm.Rotation.z;
        armRotation.w += hand.Arm.Rotation.w;
      }
      armRotation.x /= handsCount;
      armRotation.y /= handsCount;
      armRotation.z /= handsCount;
      armRotation.w /= handsCount;
      newHand.Arm = new Arm(
          CenterOfVectors(elbows),
          CenterOfVectors(wrists),
          CenterOfVectors(centers),
          CenterOfVectors(directions),
          length / handsCount,
          width / handsCount,
          armRotation.Normalized
        );
    }
    private void ComputeFingers(int handsCount, ref Hand newHand, Chirality chirality)
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
          timeVisible += hand.Fingers[j].TimeVisible;
          tipPositions.Add(hand.Fingers[j].TipPosition);
          directions.Add(hand.Fingers[j].Direction);
          width += hand.Fingers[j].Width;
          length += hand.Fingers[j].Length;
          extendedCount += hand.Fingers[j].IsExtended ? 1 : 0;

          newHand.Fingers.Add(new Finger(
            newHand.FrameId,
            newHand.Id,
            j,
            timeVisible / handsCount,
            CenterOfVectors(tipPositions),
            CenterOfVectors(directions),
            width / handsCount,
            length / handsCount,
            extendedCount >= handsCount,
            (Finger.FingerType)j,
            ComputeFingerBone(j, 0, chirality, handsCount),
            ComputeFingerBone(j, 1, chirality, handsCount),
            ComputeFingerBone(j, 2, chirality, handsCount),
            ComputeFingerBone(j, 3, chirality, handsCount)
           )
          );
        }
      }
    }

    public Bone ComputeFingerBone(int fingerIndex, int bonedIndex, Chirality chirality, int handsCount)
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
        prevJoints.Add(bone.PrevJoint);
        nextJoints.Add(bone.NextJoint);
        centers.Add(bone.Center);
        bonesDirections.Add(bone.Direction);
        length += bone.Length;
        width += bone.Width;
        bonesRotation.x += bone.Rotation.x;
        bonesRotation.y += bone.Rotation.y;
        bonesRotation.z += bone.Rotation.z;
        bonesRotation.w += bone.Rotation.w;
      }
      bonesRotation.x /= handsCount;
      bonesRotation.y /= handsCount;
      bonesRotation.z /= handsCount;
      bonesRotation.w /= handsCount;
      bonesRotation = bonesRotation.Normalized;
      return new Bone(
        CenterOfVectors(prevJoints),
        CenterOfVectors(nextJoints),
        CenterOfVectors(centers),
        CenterOfVectors(bonesDirections),
        length / handsCount,
        width / handsCount,
        (Bone.BoneType)bonedIndex,
        bonesRotation
     );
    }

    public Vector CenterOfVectors(List<Vector> vectors)
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
      return sum / vectors.Count;
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