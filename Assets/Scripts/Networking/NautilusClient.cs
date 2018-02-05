using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class NautilusClient {
	NetworkClient client;
	UserInterface userInterface;

	public NautilusClient(NetworkClient client) {
		this.client = client;
		this.userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface>();

		client.RegisterHandler (MsgTypes.IsNameOk, HandleNameIsOk);
		userInterface.ShowNameSelection (HandleNameSelectedClient, false);
	}

	void HandleNameIsOk(NetworkMessage message)
	{
		bool isOk = message.ReadMessage<MsgTypes.BooleanMessage> ().value;

		if (isOk) {
			userInterface.ShowClassSelection (HandleClassSelectedClient);
		} else {
			userInterface.ShowNameSelection (HandleNameSelectedClient, true);
		}
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

}
