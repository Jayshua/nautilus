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
		cannonShotText       = GameObject.Find ("CannonShotQuantity").GetComponent<Text> ();
		powderKegText        = GameObject.Find ("PowderKegQuantity").GetComponent<Text> ();
		spyglassText         = GameObject.Find ("SpyglassQuantity").GetComponent<Text> ();
		lemonJuiceText       = GameObject.Find ("LemonJuiceQuantity").GetComponent<Text> ();
		windBucketText       = GameObject.Find ("WindBucketQuantity").GetComponent<Text> ();

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

	void HandleNotification(string notification)
	{
		Debug.Log ("Notification: " + notification);
	}

	public void UpdatePowerUps(Player player)
	{
		Debug.Log (player.Inventory.Count);
	}

	public void PlayerConnected(Player player)
	{
		Player playerClass = player;
		player.OnGoldChange   += UpdateGold;
		player.OnFameChange   += UpdateFame;
		player.OnAddPowerups  += UpdatePowerUps;
		player.OnDeath        += HandlePlayerDeath;
		player.OnLogout       += HandlePlayerLogout;
		player.OnNotification += HandleNotification;
	}

	void HandlePlayerLogout(Player player) {
		player.OnGoldChange -= UpdateGold;
		player.OnFameChange -= UpdateFame;
		player.OnDeath      -= HandlePlayerDeath;
		player.OnLogout     -= HandlePlayerLogout;
		player.OnNotification -= HandleNotification;
	}

	void HandlePlayerDeath(Player player) {
		
	}
}
