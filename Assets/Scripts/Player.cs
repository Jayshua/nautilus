using System;
using UnityEngine;
using UnityEngine.Networking;

// Represents a player on the Server.
// At the moment, this class should only be used on the server.
public class Player : NetworkBehaviour
{
	public string playerName { get; private set; }
	public GameObject playerObject {get; set;}

	[SyncVar]
	private int Gold;
	[SyncVar]
	private int Fame;

	[Server]
	public void Setup(string name) {
		if (playerName == null) {
			this.playerName = name;
		} else {
			throw new Exception ("Called setup on an Player object that has already been setup. The player was: " + playerName);
		}
	}

	public void Destroy() {
		GameObject.Destroy (playerObject);
		GameObject.Destroy (this);
	}
}