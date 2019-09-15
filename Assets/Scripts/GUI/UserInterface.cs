#region Using
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;
using System.Text.RegularExpressions;
#endregion

#region Enums
public enum GuiScreen
{
	NameSelection,
	ClassSelection,
	Logout,
	SEIinfo,
	InGame
}

public enum ClassType 
{
	LargeShip,
	MediumShip,
	SmallShip
}

public enum Upgrade 
{
	Health,
	Damage,
	Speed
}

static class Upgrades {
	public static int UpgradeCost(int currentLevel, Upgrade upgrade, ClassType shipClass) {
		int[] costs = { 300, 400, 600, 800, 1100, 400, 600, 800, 1100, 1500 };

		if (currentLevel > 4)
			return -1;

		if (
			shipClass == ClassType.SmallShip && upgrade == Upgrade.Speed
			|| shipClass == ClassType.MediumShip && upgrade == Upgrade.Damage
			|| shipClass == ClassType.LargeShip && upgrade == Upgrade.Health
		) {
			return costs [currentLevel];
		} else {
			return costs [currentLevel + 5];
		}
	}
}
#endregion	

public class UserInterface : MonoBehaviour 
{
	#region Constants - Items
	const float CANNON_SHOT_COOLDOWN = 10f;
	const float POWDER_KEG_COOLDOWN  = 2f;
	const float SPYGLASS_COOLDOWN 	 = 12f;
	const float LEMON_JUICE_COOLDOWN = 1f;
	const float BAG_OF_WIND_COOLDOWN = 3f;
	const int   RANDOM_ITEM_COST     = 600;
	#endregion
	
	#region GameObjects (Transforms, Dropdowns, Text, Images, etc.)
	[SerializeField] GameObject    ClassSelectionPanel;
	[SerializeField] GameObject    NameSelectionPanel;
	[SerializeField] GameObject    GuiPanel;
	[SerializeField] GameObject    TutorialOverlayPanel;
	[SerializeField] GameObject    PauseMenuPanel;
	[SerializeField] GameObject    OtherProjsPanel;
	[SerializeField] RectTransform healthBar;
	[SerializeField] Compass       compass;
	[SerializeField] Dropdown      listOfNameTitle;
	[SerializeField] Dropdown      listOfNameAdjective;
	[SerializeField] Dropdown      listofNameNoun;
	[SerializeField] Text          NameSelectionMessage;
	[SerializeField] Text          goldText;
	[SerializeField] Text          fameText;
	[SerializeField] Notification  notification;
	[SerializeField] Text          healthUpgradeText;
	[SerializeField] Text          damageUpgradeText;
	[SerializeField] Text          speedUpgradeText;
	[SerializeField] Text          healthUpgradeCost;
	[SerializeField] Text          speedUpgradeCost;
	[SerializeField] Text          damageUpgradeCost;
	[SerializeField] Image 		   leftCooldownImage;
	[SerializeField] Image		   rightCooldownImage;
	[SerializeField] Button        spyglassButton;
	[SerializeField] Image         spyglassCooldown;
	[SerializeField] Button        powderKegButton;
	[SerializeField] Image         powderKegCooldown;
	[SerializeField] Button        cannonShotButton;
	[SerializeField] Image         cannonShotCooldown;
	[SerializeField] Button        lemonJuiceButton;
	[SerializeField] Image         lemonJuiceCooldown;
	[SerializeField] Button        bagOfWindButton;
	[SerializeField] Image         bagOfWindCooldown;
	[SerializeField] Text          rankText;

	#endregion

	#region SoundFX
	[Header ("SoundFX")]
	[SerializeField]
	AudioSource GUIAudioSource;
	[SerializeField]
	AudioClip   notificationSFX;

	#endregion

	#region Fields
	public GuiScreen CurrentScreen;
	float healthBarWidth;
	float cooldown;
	string playerName;
	int oldGold;
	int newGold;
	int oldFame;
	int newFame;
	#endregion

	#region Dictionary - PowerUps
	Dictionary<PowerUps, Text> itemInventory = new Dictionary<PowerUps, Text>() { };
	#endregion

	#region Events
	public event Action<PowerUps>  OnItemUsed;
	public event Action<Upgrade>   OnUpgrade;
	public event Action<ClassType> OnClassSelected;
	public event Action<string>    OnNameSelected;
	public event Action            OnLogoutClick;
	public event Action<Emoticon>  OnEmoticon;
	#endregion

