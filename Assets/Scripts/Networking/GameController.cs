using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class GameController : NetworkBehaviour {
	public List<GameObject> eventPrefabs;
	public NautilusNetworkManager networkManager;

	public List<Player> activePlayers;

	public override void OnStartServer() {
		networkManager.OnPlayerJoin += player => activePlayers.Add (player);
		networkManager.OnPlayerLeave += player => activePlayers = activePlayers.Where (p => p != player).ToList ();
		Invoke ("beginEvent", 5.0f);
	}

	void beginEvent() {
		IEvent currentEvent = (IEvent)Instantiate (eventPrefabs [0]).GetComponent (typeof(IEvent));
		currentEvent.BeginEvent (this);
		currentEvent.OnEnd += handleEventEnd;
	}

	void handleEventEnd() {
		Invoke ("beginEvent", 10.0f);
	}
}
