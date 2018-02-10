using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Triggered : MonoBehaviour {

	public event Action OnDestroy;

	// Use this for initialization
	void Start () {
		
	}

	void OnTriggerEnter(Collider ship){
		if (OnDestroy != null) {
			OnDestroy();
		}
		Destroy (this.gameObject);
	}

	// Update is called once per frame
	void Update () {
		
	}
}
