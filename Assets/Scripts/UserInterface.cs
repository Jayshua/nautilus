using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;

public enum ClassType {
	LargeShip,
	MediumShip,
	SmallShip
}
	
public class UserInterface : MonoBehaviour {

	const int HEALTHBARWIDTH = 1145;

	GameObject ClassSelectionPanel;
	GameObject NameSelectionPanel;
	GameObject GuiPanel;
	RectTransform healthBar;
	Compass compass;
	Text NameSelectionMessage;
	Text NameText;
	Text goldText;
	Text fameText;
	Text notificationText;

	Dictionary<PowerUps, Text> itemInventory = new Dictionary<PowerUps, Text>() { }; 

	public event Action<PowerUps> OnItemUsed;
	public event Action<ClassType> OnClassSelected;
	public event Action<string> OnNameSelected;

	void Start()
	{
		ClassSelectionPanel  = GameObject.Find ("ClassSelection");
		GuiPanel             = GameObject.Find ("GuiPanel");
		NameSelectionPanel   = GameObject.Find ("UserNameSelection");
		NameSelectionMessage = GameObject.Find ("UserNameSelection/Message").GetComponent<Text>();
		NameText             = GameObject.Find ("UserNameSelection/Name/NameText").GetComponent<Text>();
		healthBar            = GameObject.Find ("HealthBar").GetComponent<RectTransform>();
		goldText 			 = GameObject.Find ("GoldText").GetComponent<Text> ();
		fameText 			 = GameObject.Find ("FameText").GetComponent<Text> ();
		notificationText     = GameObject.Find ("GuiPanel/Notification").GetComponent<Text> ();
		compass              = GameObject.Find ("GuiPanel/Compass").GetComponent<Compass> ();

		itemInventory.Add(PowerUps.CannonShot, GameObject.Find ("CannonShotQuantity").GetComponent<Text> ());
		itemInventory.Add(PowerUps.PowderKeg,  GameObject.Find ("PowderKegQuantity" ).GetComponent<Text> ());
		itemInventory.Add(PowerUps.Spyglass,   GameObject.Find ("SpyglassQuantity"  ).GetComponent<Text> ());
		itemInventory.Add(PowerUps.LemonJuice, GameObject.Find ("LemonJuiceQuantity").GetComponent<Text> ());
		itemInventory.Add(PowerUps.WindBucket, GameObject.Find ("WindBucketQuantity").GetComponent<Text> ());

		ClassSelectionPanel.SetActive(false);
		GuiPanel.SetActive(false);
		NameSelectionPanel.SetActive(false);
	}

	public void ShowNameSelection(bool isNameTaken) {
		NameSelectionPanel.SetActive(true);

		if (isNameTaken) {
			NameSelectionMessage.text = "Name was taken. Please try again.";
		} else {
			NameSelectionMessage.text = "";
		}
	}
		
	public void SelectClass(string type) {
		ShowPanel (GuiPanel);
		if (OnClassSelected != null) {
			switch (type) {
			case "Black Pearl":
				OnClassSelected (ClassType.SmallShip);
				break;
			case "Captian Fortune":
				OnClassSelected (ClassType.MediumShip);
				break;
			case "Royal Dutchman":
				OnClassSelected (ClassType.LargeShip);
				break;
			default:
				Debug.Log ("Unknown class type in class selection GUI: " + type);
				break;
			}
		}
	}

	public void Submit() {
		if (OnNameSelected != null) {
			OnNameSelected (NameText.text);
		}
	}

	public void UpdateHealth (float health)
	{
		healthBar.sizeDelta = new Vector2 (health * HEALTHBARWIDTH, healthBar.sizeDelta.y);
	}


	void HandleNotification(string notification)
	{
		notificationText.text = notification;
		Invoke ("RemoveNotification", 10);
	}

	void RemoveNotification() {
		notificationText.text = "";
	}

	public void PlayerConnected(Player player)
	{
		ShowPanel (ClassSelectionPanel);
		player.OnStatsChange += HandleStatsChange;
		player.OnLogout         += HandlePlayerLogout;
		player.OnNotification   += HandleNotification;
		player.OnLaunch         += HandlePlayerLaunch;
		compass.PlayerConnected (player);
	}

	void HandleStatsChange(int fame, int gold, SyncListInt powerUps) {
		fameText.text = fame.ToString ();
		goldText.text = gold.ToString ();

		foreach (var key in itemInventory) {
			itemInventory[key.Key].text = "0";
		}

		foreach (int item in powerUps) {
			itemInventory [(PowerUps)item].text = (Convert.ToInt32 (itemInventory [(PowerUps)item].text) + 1).ToString();
		}
	}

	void HandlePlayerLogout(Player player) {
		player.OnLogout         -= HandlePlayerLogout;
		player.OnNotification   -= HandleNotification;
		player.OnLaunch         -= HandlePlayerLaunch;
	}

	void HandlePlayerLaunch(Ship ship) {
		ShowPanel (GuiPanel);
		ship.OnKeel += HandleShipKeel;
	}

	void HandleShipKeel(Ship ship) {
		ship.OnKeel -= HandleShipKeel;
		ShowPanel (ClassSelectionPanel);
	}

	void ShowPanel(GameObject panel) {
		GuiPanel.SetActive (false);
		ClassSelectionPanel.SetActive (false);
		NameSelectionPanel.SetActive (false);
		panel.SetActive (true);
	}

	public void SelectPowerUp(string type) {
		if (OnItemUsed != null) {
			switch (type) {
			case "Spyglass":
				OnItemUsed (PowerUps.Spyglass);
				break;
			case "PowderKeg":
				OnItemUsed (PowerUps.PowderKeg);
				break;
			case "CannonShot":
				OnItemUsed (PowerUps.CannonShot);
				break;
			case "LemonJuice":
				OnItemUsed (PowerUps.LemonJuice);
				break;
			case "WindBucket":
				OnItemUsed (PowerUps.WindBucket);
				break;
			default:
				Debug.Log ("Unknown powerup type in item selection GUI: " + type);
				break;
			}
		}
	}
}
