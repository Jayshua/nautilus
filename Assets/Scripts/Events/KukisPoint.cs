using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class KukisPoint : MonoBehaviour, IEvent
{
	GameController gameController;
	List<Player> marauders = new List<Player> ();
	List<Player> royalNavy = new List<Player> ();
	int maraudersLeft;

	public event Action OnEnd;

	float towerHealth = 2000f;
	float originalTowerHealth;
	bool halfwayHealthMessage = false;
	[SerializeField] GameObject towerPrefab;

	public void BeginEvent (GameController gameController)
	{
		this.gameController = gameController;

		originalTowerHealth = towerHealth;
		var tower = GameObject.Instantiate (towerPrefab, towerPrefab.transform.position, Quaternion.identity, this.transform);
		NetworkServer.Spawn (tower);

		// Sort players by fame
		IEnumerable<Player> sortedPlayers = gameController.allPlayers.OrderBy (p => p.Fame);

		// Put every odd player in the Marauders
		foreach (var player in sortedPlayers.Where((element, index) => index % 2 == 0)) {
			player.OnShipKeel += HandleShipKeel;
			marauders.Add (player);
			maraudersLeft++;
		}
		// Put every even player in the His Majesty's Navy
		foreach (var player in sortedPlayers.Skip(1).Where((element, index) => index % 2 == 0)) {
			player.OnShipKeel += HandleShipKeel;
			royalNavy.Add (player);
		}

		// Notify teams of event & set team colors
		foreach (var player in marauders) {
			player.SendNotification (UserInterface.BuildHeadingNotification("Event: Attack At Kuki's Point", "Reward: 400 Gold!\nYou have black sails and are a marauder. Attack and destroy the enemy tower! Watch out for the defenders with white sails! Your compass can guide you to the tower."));
			player.MakeSailsBlack ();
		}
		foreach (var player in royalNavy) {
			player.SendNotification (UserInterface.BuildHeadingNotification("Event: Attack At Kuki's Point", "Reward: 400 Gold!\nYou have white sails and are in His Majesty's Navy. Defend your tower from the marauders with black sails! Your compass can guide you to the tower."));
			player.MakeSailsWhite ();
		}

		StartCoroutine (CheckIfDone ());
	}

	IEnumerator CheckIfDone ()
	{
		while (true) {

			if (towerHealth == 0 || maraudersLeft == 0) {
				List<Player> winningTeam;
				List<Player> losingTeam;

				if (this.OnEnd != null) {
					this.OnEnd ();
				}

				// Reward the event winners
				if (towerHealth == 0) {
					winningTeam = marauders;
					losingTeam = royalNavy;
				} else {
					winningTeam = royalNavy;
					losingTeam = marauders;
				}

				// Notify everyone of who won & revert sail colors
				foreach (Player player in gameController.allPlayers) {
					if (winningTeam == marauders) {
						player.SendNotification (UserInterface.BuildHeadingNotification("The Marauders Have Won!", "\n400 Gold and Fame awarded to every member. Maybe the navy will have better luck next time."));
						// Reward marauders & remove royalNavy's fame
					} else {
						player.SendNotification (UserInterface.BuildHeadingNotification("His Majesty's Navy Won!", "\n400 Gold and Fame awarded to every member. Maybe the Marauders will have better luck next time."));
					}
					player.OnShipKeel -= HandleShipKeel;
					player.RevertSailColor ();
				}

				// Reward teams and revert sail colors
				foreach (Player player in winningTeam) {
					player.Gold += 400;
					player.Fame += 400;
				}

				foreach (Player player in losingTeam) {
					player.Fame -= 200;
				}

				Destroy (this.gameObject);
			}
			yield return new WaitForSeconds (1);
		}
	}

	void HandleShipKeel (Player player, Ship ship)
	{
		if (marauders.Contains (player))
			maraudersLeft--;
	}

	public void TakeDamage (float damage)
	{
		towerHealth -= damage;
		if (towerHealth < 0f) {
			towerHealth = 0f;
		}
			
		// Notify users of the health status of the tower
		if (towerHealth <= originalTowerHealth / 2f && !halfwayHealthMessage) {
			foreach (var player in marauders.Concat(royalNavy)) {
				player.SendNotification ("The tower is halfway destroyed!");
			}
			halfwayHealthMessage = true;
		}
	}
}