using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KingZone : MonoBehaviour {

	int shipInZone = 0;
	float elapsedTime = 0f;
	public float cooldown = 1f;

	[SerializeField]
	int goldAmount;

	Player player;

	void Start(){
		player = GameObject.Find ("Player").GetComponent<Player> ();
	}

	void OnTriggerEnter(Collider other){
		shipInZone++;
		print ("Increase: " + shipInZone);
	}

	void OnTriggerExit(Collider other){
		if (shipInZone >= 0) {
			shipInZone--;
			print ("Decrease: " + shipInZone);
		}
	}

	void OnTriggerStay(Collider other){
		if (shipInZone == 1) {
			if (Time.time > elapsedTime) {
				elapsedTime = Time.time + cooldown;
				player.Gold += goldAmount;
			}
		} 
	}
}