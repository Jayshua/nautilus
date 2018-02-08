using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CameraFollow : MonoBehaviour {

	Transform player;

	// Use this for initialization
	void Awake () {
	}
	
	// Update is called once per frame
	void LateUpdate () {
		if (player != null) {
			transform.position = player.position;
			transform.Translate (0,100,-100, Space.World);
			transform.LookAt (player);
		}
	}

	public void PlayerCreated(Transform player) {
		this.player = player;
	}
}