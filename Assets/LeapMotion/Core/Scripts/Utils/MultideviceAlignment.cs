using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Internal;

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

    public KeyCode takeCalibrationSampleKey;
    public KeyCode autocalibrateKey;
    public KeyCode solveForRelativeTransformKey;
    public KeyCode solveForSingleHandKey;

    private bool autoSamplingEnabled = false;
    private bool computeHand = false;

    public List<Hand> virtualHands;

    // Use this for initialization
    void Start() {
      virtualHands = new List<Hand>();
    }


    // Update is called once per frame
    void Update() {
      if (devices.Length > 1) {

        // Add the set of joints to device-specific lists
        if (Input.GetKeyUp(takeCalibrationSampleKey) || autoSamplingEnabled) {
          AddMeasurement();
        }

        if (Input.GetKeyUp(autocalibrateKey) || (devices[0].handPoints != null && devices[0].handPoints.Count == 5000))
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

        if (Input.GetKeyUp(solveForSingleHandKey) || computeHand)
        {
          ComputeCenterHandPrecise2();
          if (!computeHand)
          {
            computeHand = true;
            for (int i = 1; i < devices.Length; i++)
            {
              object child = devices[i].deviceProvider.GetComponentInChildren(typeof(HandModelManager));
              if (child != null)
              {

              }
              ((HandModelManager)child).DisableGroup("Graphics_Hands");
              ((HandModelManager)child).DisableGroup("Physics_Hands");
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

    private void ComputeCenterHand()
    {
      Matrix4x4[] matrices = new Matrix4x4[devices.Length];
      KabschSolver solver = new KabschSolver();
      matrices[0] = Matrix4x4.identity;
      for (int i = 1; i < devices.Length; i++)
      {
        List<Vector3> refValues = new List<Vector3>(devices[0].handPoints);

        Matrix4x4 deviceToOriginDeviceMatrix =
          solver.SolveKabsch(devices[i].handPoints, refValues, 200);
        
        matrices[i] = deviceToOriginDeviceMatrix;
        if (i != 0)
        {
          devices[i].handPoints.Clear();
          devices[i].deviceProvider.enabled = false;
        }
      }
      devices[0].handPoints.Clear();

      Matrix4x4 resultMatrix = Matrix4x4.zero;
      foreach (Matrix4x4 matrix in matrices)
      {
        for (int i = 0; i < 4; i++)
        {
          for (int j = 0; j < 4; j++)
          {
            resultMatrix[i,j] += matrix[i,j];
          }
        }
      }
      for (int i = 0; i < 4; i++)
      {
        for (int j = 0; j < 4; j++)
        {
          resultMatrix[i, j] = resultMatrix[i, j] / devices.Length;
        }
      }
      devices[0].deviceProvider.transform.Transform(resultMatrix);
    }

    public void ComputeCenterHandPrecise2()
    {
      virtualHands.Clear();
      foreach (Chirality chirality in Enum.GetValues(typeof(Chirality))) {
        Hand newHand = new Hand();
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
          newHand.Confidence += hand.Confidence;
          newHand.GrabStrength += hand.GrabStrength;
          newHand.GrabAngle += hand.GrabAngle;
          newHand.PinchStrength += hand.PinchStrength;
          newHand.PinchDistance += hand.PinchDistance;
          newHand.PalmWidth += hand.PalmWidth;
          newHand.TimeVisible += hand.TimeVisible;
          palmPositions.Add(hand.PalmPosition);
          stabilizedPalmPositions.Add(hand.StabilizedPalmPosition);
          palmVelocities.Add(hand.PalmVelocity);
          palmNormals.Add(hand.PalmNormal);
          newHand.Rotation.x += hand.Rotation.x;
          newHand.Rotation.y += hand.Rotation.y;
          newHand.Rotation.z += hand.Rotation.z;
          newHand.Rotation.w += hand.Rotation.w;
          directions.Add(hand.Direction);
          wristPositions.Add(hand.WristPosition);
          if (i == 0)
          {
            newHand.Fingers = hand.Fingers;
            newHand.Arm = hand.Arm;
          }
        }
        if (handsCount == 0)
        {
            continue;
        }



        newHand.Rotation.x /= handsCount;
        newHand.Rotation.y /= handsCount;
        newHand.Rotation.z /= handsCount;
        newHand.Rotation.w /= handsCount;
        newHand.Rotation = newHand.Rotation.Normalized;

        newHand.FrameId = devices[0].deviceProvider.CurrentFrame.Id;
        newHand.Id = chirality == Chirality.Right?1:2;
        newHand.Confidence /= handsCount;
        newHand.GrabStrength /= handsCount;
        newHand.GrabAngle /= handsCount;
        newHand.PinchStrength /= handsCount;
        newHand.PinchDistance /= handsCount;
        newHand.PalmWidth /= handsCount;
        newHand.IsLeft = chirality == Chirality.Left;
        newHand.TimeVisible /= handsCount;
        newHand.PalmPosition = CenterOfVectors(palmPositions);
        newHand.StabilizedPalmPosition = CenterOfVectors(stabilizedPalmPositions);
        newHand.PalmVelocity = CenterOfVectors(palmVelocities);
        newHand.PalmNormal = CenterOfVectors(palmNormals);
        newHand.Direction = CenterOfVectors(directions);
        newHand.WristPosition = CenterOfVectors(wristPositions);

        ComputeArm(handsCount, ref newHand, chirality);
        ComputeFingers(handsCount, ref newHand, chirality);

        virtualHands.Add(newHand);
        //Debug.Log(handsCount);
      }
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