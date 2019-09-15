using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class KingZone : NetworkBehaviour {

	int shipInZone = 0;
	float elapsedTime = 0f;
	Spoils spoils;
	public float cooldown = 1f;
	public event Action<KingZone> OnDone;

	[SerializeField]
	int goldAmount;

	//Player player;

	void Start(){
		spoils = new Spoils (){ Gold = 10, Fame = 20, Powerups = new int[]{ } };
	}
		
	void OnTriggerEnter(Collider other){
		if (other.CompareTag ("Player")) {
			if (isServer) {
				StartCoroutine(MoveZone());
			}
			shipInZone++;
			print ("Increase: " + shipInZone);
		}
	}


	IEnumerator MoveZone() {
		yield return new WaitForSeconds(10f);
		if (OnDone != null) {
			OnDone (this);
		}
		Destroy (gameObject);
	}

	void OnTriggerExit(Collider other){
		if (other.CompareTag ("Player")) {
			if (shipInZone >= 0) {
				shipInZone--;
				print ("Decrease: " + shipInZone);
			}
		}
	}

	void OnTriggerStay(Collider other){
		if (other.CompareTag ("Player")) {
			if (shipInZone == 1) {
				if (Time.time > elapsedTime) {
					elapsedTime = Time.time + cooldown;

					if (isServer) {
						other.gameObject.GetComponent<Ship> ().player.ServerTakeSpoils (spoils);
					}
				}
			} 
		}
	}
}