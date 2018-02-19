using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;



// Controls the network communication on both the client and the server
// Delegates most of the work to the NautilusClient and NautilusServer
// classes which run on the client and the server respectively.
public class NautilusNetworkManager : UnityEngine.Networking.NetworkManager
{
	public List<Player> activePlayers = new List<Player> ();
	public event Action<Player> OnPlayerJoin;
	public event Action<Player> OnPlayerLeave;

	// Add the new player to the active players list
	public override void OnServerAddPlayer(NetworkConnection connection, short playerControllerId) {
		base.OnServerAddPlayer (connection, playerControllerId);

		Player newPlayer = connection.playerControllers [playerControllerId].gameObject.GetComponent<Player> ();
		activePlayers.Add (newPlayer);

		if (this.OnPlayerJoin != null) {
			this.OnPlayerJoin (newPlayer);
		}
	}

	// Remove the player from the active player list
	public override void OnServerRemovePlayer(NetworkConnection connection, PlayerController playerController) {
		var leavingPlayer = playerController.gameObject.GetComponent<Player> ();

		// Also remove from the list
		if (OnPlayerLeave != null) {
			this.OnPlayerLeave (leavingPlayer);
		}

		activePlayers = activePlayers
			.Where (player => player.connectionToClient != connection)
			.ToList ();

		base.OnServerRemovePlayer (connection, playerController);
	}
}