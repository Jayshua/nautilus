using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
	// Editor Configuration Points
	#region Editor Variables
	public GameObject SmallShipPrefab;
	public GameObject MediumShipPrefab;
	public GameObject LargeShipPrefab;
	#endregion


	#region Events
	// Triggered when the player logs out, just before this object is destroyed
	public event Action<Player> OnLogout;
	// Triggered when the player's fame, gold, or powerups change. Argument order is (Fame, Gold, Powerups)
	public event Action<int, int, List<PowerUps>> OnStatsChange;
	// Triggered when a notification to display to the user is recieved
	public event Action<string> OnNotification;
	// Triggered when the player chooses a class and their ship is launched
	public event Action<Ship> OnLaunch;
	#endregion


	#region Properties
	// The player's ship. Will be null if the player hasn't chosen a ship yet or their ship has sunk.	
	Ship playerShip;

	[SyncVar] public string playerName;
	List<PowerUps> Inventory = new List<PowerUps>();
	[SyncVar(hook="HandleGoldChanged")] int _gold;
	[SyncVar] int _fame;
	#endregion Properties

	void HandleGoldChanged(int newgold) {
		Debug.Log ("Gold Changed. Gold: " + this.Gold.ToString() + ", NewGold: " + newgold.ToString() + "Authority: " + this.hasAuthority + ", Is Server: " + this.isServer + ", Is Client: " + this.isClient + ", Is LocalPlayer: " + this.isLocalPlayer);
	}


	#region Accessors
	// Accessors that trigger their corresponding event when changed
	public int Gold {
		get {return this._gold;}
		set {
			this._gold = value;

			Debug.Log ("gold Handler. Is Null: " + (OnStatsChange == null).ToString ());
			if (OnStatsChange != null) {
				OnStatsChange(this.Fame, this.Gold, this.Inventory);
			}
		}
	}

	public int Fame {
		get {return this._fame;}
		set {
			this._fame = value;

			if (OnStatsChange != null) {
				OnStatsChange (this.Fame, this.Gold, this.Inventory);
			}
		}
	}

	// This acts as an accessor for the inventory list
	[Client]
	public void AddPowerUps(List <PowerUps> powerUps)
	{
		Inventory.AddRange (powerUps);

		if (this.OnStatsChange != null) {
			this.OnStatsChange (this.Fame, this.Gold, this.Inventory);
		}
	}

	// This acts as an accessor for the inventory list
	[Client]
	void HandleItemUsed(PowerUps powerUp)
	{
		if (Inventory.Remove (powerUp)) {
			if (this.OnStatsChange != null) {
				this.OnStatsChange (this.Fame, this.Gold, this.Inventory);
			}
		}
	}
	#endregion


	[Client]
	public override void OnStartAuthority() {
		var userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
		userInterface.PlayerConnected (this);
		userInterface.OnItemUsed += HandleItemUsed;
		userInterface.OnClassSelected += type => CmdSpawnWithClass(type);
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

		this.playerShip = ship.GetComponent<Ship>();
		NetworkServer.SpawnWithClientAuthority (ship, this.connectionToClient);
		TargetShipSpawned (this.connectionToClient, ship.GetComponent<NetworkIdentity>().netId);
	}

	// Setup the local player state when a ship is spawned
	[TargetRpc]
	void TargetShipSpawned(NetworkConnection connection, NetworkInstanceId id) {
		var shipObject = ClientScene.FindLocalObject (id);
		if (shipObject == null) {
			Debug.LogError ("Unable to find the local player. It ought to have had the id: " + id.ToString ());
			return;
		}

		this.playerShip = shipObject.GetComponent<Ship> ();
		this.playerShip.OnChestGet += HandleChestGet;
		this.playerShip.OnKeel += HandleShipKeel;

		if (this.OnLaunch != null) {
			this.OnLaunch (this.playerShip);
		}
	}

	// Detach event listeners when the player's ship sinks
	[Client]
	void HandleShipKeel(Ship ship) {
		ship.OnChestGet -= HandleChestGet;
		ship.OnKeel -= HandleShipKeel;
	}

	// Add stats to the player when they collide with a chest
	[Client]
	void HandleChestGet(Chest chest) {
		Debug.Log ("Ship Collision with Chest in player class");
		CmdUpdateStats (chest.gameObject);
	}

	[Command]
	void CmdUpdateStats(GameObject chest) {
		Debug.Log ("Cmd Update States");
		var chestScript = chest.GetComponent<Chest> ();
		this.Fame += chestScript.fame;
		this.Gold += chestScript.gold;
		this.Inventory.AddRange (chestScript.ChestPowerups);
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


	[Server]
	public void Destroy() {
		GameObject.Destroy (playerShip.gameObject);
		GameObject.Destroy (this);
	}


}