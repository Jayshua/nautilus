using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonBall : MonoBehaviour {

	public int ballDamage;

	void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.CompareTag ("Player")) {
			other.gameObject.GetComponent<Ship> ().TakeDamge (ballDamage);
		}

		Destroy (this.gameObject);
	}
}
