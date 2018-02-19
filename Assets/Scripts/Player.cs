﻿using System;
using System.Linq;
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
	public event Action<int, int, SyncListInt> OnStatsChange;
	// Triggered when the player chooses a class and their ship is launched
	public event Action<Ship> OnLaunch;
	#endregion


	#region Properties
	UserInterface Gui;

	// The player's ship. Will be null if the player hasn't chosen a ship yet or their ship has sunk.	
	Ship playerShip;

	[SyncVar] public string playerName;
	SyncListInt Inventory = new SyncListInt();
	[SyncVar(hook="HandleGoldChanged")] int _gold;
	[SyncVar] int _fame;
	#endregion Properties

	void HandleGoldChanged(int newgold) {
		if (this.OnStatsChange != null) {
			this.OnStatsChange (this.Gold, this._fame, this.Inventory);
		}
		Debug.Log ("Gold Changed. Gold: " + this.Gold.ToString() + ", NewGold: " + newgold.ToString() + "Authority: " + this.hasAuthority + ", Is Server: " + this.isServer + ", Is Client: " + this.isClient + ", Is LocalPlayer: " + this.isLocalPlayer);
	}


	#region Accessors
	// Accessors that trigger their corresponding event when changed
	public int Gold {
		set { _gold = value; }
		get { return _gold; }
	}

	public int Fame {
		get {return this._fame;}
		set {this._fame = value;}
	}
	#endregion


	[Client]
	public override void OnStartAuthority() {
		this.Gui = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
		this.Gui.PlayerConnected (this);
		this.Gui.OnItemUsed += HandleItemUsed;
		this.Gui.OnClassSelected += type => CmdSpawnWithClass(type);
		this.Gui.OnNameSelected += name => CmdSelectName(name);
	}

	[Command]
	void CmdSelectName(string newName) {
		var otherPlayersWithName = FindObjectOfType<NautilusNetworkManager> ().activePlayers
			.Where (player => player.name == newName)
			.Count ();

		if (otherPlayersWithName > 0) {
			TargetNameTaken (this.connectionToClient, newName);
		} else {
			this.playerName = name;
			TargetNameSet (this.connectionToClient, newName);
		}
	}

	[TargetRpc]
	void TargetNameTaken(NetworkConnection connection, string name) {
		Gui.ShowNotification ("That name is taken! Please try again.");
	}

	[TargetRpc]
	void TargetNameSet(NetworkConnection connection, string name) {
		Gui.ShowClassSelection();
	}


	[Client]
	void HandleItemUsed(PowerUps powerUp)
	{
		if (Inventory.Remove ((int)powerUp)) {
			if (this.OnStatsChange != null) {
				this.OnStatsChange (this.Fame, this.Gold, this.Inventory);
			}
		}
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
		TargetShipSpawned (this.connectionToClient, ship);
	}

	// Setup the local player state when a ship is spawned
	[TargetRpc]
	void TargetShipSpawned(NetworkConnection connection, GameObject ship) {
		this.playerShip = ship.GetComponent<Ship> ();
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

		foreach (var item in chestScript.ChestPowerups) {
			this.Inventory.Add ((int)item);
		}
	}

	[Server]
	public void SendNotification(string message) {
		Debug.LogError ("Called SendNotification which has not yet been implemented.");
		//this.TargetSendNotification (playerConnection, message);
	}

	[Server]
	public void Destroy() {
		GameObject.Destroy (playerShip.gameObject);
		GameObject.Destroy (this);
	}
}