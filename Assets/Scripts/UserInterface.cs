using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

	public event Action<PowerUps> ItemUsed;
	public event Action<ClassType> OnClassSelected;
	public event Action<String> OnNameSelected;

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
		
	public void ShowClassSelection() {
		ClassSelectionPanel.SetActive(true);
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
		
	public void ShowGUI() {
		NameSelectionPanel.SetActive (false);
		ClassSelectionPanel.SetActive (false);
		GuiPanel.SetActive(true);
	}

	public void SelectClass(string type) {
		ShowGUI ();
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

	public void UpdateGold (Player player)
	{
		goldText.text = player.Gold.ToString();
	}

	public void UpdateFame (Player player)
	{
		fameText.text = player.Fame.ToString ();
	}



	void HandleNotification(string notification)
	{
		notificationText.text = notification;
		Invoke ("RemoveNotification", 10);
	}

	void RemoveNotification() {
		notificationText.text = "";
	}

	public void UpdatePowerUps(List <PowerUps> powerUps)
	{
		itemInventory[PowerUps.CannonShot].text = powerUps.Where(p => p == PowerUps.CannonShot).Count().ToString();
		itemInventory[PowerUps.PowderKeg ].text = powerUps.Where(p => p == PowerUps.PowderKeg ).Count().ToString();
		itemInventory[PowerUps.Spyglass  ].text = powerUps.Where(p => p == PowerUps.Spyglass  ).Count().ToString();
		itemInventory[PowerUps.LemonJuice].text = powerUps.Where(p => p == PowerUps.LemonJuice).Count().ToString();
		itemInventory[PowerUps.WindBucket].text = powerUps.Where(p => p == PowerUps.WindBucket).Count().ToString();
	}

	public void PlayerConnected(Player player)
	{
		Player playerClass = player;
		player.OnGoldChange     += UpdateGold;
		player.OnFameChange     += UpdateFame;
		player.OnChangePowerups += UpdatePowerUps;
		player.OnLogout         += HandlePlayerLogout;
		player.OnNotification   += HandleNotification;
		player.OnKeel           += HandlePlayerKeel;
		compass.PlayerConnected (player);
	}

	void HandlePlayerLogout(Player player) {
		player.OnGoldChange -= UpdateGold;
		player.OnFameChange -= UpdateFame;
		player.OnLogout     -= HandlePlayerLogout;
		player.OnNotification -= HandleNotification;
	}

	void HandlePlayerKeel(GameObject ship) {
		GuiPanel.SetActive (false);
		ClassSelectionPanel.SetActive (true);
	}

	public void SelectPowerUp(string type) {
		if (ItemUsed != null) {
			switch (type) {
			case "Spyglass":
				ItemUsed (PowerUps.Spyglass);
				break;
			case "PowderKeg":
				ItemUsed (PowerUps.PowderKeg);
				break;
			case "CannonShot":
				ItemUsed (PowerUps.CannonShot);
				break;
			case "LemonJuice":
				ItemUsed (PowerUps.LemonJuice);
				break;
			case "WindBucket":
				ItemUsed (PowerUps.WindBucket);
				break;
			default:
				Debug.Log ("Unknown powerup type in item selection GUI: " + type);
				break;
			}
		}
	}
}
