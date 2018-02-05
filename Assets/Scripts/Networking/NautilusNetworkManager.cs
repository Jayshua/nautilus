using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class NautilusNetworkManager : UnityEngine.Networking.NetworkManager
{
	public UserInterface userInterface;
	public GameObject[] shipPrefabs;
	NautilusClient client;
	NautilusServer server;

	public override void OnStartServer ()
	{
		this.server = new NautilusServer (shipPrefabs);
		base.OnStartServer ();
	}

	public override void OnStartClient (UnityEngine.Networking.NetworkClient client)
	{
		this.client = new NautilusClient(client);
		base.OnStartClient (client);
	}
}