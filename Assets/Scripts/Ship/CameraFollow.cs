using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	public Transform player;

	// Use this for initialization
	void Awake () {
		player = GameObject.FindWithTag ("Player").transform;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		transform.position = player.position;
	}
}
