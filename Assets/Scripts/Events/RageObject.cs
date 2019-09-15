using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
public class RageObject : NetworkBehaviour {

	float elapsedTime = 0f;
	Spoils spoils;
    Ship ship;
	Rigidbody rb;
	bool isCarried = false;
	bool isInAir = false;
	Renderer rd;
	public float cooldown = 1f;
	public event Action OnDone;

	[SerializeField] float floatSpeed;
	[SerializeField] float rotateSpeed;
	[SerializeField] float movementDistance;
	[SerializeField] float knockbackForce;

	void Start () {
		rd = GetComponent<Renderer> ();
		rb = GetComponent<Rigidbody> ();
		spoils = new Spoils (){ Gold = 5, Fame = 5, Powerups = new int[]{ } };
	}

	void FixedUpdate(){
		if (isInAir && !isCarried) {
			transform.rotation = Quaternion.Euler (0f, Time.fixedTime * rotateSpeed, 0f);
			transform.position = new Vector3 (transform.position.x, Mathf.Sin (Time.fixedTime * floatSpeed) * movementDistance - 1.5f, transform.position.z);
		}
	}

	void OnTriggerEnter(Collider other){
		if (other.gameObject.layer == 4 && !isCarried) {
			isInAir = true;
			rb.isKinematic = true;
		} 

		if (other.CompareTag ("TerrainCollider")) {
			var knockbackDirection = other.transform.position - transform.position;
			rb.AddForce (knockbackForce * knockbackDirection, ForceMode.Impulse);
		}

		if (!isCarried) {
			if (other.CompareTag ("Player")) {
				this.transform.SetParent (other.transform);
				isCarried = true;
				transform.position = transform.parent.position;
				rd.enabled = false;
				ship = other.GetComponent<Ship> ();
				ship.setIsCarryingMissionObject (true);
				ship.setCanShoot (false);
				if (isServer) {
					ship.player.SendNotification ("The Rage Parrot is now with you!\n" +
					"You cannot fire while holding the parrot.\n" +
					"Press R to release the parrot.");
					StartCoroutine (Timer ());
				}
				StartCoroutine (ObjectBehaviour ());
			}
		}
	}

	IEnumerator Timer(){
		yield return new WaitForSeconds (90f);
		ship.setCanShoot (true);
		if (OnDone != null) {
			OnDone ();
		}
		Destroy (gameObject);
	}

	IEnumerator ObjectBehaviour(){
		while (true) {
			if (transform.parent == null) {
				ship = null;
				isCarried = false;
				StopCoroutine (Timer ());
			}
			else if (isCarried) {
				if (Time.time > elapsedTime) {
					elapsedTime = Time.time + cooldown;

					if (isServer) {
						ship.player.ServerTakeSpoils (spoils);
					}
				}

				if (ship.hasAuthority) {
					if (Input.GetKeyDown (KeyCode.R)) {
						ship.setCanShoot (true);
						this.transform.position = ship.PowderKegPosition.position;
						rb.velocity = -transform.forward * 5f;
						transform.SetParent(null);
						rd.enabled = true;
					}
				}
			}
			yield return null;
		}
	}
}