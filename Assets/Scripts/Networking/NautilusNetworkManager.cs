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
	public GameObject[] shipPrefabs;
	public GameObject nautilusPlayerPrefab;
	NautilusClient nautilusClient;
	NautilusServer nautilusServer;

	// Initialize the server class on the server
	public override void OnStartServer ()
	{
		this.nautilusServer = new NautilusServer (shipPrefabs, nautilusPlayerPrefab);
		base.OnStartServer ();
	}

	// Initialize the client class on the client
	public override void OnStartClient (UnityEngine.Networking.NetworkClient client)
	{
		this.nautilusClient = new NautilusClient (client);
		base.OnStartClient (client);
	}

	// Remove a player
	public override void OnServerDisconnect(NetworkConnection connection) {
		nautilusServer.ClientDisconnected (connection);
	}
}