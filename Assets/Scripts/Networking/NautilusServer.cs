using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Linq;

public class NautilusServer {
	List<Player> activePlayers = new List<Player>();
	GameObject[] shipPrefabs;

	public NautilusServer(GameObject[] shipPrefabs) {
		this.shipPrefabs = shipPrefabs;
		NetworkServer.RegisterHandler (MsgTypes.SelectName, HandleNameSelectedServer);
		NetworkServer.RegisterHandler (MsgTypes.PlayerPrefabSelect, HandleClassSelectedServer);
	}

	void HandleNameSelectedServer (NetworkMessage nameMessage)
	{
		string name = nameMessage.ReadMessage<StringMessage> ().value;
		bool nameUsed = activePlayers.Any (player => player.name.Equals (name));

		if (!nameUsed) {
			activePlayers.Add (new Player (name));
		}

		nameMessage.conn.Send (
			MsgTypes.IsNameOk,
			new MsgTypes.BooleanMessage (!nameUsed)
		);
	}

	void HandleClassSelectedServer (NetworkMessage prefabMessage)
	{
		MsgTypes.PlayerPrefabMsg msg = prefabMessage.ReadMessage<MsgTypes.PlayerPrefabMsg> ();
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
	}
}
