using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SpawnChest : NetworkBehaviour {

	public GameObject ChestPrefab;

	public Transform[] ChestSpawnLocations;

	bool[] spotUsed;

	// Use this for initialization
	void Start () {

		spotUsed = new bool[ChestSpawnLocations.Length]; // Assign the number of spawn locations to the number of spots used

		if (isServer) { // Check to see if this is the server
			StartCoroutine (SpawnChests ()); // Start coroutine to spawn chests
		}
	}

	IEnumerator SpawnChests() {
		int currentIndex = 0; // Set number of chests index to zero

		while (true) { // Chest coroutine

			if (spotUsed[currentIndex] == false){ //
				GameObject chest = (GameObject)Instantiate (ChestPrefab, ChestSpawnLocations [currentIndex].position, transform.rotation); // Create and place a chest
				Triggered chestScript = chest.GetComponentInChildren<Triggered> (); // When a chest is hit
				int current = currentIndex; // 
				chestScript.OnDestroy += () => spotUsed [current] = false; // When a chest is destroyed
				NetworkServer.Spawn (chest); // Spawn a chest object
				spotUsed[currentIndex] = true; // 
			}

			currentIndex += 1;
			if (currentIndex >= spotUsed.Length) // If the current index is larger than the list of spawn points, then reset
				currentIndex = 0; // reset the spawn location index to zero
			
			yield return new WaitForSeconds (2f); // Wait 2 seconds before calling waking the routine and checking for 
		}

	}
}