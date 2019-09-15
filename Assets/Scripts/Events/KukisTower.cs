using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KukisTower : MonoBehaviour {
	[SerializeField] GameObject woodEffect;
	KukisPoint kukisPointScript;

	void Start()
	{
		kukisPointScript = GetComponentInParent<KukisPoint> ();
	}

	void OnCollisionEnter(Collision other)
	{
		if(other.gameObject.CompareTag("Player")){
			kukisPointScript.TakeDamage (5f);
			Instantiate (woodEffect, other.contacts[0].point, Quaternion.LookRotation(other.contacts[0].normal));
		}
	}
}
