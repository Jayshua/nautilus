using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;


public class NautilusNetworkManager : UnityEngine.Networking.NetworkManager
{
	public event Action<Player> OnPlayerJoin;
	public event Action<Player> OnPlayerLeave;

	// Add the new player to the active players list
	public override void OnServerAddPlayer(NetworkConnection connection, short playerControllerId) {
		base.OnServerAddPlayer (connection, playerControllerId);

		if (this.OnPlayerJoin != null) {
			Player newPlayer = connection.playerControllers [playerControllerId].gameObject.GetComponent<Player> ();
			this.OnPlayerJoin (newPlayer);
		}
	}

	// Remove the player from the active player list
	public override void OnServerRemovePlayer(NetworkConnection connection, PlayerController playerController) {
		if (OnPlayerLeave != null) {
			var leavingPlayer = playerController.gameObject.GetComponent<Player> ();
			this.OnPlayerLeave (leavingPlayer);
		}

		base.OnServerRemovePlayer (connection, playerController);
	}
}