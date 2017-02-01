﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiMeshData : LeapGuiElementData {

  [SerializeField]
  private Mesh _mesh;

  public Color color = Color.white;

  public Mesh mesh {
    get {
      Procedural
    }
    set {
      _mesh = value;
    }
  }



}
