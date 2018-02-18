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
	public List<Player> activePlayers = new List<Player> ();
	GameObject[] shipPrefabs;
	GameObject[] eventPrefabs;
	GameObject playerPrefab;

	public NautilusServer (GameObject[] shipPrefabs, GameObject[] eventPrefabs, GameObject playerPrefab)
	{
		this.shipPrefabs = shipPrefabs;
		this.eventPrefabs = eventPrefabs;
		this.playerPrefab = playerPrefab;
		NetworkServer.RegisterHandler (MsgTypes.SelectName, HandleNameSelected);
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
			var playerObject = newPlayer.GetComponent<Player> ();
			playerObject.playerName = name;
			NetworkServer.AddPlayerForConnection (nameMessage.conn, newPlayer, 0);
			activePlayers.Add (playerObject);
		}

		nameMessage.conn.Send (
			MsgTypes.IsNameOk,
			new MsgTypes.BooleanMessage (!nameUsed)
		);
	}
}