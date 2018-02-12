using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Linq;

// The Server's GameController class
// Keeps track of the active players and will start and end events.
public class NautilusServer
{
	List<Player> activePlayers = new List<Player> ();
	GameObject[] shipPrefabs;
	GameObject playerPrefab;

	public NautilusServer (GameObject[] shipPrefabs, GameObject playerPrefab)
	{
		this.shipPrefabs = shipPrefabs;
		this.playerPrefab = playerPrefab;
		NetworkServer.RegisterHandler (MsgTypes.SelectName, HandleNameSelected);
		NetworkServer.RegisterHandler (MsgTypes.SelectClass, HandleClassSelected);
	}

	// Called by the NetworkManager when a client disconnects.
	// Remove the client's player from the active player list.
	public void ClientDisconnected(NetworkConnection disconnectedConnection) {
		activePlayers = activePlayers
			.Where (player => {
				if (player.connectionToClient == disconnectedConnection) {
					player.Destroy();
					return false;
				} else {
					return true;
				}
			}).ToList ();
	}

	// Add the player to the list of selected players if their chosen name is not taken.
	// Notify them whether their name was taken.
	void HandleNameSelected (NetworkMessage nameMessage)
	{
		string name = nameMessage.ReadMessage<StringMessage> ().value;
		bool nameUsed = activePlayers.Any (player => player.name.Equals (name));

		if (!nameUsed) {
			var newPlayer = GameObject.Instantiate (playerPrefab);
			NetworkServer.Spawn (newPlayer);
			var playerObject = newPlayer.GetComponent<Player> ();
			playerObject.Setup (name);
			activePlayers.Add (playerObject);
		}

		nameMessage.conn.Send (
			MsgTypes.IsNameOk,
			new MsgTypes.BooleanMessage (!nameUsed)
		);
	}

	// Set the player's chosen class and spawn them into the game.
	void HandleClassSelected (NetworkMessage prefabMessage)
	{
		MsgTypes.SelectClassMsg msg = prefabMessage.ReadMessage<MsgTypes.SelectClassMsg> ();
		GameObject player = null;

		switch (msg.prefabIndex) {
		case ClassType.SmallShip:
			player = (GameObject)Object.Instantiate (shipPrefabs [0]);
			break;
		case ClassType.MediumShip:
			player = (GameObject)Object.Instantiate (shipPrefabs [1]);
			break;
		case ClassType.LargeShip:
			player = (GameObject)Object.Instantiate (shipPrefabs [2]);
			break;
		}

		NetworkServer.AddPlayerForConnection (prefabMessage.conn, player, 0);
		activePlayers.First (p => p.GetComponent<NetworkIdentity> ().connectionToClient == prefabMessage.conn);
	}
}