	#region Standard Functions (Start, LateUpdate)
	void Start()
	{
		healthBarWidth = healthBar.rect.width;

		itemInventory.Add(PowerUps.CannonShot, GameObject.Find ("CannonShotQuantity").GetComponent<Text> ());
		itemInventory.Add(PowerUps.PowderKeg,  GameObject.Find ("PowderKegQuantity" ).GetComponent<Text> ());
		itemInventory.Add(PowerUps.Spyglass,   GameObject.Find ("SpyglassQuantity"  ).GetComponent<Text> ());
		itemInventory.Add(PowerUps.LemonJuice, GameObject.Find ("LemonJuiceQuantity").GetComponent<Text> ());
		itemInventory.Add(PowerUps.WindBucket, GameObject.Find ("WindBucketQuantity").GetComponent<Text> ());

		ClassSelectionPanel.SetActive(false);
		GuiPanel.SetActive(false);
		NameSelectionPanel.SetActive(false);
		PauseMenuPanel.SetActive (false);
		OtherProjsPanel.SetActive (false);

		oldGold = int.Parse (Regex.Replace(goldText.text, @"[^\d]", ""));
		oldFame = int.Parse (Regex.Replace(fameText.text, @"[^\d]", ""));
		newGold = oldGold;
		newFame = oldFame;
	}

	void LateUpdate()
	{
		// PauseMenuPanel will only enabled if player is actually playing
		if (NameSelectionPanel.activeSelf == false)
		{
			if (Input.GetKeyDown (KeyCode.Escape)) 
			{
				// Setup ESC button for pause menu panel
				if (PauseMenuPanel.activeSelf == true) {
					PauseMenuPanel.SetActive (false);
				} 
				else {
					PauseMenuPanel.SetActive (true);
				}
				// Setup ESC button for projects info panel
				if (OtherProjsPanel.activeSelf == true) {
					BackToMenu ();
				}
			}
		}

		if (Input.GetKeyDown (KeyCode.Alpha1))
			SelectPowerUp ("CannonShot");

		if (Input.GetKeyDown (KeyCode.Alpha2))
			SelectPowerUp ("PowderKeg");

		if (Input.GetKeyDown (KeyCode.Alpha3))
			SelectPowerUp ("Spyglass");

		if (Input.GetKeyDown (KeyCode.Alpha4))
			SelectPowerUp ("LemonJuice");

		if (Input.GetKeyDown (KeyCode.Alpha5))
			SelectPowerUp ("WindBucket");

		// Gradually increase stats to actual values
		if (oldGold != newGold) {
			oldGold = (int)Mathf.Lerp (oldGold, newGold, .2f);
			if (oldGold > newGold - 1 && oldGold < newGold + 1)
				oldGold = newGold;
			goldText.text = string.Format("{0:N0}", oldGold);
		}
		if (oldFame != newFame) {
			oldFame = (int)Mathf.Lerp (oldFame, newFame, .2f);
			if (oldFame > (newFame - 1) && newFame < (newFame + 1))
				oldFame = newFame;
			fameText.text = string.Format("{0:N0}", oldFame);
		}

		// Animate the cooldown indicators
		if (leftCooldownImage.fillAmount > 0f && leftCooldownImage.fillAmount < .999f)
			leftCooldownImage.fillAmount += 1.0f / cooldown * Time.deltaTime;
		else
			leftCooldownImage.fillAmount = 0f;
		if (rightCooldownImage.fillAmount > 0f && rightCooldownImage.fillAmount < .999f)
			rightCooldownImage.fillAmount += 1.0f / cooldown * Time.deltaTime;
		else
			rightCooldownImage.fillAmount = 0f;
	}
	#endregion

	#region Pause Menu
	// Resume button
	public void ResumeGame() {
		PauseMenuPanel.SetActive (false);
	}

	// View SEI projects' information
	public void OtherProjs() {
		ShowPanel (GuiScreen.SEIinfo);
	}
	public void BackToMenu() 
	{
		// Player is playing and is alive
		if (GameObject.FindGameObjectWithTag ("Player") &&
			GameObject.FindGameObjectWithTag ("Player").GetComponent<Ship> ().OnKeel != null)
		{
			PauseMenuPanel.SetActive (true);
			GuiPanel.SetActive (true);
			OtherProjsPanel.SetActive (false);
		}
		// Player is not playing
		else 
		{
			PauseMenuPanel.SetActive (true);
			ClassSelectionPanel.SetActive (true);
			OtherProjsPanel.SetActive (false);
		}
	}

