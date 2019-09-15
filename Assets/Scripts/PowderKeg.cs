using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using EZCameraShake;

public class PowderKeg : NetworkBehaviour
{
	public float power;
	public float radius;
	public float damage;

	[SerializeField]
	GameObject explosionFX;

	[SerializeField]
	AudioSource PowderKegAudioSource;
	[SerializeField]
	AudioClip   BurningFuseSFX;
	[SerializeField]
	AudioClip   ExplosionSFX;

	[SerializeField]
	float rotateSpeed = 0.1f;
	float lifeTime = 60f;

	[SerializeField]
	float floatSpeed = 0.1f;

	[SerializeField]
	float movementDistance = 1f;

	bool isInAir = false;

	// Use this for initialization
	void Start ()
	{
		transform.Rotate (transform.up, UnityEngine.Random.Range (0f, 360f));
		Invoke ("DealAreaDamage", lifeTime);
	}

	void FixedUpdate ()
	{
		if (isInAir) {
			transform.rotation = Quaternion.Euler (0f, Time.fixedTime * rotateSpeed, 0f);
			transform.position = new Vector3 (transform.position.x, Mathf.Sin (Time.fixedTime * floatSpeed) * movementDistance - 1.9f, transform.position.z);
		}
	}


	void OnTriggerEnter (Collider collision)
	{
		// Turn off rigidbody movement after hitting water
		if (collision.gameObject.layer == 4) {
			isInAir = true;
			gameObject.GetComponent<Rigidbody> ().isKinematic = true;
		} else if (!collision.CompareTag ("Chest") || !collision.CompareTag ("Mission")) {
			if (collision.CompareTag ("Player")) {
				StartCoroutine (DelayTakeDamage (collision.GetComponent<Ship> ()));
				DealAreaDamage ();
			} else if (collision.CompareTag ("Barrel")) {
				Destroy (gameObject, 0.5f);
				DealAreaDamage (2f);
			} else if (collision.CompareTag ("CannonBall")) {
				Destroy (gameObject, 0.5f);
				DealAreaDamage ();
			}
			Instantiate (explosionFX, transform.position, Quaternion.identity);
		}
	}

	void DealAreaDamage (float radiusMutiplier = 1f, float damageMultiplier = 1f)
	{
		Collider[] colliders = Physics.OverlapSphere (this.transform.position, radius * radiusMutiplier);
		if (colliders != null) {
			foreach (Collider nearbyObject in colliders) {
				if (nearbyObject.CompareTag ("Player")) {
					StartCoroutine (DelayTakeDamage (nearbyObject.GetComponent<Ship> (), damageMultiplier));
					Rigidbody rb = nearbyObject.GetComponent<Rigidbody> ();
					rb.AddExplosionForce (power, this.transform.position, radius, 1f, ForceMode.Impulse);
				}
			}
		}
	}

	IEnumerator DelayTakeDamage (Ship ship, float damageMutiplier = 1f)
	{
		yield return new WaitForSeconds (0.02f);

		if (ship.hasAuthority) {
			EZCameraShake.CameraShaker.Instance.ShakeOnce (5f, 5f, .3f, .5f);
		}

		if (isServer) {
			ship.TakeDamage (damage * 0.5f * damageMutiplier);
		}
			
		gameObject.GetComponent<MeshRenderer> ().enabled = false;
		gameObject.GetComponent<BoxCollider> ().enabled  = false;

		PowderKegAudioSource.clip = ExplosionSFX;
		PowderKegAudioSource.loop = false;
		PowderKegAudioSource.Play ();

		Destroy (this.gameObject, 2f);
	}
}