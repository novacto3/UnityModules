using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoClose : MonoBehaviour {

	void Update () {
		if(Time.frameCount > 40) {
      Application.Quit();
    }
	}
}
