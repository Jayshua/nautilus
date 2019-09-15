using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour {
	void FixedUpdate () {
		this.transform.LookAt (Camera.main.transform);
		this.transform.Rotate (0, 180, 0);
	}
}
