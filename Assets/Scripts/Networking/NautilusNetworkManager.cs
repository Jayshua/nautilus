using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;


public class NautilusNetworkManager : UnityEngine.Networking.NetworkManager
{
	// Triggered when a new client connects. (Not when a player clicks the "Weight Anchor" button.)
	// Only runs on the server
	public event Action<Player> OnClientConnected;

	// Triggered when a client disconnects. (Not when a player clicks the "Logout" button.)
	// Only runs on the server
	public event Action<Player> OnClientDisconnected;

	// Trigger the OnClientConnect event
	public override void OnServerAddPlayer (NetworkConnection connection, short playerControllerId)
	{
		base.OnServerAddPlayer (connection, playerControllerId);

		if (this.OnClientConnected != null) {
			Player newPlayer = connection.playerControllers [playerControllerId].gameObject.GetComponent<Player> ();
			this.OnClientConnected (newPlayer);
		}
	}

	// This should be done in the OnServerRemovePlayer function "technically", but that function isn't run
	// when a player disconnects manually.
	public override void OnServerDisconnect (NetworkConnection connection)
	{
		Debug.Log ("Player Disconnected");

		bool foundPlayer = false;
		foreach (var objectID in connection.clientOwnedObjects) {
			var potentialPlayerObject = NetworkServer.FindLocalObject (objectID);
			if (potentialPlayerObject != null) {
				var potentialPlayer = potentialPlayerObject.GetComponent<Player> ();
				if (potentialPlayer != null) {
					Debug.Log ("Found player object");
					if (this.OnClientDisconnected != null) {
						Debug.Log ("Calling disconnect event");
						this.OnClientDisconnected (potentialPlayer);
					}
					foundPlayer = true;
				}
			}
		}

		if (!foundPlayer) {
			Debug.Log ("didn't find the player object :(");
		}

		base.OnServerDisconnect (connection);
	}
}