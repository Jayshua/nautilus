using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	public static Transform player;

	// Use this for initialization
	void Awake () {
	}
	
	// Update is called once per frame
	void LateUpdate () {
		transform.position = player.position;
	}
}
