using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;



// Controls the network communication on both the client and the server
// Delegates most of the work to the NautilusClient and NautilusServer
// classes which run on the client and the server respectively.
public class NautilusNetworkManager : UnityEngine.Networking.NetworkManager
{
	public GameObject[] eventPrefabs;
	public GameObject nautilusPlayerPrefab;

	public List<Player> activePlayers = new List<Player> ();


	/**** Server Code ****/
	// Initialize the server class on the server
	public override void OnStartServer ()
	{
		NetworkServer.RegisterHandler (MsgTypes.SelectName, HandleNameSelectedServer);
		base.OnStartServer ();
	}

	// Handle a name being chosen on a client
	void HandleNameSelectedServer (NetworkMessage nameMessage)
	{
		string name = nameMessage.ReadMessage<StringMessage> ().value;
		bool nameUsed = activePlayers.Any (player => player.name.Equals (name));

		if (!nameUsed) {
			var newPlayer = GameObject.Instantiate (nautilusPlayerPrefab);
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

	// Remove the client's player from the active player list.
	public override void OnServerDisconnect(NetworkConnection disconnectedConnection) {
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



	/**** Client Code ****/
	UserInterface userInterface;
	// Initialize the client class on the client
	public override void OnStartClient (UnityEngine.Networking.NetworkClient client)
	{
		this.userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface> ();

		// Show the class selection screen if the name is not taken, or an error if it is.
		client.RegisterHandler (MsgTypes.IsNameOk, message => {
			bool isOk = message.ReadMessage<MsgTypes.BooleanMessage> ().value;

			if (!isOk) {
				userInterface.ShowNameSelection (true);
			}
		});

		// Send a name selected message to the client when a name is selected
		this.userInterface.OnNameSelected += name => {
			Debug.Log("Called onnameselected: " + name);
			client.Send (MsgTypes.SelectName, new StringMessage (name));
		};

		this.userInterface.ShowNameSelection (false);

		// So when the server calls AddPlayerForConnection when creating the player it is
		// required to pass a controllerID. That controllerID is used to support multiple
		// players on a single machine playing over the network. Since this game only supports
		// a single player on a machine, that controllerID is always 0. However, since that
		// controller was never explicetly created, Unity throws a warning in the console saying so.
		// It was getting annoying. The internet says this is a safe way to get rid of it.
		// http://answers.unity.com/questions/1084534/playercontrollerid-higher-than-expected.html
		ClientScene.localPlayers.Add (null);

		//this.nautilusClient = new NautilusClient (client);
		base.OnStartClient (client);
	}
}