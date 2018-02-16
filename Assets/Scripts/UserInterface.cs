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
	Text NameSelectionMessage;
	Text NameText;
	Text goldText;
	Text fameText;
	Text cannonShotText;
	Text powderKegText;
	Text spyglassText;
	Text lemonJuiceText;
	Text windBucketText;

	Dictionary<PowerUps, Text> itemInventory = new Dictionary<PowerUps, Text>() { }; 

	private Action<ClassType> handleClassSelected;
	private Action<string>    handleNameSelected;

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

		itemInventory.Add(PowerUps.CannonShot, GameObject.Find ("CannonShotQuantity").GetComponent<Text> ());
		itemInventory.Add(PowerUps.PowderKeg,  GameObject.Find ("PowderKegQuantity" ).GetComponent<Text> ());
		itemInventory.Add(PowerUps.Spyglass,   GameObject.Find ("SpyglassQuantity"  ).GetComponent<Text> ());
		itemInventory.Add(PowerUps.LemonJuice, GameObject.Find ("LemonJuiceQuantity").GetComponent<Text> ());
		itemInventory.Add(PowerUps.WindBucket, GameObject.Find ("WindBucketQuantity").GetComponent<Text> ());

		ClassSelectionPanel.SetActive(false);
		GuiPanel.SetActive(false);
		NameSelectionPanel.SetActive(false);
	}
		
	public void ShowClassSelection(Action<ClassType> callback) {
		handleClassSelected = callback;
		ClassSelectionPanel.SetActive(true);
		NameSelectionPanel.SetActive(false);
	}

	public void ShowNameSelection(Action<string> callback, bool isNameTaken) {
		handleNameSelected = callback;
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
		if (handleClassSelected != null) {
			switch (type) {
			case "Black Pearl":
				handleClassSelected (ClassType.SmallShip);
				break;
			case "Captian Fortune":
				handleClassSelected (ClassType.MediumShip);
				break;
			case "Royal Dutchman":
				handleClassSelected (ClassType.LargeShip);
				break;
			default:
				Debug.Log ("Unknown class type in class selection GUI: " + type);
				break;
			}

			handleClassSelected = null;
		}
	}

	public void Submit() {
		if (handleNameSelected != null) {
			handleNameSelected (NameText.text);
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
		player.OnDeath          += HandlePlayerDeath;
		player.OnLogout         += HandlePlayerLogout;
	}

	void HandlePlayerLogout(Player player) {
		player.OnGoldChange -= UpdateGold;
		player.OnFameChange -= UpdateFame;
		player.OnDeath      -= HandlePlayerDeath;
		player.OnLogout     -= HandlePlayerLogout;
	}

	void HandlePlayerDeath(Player player) {
		
	}
}
