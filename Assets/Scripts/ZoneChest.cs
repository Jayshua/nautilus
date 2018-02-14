using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ZoneChest : NetworkBehaviour {

	void OnTriggerEnter(Collider other)
	{
		if (other.GetComponent<NetworkIdentity> ().isLocalPlayer) {
			print ("Player get extra 50 gold");
		}
		Destroy (this.gameObject);
	}
}
