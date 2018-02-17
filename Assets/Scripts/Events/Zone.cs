using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Zone : NetworkBehaviour {

	bool entered = false;

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player")) {
			if (other.GetComponent<NetworkIdentity>().isLocalPlayer){
				if (!entered)
				{
					entered = true;
					this.tag = "Untagged";
				}
			}	
		}
	}
}
