using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class MsgTypes
{
	public const short PlayerPrefabSelect = MsgType.Highest + 1;
	public const short SelectName = MsgType.Highest + 2;
	public const short IsNameOk = MsgType.Highest + 3;

	public class BooleanMessage : MessageBase
	{
		public bool value;

		public BooleanMessage() {value = false;}

		public BooleanMessage (bool value)
		{
			this.value = value;
		}
	}

	public class PlayerPrefabMsg : MessageBase
	{
		public ClassType prefabIndex;
	}
}

class Player
{
	public string name { get; private set; }

	public Player (string name)
	{
		this.name = name;
	}
}

public class NautilusNetworkManager : UnityEngine.Networking.NetworkManager
{
	private List<Player> activePlayers = new List<Player>();
	public UserInterface userInterface;

	public override void OnStartServer ()
	{
		NetworkServer.RegisterHandler (MsgTypes.SelectName, HandleNameSelectedServer);
		NetworkServer.RegisterHandler (MsgTypes.PlayerPrefabSelect, HandleClassSelectedServer);
		base.OnStartServer ();
	}

	public override void OnStartClient (UnityEngine.Networking.NetworkClient client)
	{
		client.RegisterHandler (MsgTypes.IsNameOk, message => {
			bool isOk = message.ReadMessage<MsgTypes.BooleanMessage> ().value;

			if (isOk) {
				userInterface.ShowClassSelection (HandleClassSelectedClient);
			} else {
				userInterface.ShowNameSelection (HandleNameSelectedClient, true);
			}
		});

		userInterface.ShowNameSelection (HandleNameSelectedClient, false);

		base.OnStartClient (client);
	}

	void HandleNameSelectedClient (string name)
	{
		client.Send (MsgTypes.SelectName, new StringMessage (name));
	}

	void HandleClassSelectedClient (ClassType chosenClass)
	{
		client.Send (MsgTypes.PlayerPrefabSelect, new MsgTypes.PlayerPrefabMsg () {
			prefabIndex = chosenClass
		});
	}


	void HandleNameSelectedServer (NetworkMessage nameMessage)
	{
		string name = nameMessage.ReadMessage<StringMessage> ().value;
		bool nameUsed = activePlayers.Any (player => player.name.Equals (name));

		if (!nameUsed) {
			activePlayers.Add (new Player(name));
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
			player = (GameObject)Instantiate (spawnPrefabs [0]);
			break;
		case ClassType.MediumShip:
			player = (GameObject)Instantiate (spawnPrefabs [1]);
			break;
		case ClassType.LargeShip:
			player = (GameObject)Instantiate (spawnPrefabs [2]);
			break;
		}
		NetworkServer.AddPlayerForConnection (prefabMessage.conn, player, 0);
	}
}