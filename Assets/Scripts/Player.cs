using UnityEngine;
using UnityEngine.Networking;

// Represents a player on the Server.
// At the moment, this class should only be used on the server.
public class Player
{
	public string name { get; private set; }
	public NetworkConnection connection { get; private set; }
	public GameObject playerObject {get; set;}

	public Player (string name, NetworkConnection connection)
	{
		this.name = name;
		this.connection = connection;
	}

	public void Destroy() {
		GameObject.Destroy (playerObject);
	}
}