using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// At the moment, this class should only be used on the server.
public class Player : NetworkBehaviour
{
	public string playerName { get; private set; }
	public NetworkConnection playerConnection;

	GameObject _playerObject;
	public GameObject playerObject {
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
		GUI.ItemUsed += UsePowerUp;
	}

	[Server]
	public void SendNotification(string message) {
		this.TargetSendNotification (playerConnection, message);
	}

	[TargetRpc]
	private void TargetSendNotification(NetworkConnection connection, string message) {
		if (OnNotification != null) {
			OnNotification (message);
		}
	}

	public void Destroy() {
		GameObject.Destroy (playerObject);
		GameObject.Destroy (this);
	}

	public void AddPowerUps(List <PowerUps> powerUps)
	{
		Inventory.AddRange (powerUps);
		OnChangePowerups (Inventory);
	}

	public void UsePowerUp(PowerUps powerUp)
	{
		if (Inventory.Remove (powerUp)) {
			OnChangePowerups (Inventory);

		}
	}
}