﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Represents a player on the Server.
// At the moment, this class should only be used on the server.
public class Player : NetworkBehaviour
{
	public string playerName { get; private set; }
	public NetworkConnection playerConnection;
	public GameObject playerObject {get; set;}

	event Action OnDeath;
	event Action OnGoldChange;
	event Action OnFameChange;
	event Action OnHealthChange;
	event Action OnAchievement;

	public List<PowerUps> Inventory = new List<PowerUps>() {
		PowerUps.Spyglass, PowerUps.PowderKeg, PowerUps.CannonShot, PowerUps.LemonJuice, PowerUps.WindBucket
	};

	void Start()
	{
		if (isLocalPlayer) {
			var GUI = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
			GUI.PlayerConnected (this);
		}
	}

	void Update()
	{

	}

	[SyncVar]
	public int Gold;
	[SyncVar]
	public int Fame;

	[Server]
	public void Setup(string playerName, NetworkConnection playerConnection) {
		if (this.playerName == null || this.playerConnection == null ) {
			this.playerName = playerName;
			this.playerConnection = playerConnection;
		} else {
			throw new Exception ("Called setup on an Player object that has already been setup. The player was: " + playerName);
		}
	}

	public void Destroy() {
		GameObject.Destroy (playerObject);
		GameObject.Destroy (this);
	}
}