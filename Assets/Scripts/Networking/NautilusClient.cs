using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

// The client's game controller class
// Keeps track of communication between the server and the client,
// displaying notifications and such.
public class NautilusClient
{
	NetworkClient networkClient;
	UserInterface userInterface;

	public NautilusClient (NetworkClient client)
	{
		this.networkClient = client;
		this.userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface> ();

		client.RegisterHandler (MsgTypes.IsNameOk, HandleNameIsOk);
		userInterface.ShowNameSelection (HandleNameSelectedClient, false);

		// So when the server calls AddPlayerForConnection when creating the player it is
		// required to pass a controllerID. That controllerID is used to support multiple
		// players on a single machine playing over the network. Since this game only supports
		// a single player on a machine, that controllerID is always 0. However, since that
		// controller was never explicetly created, Unity throws a warning in the console saying so.
		// It was getting annoying. The internet says this is a safe way to get rid of it.
		// http://answers.unity.com/questions/1084534/playercontrollerid-higher-than-expected.html
		ClientScene.localPlayers.Add (null);
	}

	// Run when the server responds to a SelectName message.
	// Show the class selection screen if the name is not taken, or an error if it is.
	void HandleNameIsOk (NetworkMessage message)
	{
		bool isOk = message.ReadMessage<MsgTypes.BooleanMessage> ().value;

		if (isOk) {
			userInterface.ShowClassSelection (HandleClassSelectedClient);
		} else {
			userInterface.ShowNameSelection (HandleNameSelectedClient, true);
		}
	}

	// Send name request message to the server when the UI indicates a name
	// has been selected.
	void HandleNameSelectedClient (string name)
	{
		networkClient.Send (MsgTypes.SelectName, new StringMessage (name));
	}

	// Send a class selection message to the server when the UI indicates
	// a class has been selected.
	void HandleClassSelectedClient (ClassType chosenClass)
	{
		networkClient.Send (MsgTypes.SelectClass, new MsgTypes.SelectClassMsg (chosenClass));
	}
}
