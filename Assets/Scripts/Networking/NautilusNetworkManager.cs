using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

// Controls the network communication on both the client and the server
// Delegates most of the work to the NautilusClient and NautilusServer
// classes which run on the client and the server respectively.
public class NautilusNetworkManager : UnityEngine.Networking.NetworkBehaviour
{
	public GameObject[] eventPrefabs;
	public GameObject nautilusPlayerPrefab;

	public List<Player> activePlayers;
	public event Action<Player> OnPlayerJoin;


	/**** Server ****/
	[Command]
	void CmdPlayerLogin(string playerName) {
		bool nameUsed = activePlayers.Any (p => p.name.Equals (playerName));

		if (nameUsed) {
			RpcPlayerNameTaken(playerName);
		} else {
			var newPlayer = Instantiate (nautilusPlayerPrefab);
			NetworkServer.Spawn (newPlayer);
			var newPlayerScript = newPlayer.GetComponent<Player>();
			if (this.OnPlayerJoin != null) {
				this.OnPlayerJoin(newPlayerScript);
			}
			var newPlayerIdentity = newPlayer.GetComponent<NetworkIdentity> ().netId;
			RpcPlayerLoggedIn (newPlayerIdentity);
		}
	}

	[Command]
	void CmdPlayerLogout(NetworkInstanceId id) {
		var player = NetworkServer.FindLocalObject (id).GetComponent<Player> ();
		this.activePlayers = this.activePlayers.Where (p => p != player).ToList ();
	}


	/**** Client ****/
	UserInterface userInterface;
	public void OnStartClient ()
	{
		Debug.Log ("HERE");
		userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface>();
		userInterface.OnNameSelected += HandleNameSelected;
	}

	void HandleNameSelected(string name) {
		CmdPlayerLogin (name);
	}

	[ClientRpc]
	void RpcPlayerLoggedIn(NetworkInstanceId id) {
		var player = ClientScene.FindLocalObject (id).GetComponent<Player>();
		if (OnPlayerJoin != null) {
			OnPlayerJoin (player);
		}
		player.OnLogout += (logoutPlayer) => {
			CmdPlayerLogout(
				logoutPlayer.GetComponent<NetworkIdentity>().netId
			);
		};
	}

	[ClientRpc]
	void RpcPlayerNameTaken(string name) {
		this.userInterface.ShowNameSelection (true);
	}
}