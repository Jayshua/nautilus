using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonBall : MonoBehaviour {

	public float ballDamage;
	public Player player;

	void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.CompareTag ("Player")) {
			other.gameObject.GetComponent<Ship> ().TakeDamage (ballDamage, player);
		}

		Destroy (this.gameObject);
	}

	void Start()
	{
		Destroy(this, 6);
	}
}
