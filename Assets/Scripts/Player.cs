#region Using
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#endregion

public class Player : NetworkBehaviour
{

	#region Fields, Events, Syncvars, etc.

	#region Gold

	const float GOLD_MULTIPLIER = 1.5f;

	#endregion

	#region Prefabs

	public GameObject chestPrefab;

	#endregion

	#region Editor Variables

	public GameObject SmallShipPrefab;
	public GameObject MediumShipPrefab;
	public GameObject LargeShipPrefab;

	#endregion

	GameController gameController;

	bool hasSeenTutorial = false;

	#region Events

	// Triggered when the player logs out, just before this object is destroyed
	public event Action<Player> OnNetworkDisconnect;
	// Triggered when the player's fame, gold, or powerups change. Argument order is (Fame, Gold, Powerups)
	//public event Action<int, int, SyncListInt> OnStatsChange;
	// Triggered when the player chooses a class and their ship is launched
	public event Action<Ship> OnLaunch;
	public event Action<Player, Ship> OnShipKeel;
	public event Action<int> OnGoldChanged;
	public event Action<int> OnFameChanged;
	public event Action<int> OnRankChange;
	public event Action<SyncListInt> OnInventoryChanged;

	#endregion

	#region Properties

	UserInterface Gui;

	// The player's ship. Will be null if the player hasn't chosen a ship yet or their ship has sunk.
	// todo: Since the Ship game object has a network identity, I think that this field could be marked as a [SyncVar]...
	Ship playerShip;

	[SyncVar] public string playerName;
	// The player's name. Will be the empty string if no user is currently logged in with this player.
	SyncListInt Inventory = new SyncListInt ();
	[SyncVar (hook = "HandleGoldChanged")] public int Gold;
	[SyncVar (hook = "HandleFameChanged")] public int Fame;
	[SyncVar (hook = "HandleRankChanged")] public int Rank;

	#endregion

	#region Cleint UI Events

	// Client GUI Gold changed
	[Client]
	void HandleGoldChanged (int newGold)
	{
		if (this.OnGoldChanged != null) {
			this.OnGoldChanged (newGold);
		}
	}

	// Client GUI Fame changed
	[Client]
	void HandleFameChanged (int newFame)
	{
		if (newFame < 0) {
			newFame = 0;
		}
		if (this.OnFameChanged != null) {
			this.OnFameChanged (newFame);
		}
	}

	// Client GUI Items changed
	[Client]
	void HandleInventoryChanged (SyncListInt.Operation inventory, int index)
	{
		if (this.OnInventoryChanged != null) {
			this.OnInventoryChanged (this.Inventory);
		}
	}

	// Client GUI Rank changed
	[Client]
	void HandleRankChanged(int newRank) {
		if (Gui.CurrentScreen != GuiScreen.InGame) {
			return;
		}

		// Don't forget, these hooks will run on all clients
		if (this.isLocalPlayer) {
			// If moving up in rank
			if (newRank < this.Rank) {
				if (newRank == 1) {
					Gui.ShowNotification ("You've reached 1st place on the Fame Plank!", 5f);
				} else if (newRank == 2) {
					Gui.ShowNotification ("You've reached 2nd place on the Fame Plank!", 5f);
				} else if (newRank == 3) {
					Gui.ShowNotification ("You've reached 3rd place on the Fame Plank!", 5f);
				}
			}
			// If moving down in rank
			else {
				if (newRank <= 4) {
					Gui.ShowNotification ("Someone has surpassed you on the Fame Plank!", 5f);
				}
			}
		}

		this.Rank = newRank;

		if (this.OnRankChange != null) {
			this.OnRankChange (newRank);
		}
	}

	#endregion

	#region Accessors

	// Accessors that trigger their corresponding event when changed

	#endregion

	#endregion

	#region Functions and Methods

	#region Standard Functions (Start, OnStartAuthority)

