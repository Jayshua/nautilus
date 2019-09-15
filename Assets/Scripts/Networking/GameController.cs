using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class GameController : NetworkBehaviour
{
	public List<GameObject> eventPrefabs;
	public NautilusNetworkManager networkManager;

	public List<Player> allPlayers;

	public event Action<Player> OnPlayerJoin;

	int nextEvent = 0;

	#region Spawn map variables

	const int MAP_WIDTH = 2637;
	const int MAP_HEIGHT = 2646;
	const int MAP_X_OFFSET = -1374;
	const int MAP_Z_OFFSET = -1356;

	[SerializeField] Texture2D spawnMap;
	[SerializeField] LayerMask spawnMask;

	#endregion

	public override void OnStartServer ()
	{
		networkManager.OnClientConnected += player => {
			allPlayers.Add (player);
			if (OnPlayerJoin != null) {
				OnPlayerJoin (player);
			}
		};

		networkManager.OnClientDisconnected += player =>
			allPlayers = allPlayers.Where (p => p != player).ToList ();

		Invoke ("beginEvent", 120.0f);
	}

	void beginEvent ()
	{
		nextEvent = (1 + nextEvent) % eventPrefabs.Count;
		IEvent currentEvent = (IEvent)Instantiate (eventPrefabs [nextEvent]).GetComponent (typeof(IEvent));
		currentEvent.BeginEvent (this);
		currentEvent.OnEnd += handleEventEnd;
	}

	void handleEventEnd ()
	{
		Invoke ("nextEventMessage", 15.0f);
		Invoke ("beginEvent", 120.0f);
	}

	void nextEventMessage()
	{
		foreach (var player in allPlayers) {
			player.SendNotification ("The next event will begin in two minutes!");
		}
	}

	[Server]
	public Vector3 FindSpawnPoint ()
	{
		Color spawnPointColor;
		int xCoordinate;
		int zCoordinate;
		Vector3 spawnPoint = new Vector3 (0f, 2.5f, 0f);

		do {
			do {
				xCoordinate = UnityEngine.Random.Range (0, spawnMap.width);
				zCoordinate = UnityEngine.Random.Range (0, spawnMap.height);
				spawnPointColor = spawnMap.GetPixel (xCoordinate, zCoordinate);
			} while(spawnPointColor != Color.white);

			spawnPoint.x = (float)xCoordinate / spawnMap.width * MAP_WIDTH + MAP_X_OFFSET;
			spawnPoint.z = (float)zCoordinate / spawnMap.height * MAP_HEIGHT + MAP_Z_OFFSET;
		} while(!Physics.CheckSphere (spawnPoint, 20f, spawnMask));

		return spawnPoint;
	}
}
