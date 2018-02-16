using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
	Text NameSelectionMessage;
	Text NameText;
	RectTransform healthBar;
	Text goldText;
	Text fameText;

	private Action<ClassType> handleClassSelected;
	private Action<string> handleNameSelected;

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
		ClassSelectionPanel.SetActive(false);
		GuiPanel.SetActive(false);
		NameSelectionPanel.SetActive(false);
	}

	void Update()
	{

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

	public void PlayerConnected(Player player)
	{
		Player playerClass = player;
		player.OnGoldChange   += UpdateGold;
		player.OnFameChange   += UpdateFame;
		player.OnDeath        += HandlePlayerDeath;
		player.OnLogout       += HandlePlayerLogout;
	}

	void HandlePlayerLogout(Player player) {
		player.OnGoldChange -= UpdateGold;
	}

	void HandlePlayerDeath(Player player) {
		
	}

	void HandleHealthChange(Player player) {

	}

}
