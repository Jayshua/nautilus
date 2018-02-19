using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NewNetworkManager : NetworkManager {
	public override void OnStartClient (UnityEngine.Networking.NetworkClient client) {
		if (this.gameObject.GetComponent<NautilusNetworkManager> () == null) {
			this.gameObject.AddComponent<NautilusNetworkManager> ();
		}
	}

	public override void OnStartServer() {
		if (this.gameObject.GetComponent<NautilusNetworkManager> () == null) {
			this.gameObject.AddComponent<NautilusNetworkManager> ();
		}
	}
}
