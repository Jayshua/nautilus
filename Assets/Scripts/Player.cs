using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
	public GameObject SmallShipPrefab;
	public GameObject MediumShipPrefab;
	public GameObject LargeShipPrefab;

	GameObject _playerObject;
	public GameObject playerShip {
		get {
			return _playerObject;
		}

		set {
			if (value == null && this.OnKeel != null) {
				this.OnKeel (this._playerObject);
			} else {
				this.OnLaunch (value);
			}
			this._playerObject = value;
		}
	}

	private List<PowerUps> Inventory = new List<PowerUps>() { };

	[SyncVar]
	public string playerName;


	[SyncVar]
	int _gold;

	public int Gold {
		get {
			return this._gold;
		}

		set {
			this._gold = value;

			if (OnGoldChange != null) {
				OnGoldChange (this);
			}
		}
	}

	[SyncVar]
	int _fame;

	public int Fame {
		get{
			return this._fame;
		}
		set {
			this._fame = value;

			if (OnFameChange != null) {
				OnFameChange (this);
			}
		}
	}

	public event Action<Player> OnLogout;
	public event Action<Player> OnGoldChange;
	public event Action<Player> OnFameChange;

	public event Action<List <PowerUps>> OnChangePowerups;

	public event Action<Player> OnAddPowerups;
	public event Action<string> OnNotification;
	public event Action<GameObject> OnLaunch;
	public event Action<GameObject> OnKeel;



	public override void OnStartAuthority() {
		var userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
		userInterface.PlayerConnected (this);
		userInterface.OnItemUsed += HandleItemUsed;
		userInterface.OnClassSelected += CmdSpawnWithClass;
	}

	// Spawn ship prefab upon class selected
	[Command]
	void CmdSpawnWithClass(ClassType type) {
		GameObject ship = null;
		switch (type) {
		case ClassType.SmallShip:
			ship = (GameObject)Instantiate (SmallShipPrefab);
			break;
		case ClassType.MediumShip:
			ship = (GameObject)Instantiate (MediumShipPrefab);
			break;
		case ClassType.LargeShip:
			ship = (GameObject)Instantiate (LargeShipPrefab);
			break;
		}

		this.playerShip = ship;
		NetworkServer.SpawnWithClientAuthority (ship, this.connectionToClient);
		TargetShipSpawned (this.connectionToClient, ship.GetComponent<NetworkIdentity>().netId);
	}

	// Setup the local player state when a ship is spawned
	[TargetRpc]
	void TargetShipSpawned(NetworkConnection connection, NetworkInstanceId id) {
		this.playerShip = ClientScene.FindLocalObject (id);
		if (this.playerShip = null) {
			Debug.LogError ("Unable to find the local player. It ought to have had the id: " + id.ToString ());
			return;
		}

		if (this.OnLaunch != null) {
			this.OnLaunch (this.playerShip);
		}
	}



	[Server]
	public void SendNotification(string message) {
		Debug.LogError ("Called SendNotification which has not yet been implemented.");
		//this.TargetSendNotification (playerConnection, message);
	}

	[TargetRpc]
	private void TargetSendNotification(NetworkConnection connection, string message) {
		if (OnNotification != null) {
			OnNotification (message);
		}
	}



	public void Destroy() {
		GameObject.Destroy (playerShip);
		GameObject.Destroy (this);
	}

	public void AddPowerUps(List <PowerUps> powerUps)
	{
		Inventory.AddRange (powerUps);
		OnChangePowerups (Inventory);
	}

	void HandleItemUsed(PowerUps powerUp)
	{
		if (Inventory.Remove (powerUp)) {
			OnChangePowerups (Inventory);

		}
	}
}