	// Player log out of gameplay
	public void LogOut() {
		OnLogoutClick ();
		ShowPanel (GuiScreen.NameSelection);
	}

	// Player quits the game
	void HandlePlayerLogout(Player player) 
	{
		player.OnFameChanged       -= HandleFameChanged;
		player.OnGoldChanged       -= HandleGoldChanged;
		player.OnInventoryChanged  -= HandleInventoryChanged;
		player.OnNetworkDisconnect -= HandlePlayerLogout;
		player.OnLaunch            -= HandlePlayerLaunch;
	}
	#endregion

	#region Main Menu
	// Show the Name Selection Menu
	public void ShowNameSelection(bool isNameTaken) 
	{
		NameSelectionPanel.SetActive(true);

		if (isNameTaken)
		{
			NameSelectionMessage.text = "Name was taken. Please try again.";
		} 
		else 
		{
			NameSelectionMessage.text = "";
		}
	}

	// User select a ship class in Ship Class Menu
	public void SelectClass(string type)
	{
		ShowPanel (GuiScreen.InGame);
		if (OnClassSelected != null)
		{
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

	// User chooses the name
	public void Submit() 
	{
		int    titleIndex;
		int    adjectiveIndex;
		int    nounIndex;
		string nameTitle;
		string nameAdjective;
		string nameNoun;

		// Get the selected item's index on the dropdown list
		titleIndex     = listOfNameTitle.value;
		adjectiveIndex = listOfNameAdjective.value;
		nounIndex      = listofNameNoun.value;
		// Get the player's name
		nameTitle     = listOfNameTitle.options [titleIndex].text;
		nameAdjective = listOfNameAdjective.options [adjectiveIndex].text;
		nameNoun      = listofNameNoun.options [nounIndex].text;
		playerName    = nameTitle + " " + nameAdjective + " " + nameNoun;

		if (OnNameSelected != null) 
		{
			OnNameSelected (playerName);
		}
	}
	#endregion

	#region Notifications
	// Show event notification
	public void ShowNotification(string message, float seconds)
	{
		GUIAudioSource.clip = notificationSFX;
		GUIAudioSource.Play();
		notification.ShowNotification (message, seconds);
	}
	#endregion

	// Show the class selection pannel
	public void ShowClassSelection()
	{
		ShowPanel (GuiScreen.ClassSelection);
	}

	// Player connected to the game
	public void PlayerConnected(Player player)
	{
		ShowPanel (GuiScreen.NameSelection);
		player.OnFameChanged += HandleFameChanged;
		player.OnGoldChanged += HandleGoldChanged;
		player.OnRankChange += HandlePlayerRankChange;
		player.OnInventoryChanged += HandleInventoryChanged;
		player.OnNetworkDisconnect += HandlePlayerLogout;
		player.OnLaunch += HandlePlayerLaunch;
		compass.PlayerConnected (player);
	}

	// Show initial tutorial overlay
	public void ShowTutorialOverlay()
	{
		TutorialOverlayPanel.SetActive (true);
		StartCoroutine (HideTutorialOverlay ());
	}

	// Hide initial tutorial overlay
	IEnumerator HideTutorialOverlay()
	{
		yield return new WaitForSeconds (8f);
		float fadeOutTime = 2f;
		Image[] icons = TutorialOverlayPanel.GetComponentsInChildren<Image>();
		Text[] textBoxes = TutorialOverlayPanel.GetComponentsInChildren<Text>();
		Color[] iconColors = new Color[icons.Length];
		Color[] textColors = new Color[textBoxes.Length];
		for(int i = 0;i < icons.Length;i++)
			iconColors[i] = icons[i].color;
		for(int i = 0;i < textBoxes.Length;i++)
			textColors[i] = textBoxes[i].color;
		for(float time = 0.01f;time < fadeOutTime;time += Time.deltaTime)
		{
			for(int i = 0;i< icons.Length;i++)
				icons[i].color = Color.Lerp(iconColors[i], new Color(1,1,1,0), Mathf.Min(1, time/fadeOutTime));
			for(int i = 0;i< textBoxes.Length;i++)
				textBoxes[i].color = Color.Lerp(textColors[i], new Color(1,1,1,0), Mathf.Min(1, time/fadeOutTime));

			yield return new WaitForFixedUpdate();
		}
		TutorialOverlayPanel.SetActive (false);
	}

	#region Stats
	// Fame increases or decreases
	void HandleFameChanged(int fame) 
	{
		newFame = fame;
	}

	// Gold increases or decreases
	void HandleGoldChanged(int gold) 
	{
		newGold = gold;
	}

	// Update the visual health bar
	public void UpdateHealth (float health)
	{
		healthBar.sizeDelta = new Vector2 (health * healthBarWidth, healthBar.sizeDelta.y);
	}

	public void HandlePlayerRankChange(int newRank)
	{
		this.rankText.text = UserInterface.NumberOrdinality(newRank);
	}
	#endregion

	#region PowerUps
	// Player gets new item(s) or used item(s)
	void HandleInventoryChanged(SyncListInt powerUps) 
	{
		foreach (var key in itemInventory) 
		{
			itemInventory[key.Key].text = "0";
		}

		foreach (int item in powerUps) 
		{
			itemInventory [(PowerUps)item].text = (Convert.ToInt32 (itemInventory [(PowerUps)item].text) + 1).ToString();
		}
	}

	// Player uses powerup item(s)
	public void SelectPowerUp(string type) 
	{
		if (OnItemUsed != null) 
		{
			switch (type) 
			{
			case "Spyglass":
				if (!QuantityIsZero(spyglassButton)) {
					OnItemUsed (PowerUps.Spyglass);
					StartCoroutine (ItemCooldownEffect (spyglassButton, spyglassCooldown, SPYGLASS_COOLDOWN));
				}
				break;
			case "PowderKeg":
				if (!QuantityIsZero (powderKegButton)) {
					OnItemUsed (PowerUps.PowderKeg);
					StartCoroutine (ItemCooldownEffect (powderKegButton, powderKegCooldown, POWDER_KEG_COOLDOWN));
				}
				break;
			case "CannonShot":
				if (!QuantityIsZero (cannonShotButton)) {
					OnItemUsed (PowerUps.CannonShot);
					StartCoroutine (ItemCooldownEffect (cannonShotButton, cannonShotCooldown, CANNON_SHOT_COOLDOWN));
				}
				break;
			case "LemonJuice":
				if (!QuantityIsZero (lemonJuiceButton)) {
					OnItemUsed (PowerUps.LemonJuice);
					StartCoroutine (ItemCooldownEffect (lemonJuiceButton, lemonJuiceCooldown, LEMON_JUICE_COOLDOWN));
				}
				break;
			case "WindBucket":
				if (!QuantityIsZero (bagOfWindButton)) {
					OnItemUsed (PowerUps.WindBucket);
					StartCoroutine (ItemCooldownEffect (bagOfWindButton, bagOfWindCooldown, BAG_OF_WIND_COOLDOWN));
				}
				break;
			default:
				Debug.Log ("Unknown powerup type in item selection GUI: " + type);
				break;
			}
		}
	}

	private bool QuantityIsZero(Button itemButton)
	{
		return itemButton.GetComponentInChildren<Text> ().text.Equals ("0");
	}

	IEnumerator ItemCooldownEffect(Button itemButton, Image cooldownImage, float cooldownTime)
	{
		itemButton.interactable  = false;
		cooldownImage.fillAmount = 0f;

		while (cooldownImage.fillAmount < 1f) {
			cooldownImage.fillAmount += 1.0f / cooldownTime * Time.deltaTime;
			yield return new WaitForFixedUpdate();
		}

		itemButton.interactable       = true;
		cooldownImage.fillAmount = 0f;
		yield break;
	}

	#endregion

	#region Sinking
	// Handle ????
	void HandlePlayerLaunch(Ship ship) 
	{
		ShowPanel (GuiScreen.InGame);
		ship.OnKeel += HandleShipKeel;
		ship.OnUpgradeAuthority += HandleShipUpgrade;

		HandleShipUpgrade (ship, Upgrade.Damage, ship.damageUpgrades);
		HandleShipUpgrade (ship, Upgrade.Speed, ship.speedUpgrades);
		HandleShipUpgrade (ship, Upgrade.Health, ship.healthUpgrades);
	}

	// Handle ship got sunk
	void HandleShipKeel(Ship ship) 
	{
		ship.OnKeel -= HandleShipKeel;
		ship.OnUpgradeAuthority -= HandleShipUpgrade;
		ShowPanel (GuiScreen.ClassSelection);
	}
	#endregion

	// Show the UI panel
	void ShowPanel(GuiScreen panel)
	{
		GuiPanel.SetActive (false);
		ClassSelectionPanel.SetActive (false);
		NameSelectionPanel.SetActive (false);
		PauseMenuPanel.SetActive (false);
		OtherProjsPanel.SetActive (false);
		CurrentScreen = panel;

		switch (panel) {
		case GuiScreen.NameSelection:
			NameSelectionPanel.SetActive(true);
			TutorialOverlayPanel.SetActive (false);
			break;
		case GuiScreen.ClassSelection:
			ClassSelectionPanel.SetActive(true);
			break;
		case GuiScreen.Logout:
			PauseMenuPanel.SetActive(true);
			break;
		case GuiScreen.SEIinfo:
			OtherProjsPanel.SetActive(true);
			break;
		case GuiScreen.InGame:
			GuiPanel.SetActive(true);
			break;
		default:
			break;
		}
	}
		
	#region Upgrades
	// Player upgrades the ship stat
	public void SelectUpgrade(string type)
	{
		if (OnUpgrade != null)
		{
			switch (type) {
			case "Health":
				OnUpgrade (Upgrade.Health);
				break;
			case "Damage":
				OnUpgrade (Upgrade.Damage);
				break;
			case "Speed":
				OnUpgrade (Upgrade.Speed);
				break;
			default:
				Debug.Log ("Unknown upgrade GUI type selection: " + type);
				break;
			}
		}
	}	

	// Update the ship stats: health, damage, and speed
	public void HandleShipUpgrade (Ship ship, Upgrade upgradeType, int upgradeLevel)
	{
		var upgradeCost = Upgrades.UpgradeCost (upgradeLevel, upgradeType, ship.shipType);
		var upgradeCostText = upgradeCost == -1 ? "--" : upgradeCost.ToString();

		switch (upgradeType) {
		case Upgrade.Health:
			healthUpgradeText.text = upgradeLevel.ToString () + "/5";
			healthUpgradeCost.text = upgradeCostText;
			break;
		case Upgrade.Damage:
			damageUpgradeText.text = upgradeLevel.ToString () + "/5";
			damageUpgradeCost.text = upgradeCostText;
			break;
		case Upgrade.Speed:
			speedUpgradeText.text = upgradeLevel.ToString () + "/5";
			speedUpgradeCost.text = upgradeCostText;
			break;
		}
	}
	#endregion

	#region Cannon Cooldown
	// Start cannon cooldown indicators
	public void LeftCannonCooldown(float newCooldown)
	{
		cooldown = newCooldown;
		leftCooldownImage.fillAmount = .01f;
	}
	public void RightCannonCooldown(float newCooldown)
	{
		cooldown = newCooldown;
		rightCooldownImage.fillAmount = .01f;
	}
	#endregion

	public void SelectEmoticon(String emoticon) {
		var map = new Dictionary<String, Emoticon> {
			{"Angry", Emoticon.Angry},
			{"Adkins", Emoticon.Adkins},
			{"Sad", Emoticon.Sad},
			{"Hook", Emoticon.Hook}
		};

		if (this.OnEmoticon != null) {
			this.OnEmoticon (map[emoticon]);
		}
	}

	// Convert a number to it's english suffixed representation
	// E.g. 1 => "1st", 10 => "10th"
	// Does not work for numbers greater than 110
	public static string NumberOrdinality(int number) {
		// English is weird.
		if (number % 10 == 1 && number != 11) {
			return number.ToString () + "st";
		} else if (number % 10 == 2 && number != 12) {
			return number.ToString () + "nd";
		} else if (number % 10 == 3 && number != 13) {
			return number.ToString () + "rd";
		} else {
			return number.ToString () + "th";
		}
	}

	// Build a heading notification with the heading larger than the text
	// Use this function to provide consistent font sizes
	public static string BuildHeadingNotification(string heading, string message) {
		return string.Format("<size=40>{0}</size>\n{1}", heading, message);
	}

	public void SelectBuyRandomItem() {
		int itemIndex = UnityEngine.Random.Range (0,5);
		switch (itemIndex) {
			case (int)PowerUps.Spyglass:
				
				break;
			case (int)PowerUps.PowderKeg:
			
				break;
			case (int)PowerUps.CannonShot:
			
				break;
			case (int)PowerUps.LemonJuice:
			
				break;
			case (int)PowerUps.WindBucket:
			
				break;
			default:
				break;
		}
	}
}