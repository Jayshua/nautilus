// Prefab for coin zone
// - Detects when player enters and gives gold/fame
// - Player Enter Event
// - HashSet<Player>

// NautilusServer
// - OnPlayerJoin event

using System;
using System.Collections.Generic;
using UnityEngine;

public class GoldRush : MonoBehaviour, IEvent {
	public GameObject zonePrefab;
	public GameObject chestPrefab;

	List<Zone> zones = new List<Zone> ();
	public event Action OnEnd;

	public void BeginEvent(NautilusNetworkManager server) {
		int zoneCount = (int)Math.Ceiling((float)server.activePlayers.Count / 4.0f);

		var zoneSpawnLocations = new HashSet<GameObject>();
		while (zoneSpawnLocations.Count < zoneCount) {
			var newLocation = this.transform.GetChild(UnityEngine.Random.Range(0, this.transform.childCount)).gameObject;
			zoneSpawnLocations.Add (newLocation);
		}
		Debug.Log (zoneSpawnLocations.Count);

		foreach (var zoneSpawn in zoneSpawnLocations) {
			var newZone = GameObject.Instantiate (zonePrefab, zoneSpawn.transform).GetComponent<Zone>();
			var newChest = GameObject.Instantiate (chestPrefab, zoneSpawn.transform).GetComponent<Chest>();
			newChest.gold = 500;
			this.zones.Add (newZone);
		}

		foreach (var player in server.activePlayers) {
			player.SendNotification ("<size=50>Event: Gold Rush</size>\nBe the first visit as many zones as possible. Trust the compass Luke!");
		}

		//server.OnPlayerJoin += HandlePlayerJoin;

		// Create coin zones
		// Create the chests
		// Notify players
		// Listen for new players
		// Listen for lost players
	}

	// Ongoing:
	// - Update compass markers
	// - End when all zones are found

	// Cleanup:
	// - Remove coin zones
	// - Remove compass markers
	// - Remove player messages
	// - Remove event listeners on the server
}