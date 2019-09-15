using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Zone : NetworkBehaviour {

	bool entered = false;

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player")) {
			if (other.GetComponent<NetworkIdentity>().hasAuthority) {
				other.GetComponent<Player> ().TakeSpoils (new Spoils () {
					Gold = 50,
					Fame = 50,
					Powerups = new int[] { }
				});

				if (!entered) {
					entered = true;
					this.tag = "Untagged";
				}
			}	
		}
	}
}