	// Subscribe to events and find the GUI
	[Client]
	public override void OnStartAuthority ()
	{
		GameObject.Find ("Network Manager").GetComponent<NetworkManagerHUD> ().showGUI = false;
		this.Gui = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
		this.Gui.PlayerConnected (this);
		this.Gui.OnItemUsed      += HandleItemUsed;
		this.Gui.OnClassSelected += HandleClassSelected;
		this.Gui.OnNameSelected  += HandleNameSelected;
		this.Gui.OnLogoutClick   += HandleLogoutClick;
		this.Inventory.Callback   = HandleInventoryChanged;
		OnInventoryChanged (Inventory);
		OnGoldChanged (Gold);
		OnFameChanged (Fame);
	}

	[Server]
	public override void OnStartServer ()
	{	
		GameObject.Find ("Network Manager").GetComponent<NetworkManagerHUD> ().showGUI = false;
		gameController = GameObject.Find ("GameController").GetComponent<GameController> ();
		StartCoroutine (LoseFame ());
		base.OnStartServer ();
	}

	#endregion

	#region Client event handler wrappers

	// Unity does not support calling Command functions from event handlers :( So these wrappers are required instead.
	// They could have been lambdas, but C# does not allow removing event listeners unless they are given a name. :(
	[Client] void HandleItemUsed (PowerUps  type)
	{
		CmdItemUsed (type);
	}

	[Client] void HandleClassSelected (ClassType type)
	{
		CmdSpawnWithClass (type);
	}

	[Client] void HandleNameSelected (String    name)
	{
		CmdSelectName (name);
	}

	[Client] void HandleLogoutClick ()
	{
		CmdLogout ();
	}

	#endregion

	#region Name and Ship Selection

	// Check to see if the chosen name is availible
	[Command]
	void CmdSelectName (string newName)
	{
		var otherPlayersWithName = FindObjectOfType<GameController> ().allPlayers
			.Where (player => player.playerName.Equals (newName))
			.Count ();

		if (otherPlayersWithName > 0) {
			TargetNameTaken (this.connectionToClient, newName);
		} else {
			this.playerName = newName;
			TargetNameSet (this.connectionToClient, newName);
		}

	}

	// Name taken error message
	[TargetRpc]
	void TargetNameTaken (NetworkConnection connection, string name)
	{
		Gui.ShowNotification ("That name is taken! Please try again.", 5f);
	}

	// Set this client's player name
	[TargetRpc]
	void TargetNameSet (NetworkConnection connection, string name)
	{
		Gui.ShowClassSelection ();
	}

	// Show tutorial overlay
	[TargetRpc]
	void TargetShowTutorial (NetworkConnection connection)
	{
		Gui.ShowTutorialOverlay();
	}

	// Set the new player's name on all clients
	[ClientRpc]
	void RpcSetNameTag ()
	{
		this.playerShip.playerName.text = playerName;
	}
		
	// Spawn ship prefab upon class selection
	[Command]
	void CmdSpawnWithClass (ClassType type)
	{
		GameObject ship = null;
		// Instantiate selected ship prefab
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
		default:
			Debug.Log ("Invalid class type!");
			break;
		}
			
		this.playerShip = ship.GetComponent<Ship> ();
		this.playerShip.transform.position = gameController.FindSpawnPoint ();
		this.playerShip.transform.LookAt (new Vector3 (0, 0, 0));
		this.playerShip.playerObject = this.gameObject;
		this.playerShip.shipType = type;
		this.playerShip.OnKeel += HandleShipKeelServer;
		this.playerShip.OnChestGetServer += HandleChestGetServer;
		NetworkServer.SpawnWithClientAuthority (ship, this.connectionToClient);
		TargetShipSpawned (this.connectionToClient, ship);
		if (!hasSeenTutorial) {
			hasSeenTutorial = true;
			TargetShowTutorial (this.connectionToClient);
		}

