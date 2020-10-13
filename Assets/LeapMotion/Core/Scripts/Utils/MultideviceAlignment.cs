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

    public MultiLeapServiceProvider virtualDevice;

    // Use this for initialization
    void Start() {}


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

        if (Input.GetKeyUp(solveForSingleHandKey))
        {
          ComputeCenterHandPrecise2();
          if (!computeHand)
          {
            computeHand = true;
            /*for (int i = 0; i < devices.Length; i++)
            {
              devices[i].deviceProvider.enabled = false;
            }*/
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
          virtualDevice.originTransformations[i] = deviceToOriginDeviceMatrix;
        }
        devices[0].handPoints.Clear();
        virtualDevice.enabled = true;
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
      foreach (Chirality chirality in Enum.GetValues(typeof(Chirality))) {
        Hand currentHand = devices[0].currentHand;
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
        }
        if (handsCount == 0)
        {
          continue;
        }
        for (int j = 0; j < 5; j++)
        {
          for (int k = 0; k < 4; k++)
          {
            List<Vector> prevJoints = new List<Vector>();
            List<Vector> nextJoints = new List<Vector>();
            List<Vector> centers = new List<Vector>();
            List<Vector> bonesDirections = new List<Vector>();
            float length = 0;
            float width = 0;
            LeapQuaternion bonesRotation = new LeapQuaternion(0,0,0,0);
            for (int i = 0; i < devices.Length; i++)
            {
              Hand hand = devices[i].deviceProvider.CurrentFrame.Get(chirality);
              if (hand == null)
              {
                continue;
              }
              prevJoints.Add(hand.Fingers[j].bones[k].PrevJoint);
              nextJoints.Add(hand.Fingers[j].bones[k].NextJoint);
              centers.Add(hand.Fingers[j].bones[k].Center);
              bonesDirections.Add(hand.Fingers[j].bones[k].Direction);
              length += hand.Fingers[j].bones[k].Length;
              width += hand.Fingers[j].bones[k].Width;
              bonesRotation.x += hand.Rotation.x;
              bonesRotation.y += hand.Rotation.y;
              bonesRotation.z += hand.Rotation.z;
              bonesRotation.w += hand.Rotation.w;
            }
            bonesRotation.x /= handsCount;
            bonesRotation.y /= handsCount;
            bonesRotation.z /= handsCount;
            bonesRotation.w /= handsCount;
            newHand.Fingers[j].bones[k].Fill(
              CenterOfVectors(prevJoints),
              CenterOfVectors(nextJoints),
              CenterOfVectors(centers),
              CenterOfVectors(bonesDirections),
              length / handsCount,
              width / handsCount,
              (Bone.BoneType)k,
              bonesRotation
           );
          }
        }
        newHand.Rotation.x /= handsCount;
        newHand.Rotation.y /= handsCount;
        newHand.Rotation.z /= handsCount;
        newHand.Rotation.w /= handsCount;
        Hand virtualHand = virtualDevice.Get(chirality);
        if (virtualHand != null)
        {
          virtualHand.Fill(
            currentHand.FrameId,
            currentHand.Id,
            newHand.Confidence / handsCount,
            newHand.GrabStrength / handsCount,
            newHand.GrabAngle / handsCount,
            newHand.PinchStrength / handsCount,
            newHand.PinchDistance / handsCount,
            newHand.PalmWidth / handsCount,
            chirality == Chirality.Left,
            newHand.TimeVisible / handsCount,
            newHand.Fingers,
            CenterOfVectors(palmPositions),
            CenterOfVectors(stabilizedPalmPositions),
            CenterOfVectors(palmVelocities),
            CenterOfVectors(palmNormals),
            newHand.Rotation,
            CenterOfVectors(directions),
            CenterOfVectors(wristPositions)
          );
        } 
      }
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