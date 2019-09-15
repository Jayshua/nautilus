using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CannonBall : MonoBehaviour {
	public float ballDamage;
	public GameObject woodEffect;

	void Start()
	{
		//Destroy cannonballs after 5 seconds
		Destroy (this.gameObject, 5f);
	}

	void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.CompareTag ("Player")) {
			// This object is not a network behaviour to save on network traffic.
			// Therefore, it doesn't have access to isServer, hasAuthority, etc.
			// Instead, we check those values on the ship object.
			var otherShip = other.gameObject.GetComponent<Ship> ();

			if (otherShip.isServer) {
				otherShip.TakeDamage (ballDamage);
			}

			if (otherShip.hasAuthority) {
				EZCameraShake.CameraShaker.Instance.ShakeOnce (3f, 3f, .3f, .5f);
			}

			Instantiate (woodEffect, other.contacts[0].point, Quaternion.LookRotation(other.contacts[0].normal));
		}
		if (other.gameObject.CompareTag ("Tower")) {
			other.gameObject.GetComponentInParent<KukisPoint> ().TakeDamage (ballDamage);
			Instantiate (woodEffect, other.contacts[0].point, Quaternion.LookRotation(other.contacts[0].normal));
		}
		Destroy (this.gameObject);
	}
}
