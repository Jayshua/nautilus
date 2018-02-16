using System;
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

	public List<PowerUps> Inventory = new List<PowerUps>() {
		PowerUps.Spyglass, PowerUps.PowderKeg, PowerUps.CannonShot, PowerUps.LemonJuice, PowerUps.WindBucket
	};

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
				OnAddPowerups (this);
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
		
	public event Action<Player> OnDeath;
	public event Action<Player> OnLogout;
	public event Action<Player> OnGoldChange;
	public event Action<Player> OnFameChange;
	public event Action<Player> OnAddPowerups;

	[Server]
	public void Setup(string playerName, NetworkConnection playerConnection) {
		if (this.playerName == null || this.playerConnection == null ) {
			this.playerName = playerName;
			this.playerConnection = playerConnection;
			this.TargetSetAuthority (playerConnection);
		} else {
			throw new Exception ("Called setup on an Player object that has already been setup. The player was: " + playerName);
		}
	}

	[TargetRpc]
	private void TargetSetAuthority(NetworkConnection connection) {
		var GUI = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
		GUI.PlayerConnected (this);
	}

	public void Destroy() {
		GameObject.Destroy (playerObject);
		GameObject.Destroy (this);
	}
}