		SetStartingInventory (type);
	}

	// Give the players ship the appropriate items for their class at startup
	[Server]
	void SetStartingInventory(ClassType shipType)
	{
		switch (shipType) {
		case ClassType.LargeShip:
			for (int i = 0; i < 3; i++) {
				Inventory.Add (3);
				Inventory.Add (1);
				Inventory.Add (4);
			}
			break;
		case ClassType.MediumShip:
			for (int i = 0; i < 3; i++) {
				Inventory.Add (2);
				Inventory.Add (1);
				Inventory.Add (4);
			}
			break;
		case ClassType.SmallShip:
			for (int i = 0; i < 3; i++) {
				Inventory.Add (4);
				Inventory.Add (1);
			}
			break;
		default:
			Debug.Log ("Invalid class for inventory!");
			break;
		}
	}

	// Setup the local player state when a ship is spawned
	[TargetRpc]
	void TargetShipSpawned (NetworkConnection connection, GameObject ship)
	{
		this.playerShip = ship.GetComponent<Ship> ();
		this.playerShip.OnKeel += HandleShipKeelClient;

		if (this.OnLaunch != null) {
			this.OnLaunch (this.playerShip);
		}
	}

	[Server]
	public void MakeSailsWhite ()
	{
		if (playerShip != null) {
			playerShip.RpcMakeSailsWhite ();
		}
	}

	[Server]
	public void MakeSailsBlack ()
	{
		if (playerShip != null) {
			playerShip.RpcMakeSailsBlack ();
		}
	}

	[Server]
	public void RevertSailColor ()
	{
		if (playerShip != null) {
			playerShip.RpcRevertSailColor ();
		}
	}

	#endregion

	#region Items

	// Called when the player uses an Item
	[Command]
	void CmdItemUsed (PowerUps powerUp)
	{
		int powerUpInt = (int)powerUp;

		// Check to see if it's the spyglass, else go to the ship for item
		if (Inventory.Remove (powerUpInt)) {
			if (powerUp == PowerUps.Spyglass) {
				StartCoroutine (SpyglassRoutine ());
			} else {
				playerShip.UseItem (powerUp);
			}
		}
	}

	// Lose 1 fame every second
	[Server]
	IEnumerator LoseFame ()
	{
		while (true) {
			yield return new WaitForSeconds (1f);
			int currentRank = gameController.allPlayers
				.Where (p => !p.name.Equals (string.Empty))
				.OrderByDescending (p => p.Fame)
				.ToList ()
				.FindIndex (p => p == this)
				+ 1;

			if (this.Rank != currentRank) {
				this.Rank = currentRank;
			}

			if (Fame > 0)
				Fame--;
		}
	}

	#endregion

	#region Chests & Spoils

	// Event run for collecting a chest
	[Server]
	void HandleChestGetServer (Chest chest)
	{
		ServerTakeSpoils (chest.spoils);
	}

	// Add loot to the player when they collide with a chest
	[Client]
	public void TakeSpoils (Spoils spoils)
	{
		CmdTakeSpoils (spoils.Fame, spoils.Gold, Array.ConvertAll (spoils.Powerups, value => (int)value));
	}

	// Collect loot from chests
	// This takes fame, gold, and powerups rather than a spoils struct because
	// the array of an enumerated type in the spoils struct causes problems with
	// Unity's networking api.
	[Command]
	void CmdTakeSpoils (int fame, int gold, int[] powerups)
	{
		ServerTakeSpoils (new Spoils {
			Fame = fame,
			Gold = gold,
			Powerups = powerups
		});
	}

	[Server]
	public void ServerTakeSpoils (Spoils spoils)
	{
		Debug.Log (String.Format ("ServerTakeSpoils - Fame: {0}, Gold: {1}, Powerups: {2}", spoils.Fame, spoils.Gold, spoils.Powerups.ToString ()));
		this.Fame += spoils.Fame;
		this.Gold += spoils.Gold;
		foreach (var item in spoils.Powerups) {
			this.Inventory.Add (item);
		}
	}

	#endregion


	// Reset the player's stats and items
	[Command]
	void CmdLogout ()
	{
		this.playerName = string.Empty;
		this.Gold = 0;
		this.Fame = 0;
		this.Inventory.Clear ();
		hasSeenTutorial = false;

		OnInventoryChanged (Inventory);
		OnGoldChanged (Gold);
		OnFameChanged (Fame);

		GameController gameController = GameObject.Find ("GameController").GetComponent<GameController> ();
		gameController.allPlayers.Remove (this);

		// Check if a playerShip reference exists before attempting to delete the ship
		if (playerShip != null) {
			if (playerShip.gameObject != null) {
				Destroy (playerShip.gameObject);
				playerShip = null;
			}
		}

		if (hasAuthority) {
			GameObject.Find ("Camera").GetComponent<SmoothCamera> ().MainScreen ();
		}
	}

	#region Notifications

	// Server sends out a notification message
	[Server]
	public void SendNotification (string message)
	{
		TargetSendNotification (this.connectionToClient, message, 10f);
	}

	// Send a specific player the Server's notification message
	[TargetRpc]
	void TargetSendNotification (NetworkConnection connection, string message, float seconds)
	{
		
		Gui.ShowNotification (message, seconds);
	}

	#endregion

	#region Spyglass

	// Spyglass powerup duration
	[Server]
	IEnumerator SpyglassRoutine ()
	{
		float goldMultiplier = GOLD_MULTIPLIER;
		yield return new WaitForSeconds (12f);
		goldMultiplier = 1f;
	}

	#endregion

	#region Sinking & Logout

	// Detach event listeners when the player's ship sinks
	[Server]
	void HandleShipKeelServer (Ship ship)
	{
		if (this.OnShipKeel != null)
			this.OnShipKeel (this, this.playerShip);
		// Spawn a chest with some spoils in it
		// todo: Figure out why this doesn't work.
		// Penalize the user for sinking
		int lostFame = (int)(Fame * .2f);
		int lostGold = (int)(Gold * .2f);
		Fame = (int)(Fame * .8f);
		Gold = (int)(Gold * .8f);
		Inventory.Clear ();

		StartCoroutine (DeathChest (ship.gameObject.transform.position, lostFame, lostGold));
		
		OnInventoryChanged (Inventory);
		OnGoldChanged      (Gold);
		OnFameChanged      (Fame);

		// Reset the ship variables
		ship.OnChestGetServer -= HandleChestGetServer;
		ship.OnKeel           -= HandleShipKeelServer;
		this.playerShip        = null;
	}

	#region Death Chest

	[Server]
	IEnumerator DeathChest (Vector3 shipPosition, int lostFame, int lostGold)
	{
		Vector3 chestDropLocation = new Vector3 (shipPosition.x, -2f, shipPosition.z);
		yield return new WaitForSeconds (0.3f);
		var deathChest = GameObject.Instantiate (chestPrefab, chestDropLocation, Quaternion.identity);
		var newChestScript = deathChest.GetComponent<Chest> ();
		newChestScript.spoils = new Spoils () {
			Gold = lostGold,
			Fame = lostFame,
			Powerups = Inventory.Where (_ => UnityEngine.Random.Range (0, 1) == 1).ToArray (),
		};
		NetworkServer.Spawn (deathChest);
		//DeathChestAnimation(deathChest, shipPosition);
	}

	[Server]
	IEnumerator DeathChestAnimation (GameObject deathChest, Vector3 shipPosition)
	{
		Vector3 chestDestination = new Vector3 (shipPosition.x, -2, shipPosition.z);
		Vector3 velocity = Vector3.zero;

		while (deathChest.transform.position.y < -2.1f) {
			Vector3.SmoothDamp (deathChest.transform.position, chestDestination, ref velocity, 2f);
		}
		deathChest.transform.position = chestDestination;
		//deathChest.GetComponent<Chest> ().BeginFloating ();

		yield break;
	}

	#endregion

	[Client]
	void HandleShipKeelClient (Ship ship)
	{
		ship.OnKeel -= HandleShipKeelClient;
		this.playerShip = null;
	}

	// When the player is destroyed or logs out
	void OnDestroy ()
	{
		if (isLocalPlayer) {
			if (this.OnNetworkDisconnect != null) {
				this.OnNetworkDisconnect (this);
			}

			// Under some circumstances that I don't fully understand,
			// Unity calls this OnDestroy method prior to the Gui property
			// being populated. I'm not sure what's up with that. Regardless,
			// we need to ensure that Gui is not null prior to using it.
			if (this.Gui != null) {
				this.Gui.OnItemUsed      -= HandleItemUsed;
				this.Gui.OnClassSelected -= HandleClassSelected;
				this.Gui.OnNameSelected  -= HandleNameSelected;
				this.Gui.OnLogoutClick   -= HandleLogoutClick;

				//this.Inventory.Callback   = HandleInventoryChanged;
			}
		}
		//GameObject.Destroy (playerShip.gameObject);
		//GameObject.Destroy (this);
	}

	#endregion

	#endregion
}