#region Using
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using EZCameraShake;
#endregion

[RequireComponent (typeof(Rigidbody))]
public class Ship : NetworkBehaviour
{
	#region Fields, Constants, Events & Syncvars
	#region Prefabs
	[SerializeField] ParticleSystem UpgradeFXPrefab;
	#endregion

	#region Upgrade Constants

	public const float LEMON_JUICE_MULTIPLIER = .2f;
	public const float CANNON_SHOT_MULTIPLIER = 1.5f;
	public const float HEALTH_UPGRADE_INCREMENT = 25f;
	public const float DAMAGE_UPGRADE_INCREMENT = 2f;
	public const float SPEED_UPGRADE_INCREMENT = 3f;
	public const float ACCELERATION_UPGRADE = 200f;

	#endregion

	#region Ship Stats

	[Header ("Ship Stat")]
	[SerializeField] [SyncVar] float maxHealth;
	[SerializeField] [SyncVar] [Tooltip ("Speed")]float fullTorqueOverAllWheels;
	[SerializeField] [Tooltip ("Backward Speed")]float reverseTorque;
	[SerializeField] [SyncVar] float damage;
	[SerializeField] [Tooltip ("Grip Force")]float downForce;
	[SerializeField] float brakeTorque;
	[SerializeField] float maximumSteerAngle;
	[SerializeField] [SyncVar] [Tooltip ("Speed Limitation")]float maximumSpeed;
	[SerializeField] float knockBackForce;
	[SerializeField] float rayLength;
	[SerializeField] [Range(0f, 1f)] float steerHelper; // 0 is raw physics , 1 the car will grip in the direction it is facing
	#endregion

	#region Wheels

	[Header ("Wheels")]
	[SerializeField] WheelCollider[] wheels = new WheelCollider[4];

	#endregion

	#region Cannons

	[Header ("Projectile")]
	[SerializeField] GameObject cannonBallPrefab;
	[SerializeField] Transform[] shotPositionLeft;
	[SerializeField] Transform[] shotPositionRight;
	[SerializeField] float projectileSpeed;
	[SerializeField] float projectilesOffset;
	[SerializeField] float cooldown;
	float elapsedTimeOnRight = 0f;
	float elapsedTimeOnLeft = 0f;

	#endregion

	#region PowerUps

	[Header ("PowerUps")]
	[SerializeField] GameObject powderKegPrefab;
	[SerializeField] Transform powderKegPosition;
	[SerializeField] float barrelSpeed = 5f;

	public Transform PowderKegPosition {
		get {
			return powderKegPosition;
		}
	}

	#endregion

	#region Health & Upgrades

	//Health and upgrades
	[SyncVar (hook = "UpdateHealth")]
	float currentHealth;
	[SyncVar (hook = "UpdateHealthUpgrade")]
	public int healthUpgrades = 0;
	[SyncVar (hook = "UpdateDamageUpgrade")]
	public int damageUpgrades = 0;
	[SyncVar (hook = "UpdateSpeedUpgrade")]
	public int speedUpgrades = 0;
	int[] upgradeCost = { 75, 100, 125, 150, 175, 125, 175, 225, 275 };

	#endregion

	#region Rigidbodys

	Rigidbody rb;

	#endregion

	#region UI & EventSystem

	UserInterface userInterface;
	EventSystem eventSystem;

	#endregion

	#region Events

	public Action<Ship> OnKeel;
	public Action<Ship, Upgrade, int> OnUpgradeAuthority;
	public Action<Chest> OnChestGetServer;

	#endregion

	#region Synchvars

	[SyncVar]
	float damageMultiplier = 1f;
	[SyncVar]
	public ClassType shipType;

	#endregion

	#region Player & Miscellaneous
	[Header ("Misc")]
	[SerializeField] float unsafeSpeedLevel;
	[SerializeField] float collisionDamage;
	[SerializeField] ParticleSystem bubbleTrailer;
	[SerializeField] ParticleSystem[] fireFxs;
	[SerializeField] Material blackMaterial;
	[SerializeField] Material bettyHwiteMaterial;
	Material oldSailMaterial;
	[SyncVar(hook="HandlePlayerSet")]
	public GameObject playerObject;
	public Player player;

	public Text playerName;

	[SyncVar] private bool canShoot = true;
	private bool isCarryingMissionObject = false;
	private float steerAngle;
	private float oldRotation;

	public  float  currentSpeed{ get { return rb.velocity.magnitude; } }

	#endregion

	#region Sound
	[Header ("SoundFX")]
	[SerializeField] AudioSource GoldAudioSource;
	[SerializeField] AudioClip   GoldSFX;
	[SerializeField] AudioSource BurningFireAudioSource;
	[SerializeField] AudioClip   BurningFireSFX;
	[SerializeField] AudioSource ShipMovementAudioSource;
	[SerializeField] AudioClip   SinkingBubblesSFX;
	[SerializeField] AudioSource CannonsFiringAudioSource;
	[SerializeField] AudioClip	 CannonsFiringSFX;

	#endregion

	#endregion

	#region Functions & Methods

	#region Standard Functions (Awake, OnStartAuthority, FixedUpdate)

	private float magnitudeHit;
	private bool  canCollideDamage;

	// Assign rigidbody, health, and connect to the event system
	void Awake ()
	{
		rb               = GetComponent<Rigidbody> ();
		eventSystem      = GameObject.Find ("EventSystem").GetComponent<EventSystem> ();
		currentHealth    = maxHealth;
		canCollideDamage = true;
		StartCoroutine (FixedUpdateHealth ());
	}

	// Runs on the server
	public override void OnStartServer()
	{
		currentHealth = maxHealth;
		HandlePlayerSet (this.playerObject);
	}

	// Runs on all clients
	public override void OnStartClient()
	{
		HandlePlayerSet (this.playerObject);
		this.playerName.text = this.player.playerName;
	}

	// Camera follow, GUI health, and subscribe to rades
	public override void OnStartAuthority ()
	{
		GameObject.Find ("Camera").GetComponent<SmoothCamera> ().PlayerCreated (this.transform);
		userInterface   = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
		userInterface.UpdateHealth (currentHealth / maxHealth);
		userInterface.OnUpgrade += UpgradeShip;

		if (this.OnUpgradeAuthority != null) {
			this.OnUpgradeAuthority (this, Upgrade.Damage, 0);
			this.OnUpgradeAuthority (this, Upgrade.Health, 0);
			this.OnUpgradeAuthority (this, Upgrade.Speed, 0);
		}
	}

	// Movement, collision, and firing
	void FixedUpdate ()
	{
		RaycastHit hit; // Shoot raycast to detect objects
		float horizontal = Input.GetAxis ("Horizontal");
		float vertical   = Input.GetAxis ("Vertical");

		//Move fast for debugging purposes
		if(Input.GetKeyDown(KeyCode.F1))
			TargetApplyWindbag (this.gameObject.GetComponent<NetworkIdentity>().clientAuthorityOwner);

		// Change collision detection mode if the raycast hit the target or not
		if (Physics.Raycast (this.transform.position, transform.forward, out hit, rayLength)) {
			if (hit.collider.CompareTag ("TerrainCollider") || hit.collider.CompareTag ("Player")) {
				if (rb.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic) {
					rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
					magnitudeHit = currentSpeed; // Get this ship magnitude for collision damage condition
				} 
				else {
					rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
				}
			}
		}

		if (vertical <= 0) {
			StopBubbleFx ();
		} else {
			if (rb.velocity.magnitude > 5f) {
				StartBubbleFx ();
			}
		}

		if (this.hasAuthority) {
			Move (vertical, horizontal, vertical);
			Fire ();
		}
	}

	// Loops yielding until the players health drops to less than zero.
	// At that point it tirggers events, detaches the camera, and starts
	// the sinking animation in the correct server/client locations.
	// This function can be run everywhere
	[Everywhere]
	IEnumerator FixedUpdateHealth() {
		while (currentHealth > 0) {
			if (currentHealth < (maxHealth * 0.5f)) {
				// Play fire particle;
				StartFireFx ();
			} else {
				// Stop fire particle;
				StopFireFx ();
			}
			yield return new WaitForFixedUpdate ();
		}

		if (currentHealth <= 0) {
			if (isCarryingMissionObject) {
				var missionObject = GetComponentInChildren<RageObject> ();
				var missionObjectPosition = missionObject.GetComponent<Transform> ().position;
				missionObject.GetComponent<Renderer> ().enabled = true;
				missionObject.GetComponent<Transform> ().SetParent (null);
			}
		}

		if (this.OnKeel != null) {
			this.OnKeel (this);
		}

		if (hasAuthority) {
			Debug.Log ("OnEndAuthority");
			GameObject.Find ("Camera").GetComponent<SmoothCamera> ().DetachCamera ();
		}

		if (isClient) {
			rb.isKinematic      = true;
			rb.detectCollisions = false;
			StartCoroutine (SinkRolloverRoutine ());
		}

		// Cleanup
		Destroy (this.gameObject, 5f);
	}

	void OnDestroy()
	{
		if (hasAuthority) {
			userInterface.OnUpgrade -= UpgradeShip;
		}
	}

	#endregion

	#region Ship Movement

	// Movement Calculations
	void Move (float accel, float steer, float brake)
	{		
		accel = Mathf.Clamp (accel, 0, 1);
		steer = Mathf.Clamp (steer, -1, 1);
		brake = -1 * Mathf.Clamp (brake, -1, 0);

		// 0 and 1 in the wheels array are the FL and FR wheels 
		steerAngle = steer * maximumSteerAngle;
		wheels [0].steerAngle = wheels [1].steerAngle = steerAngle;

		if (accel == 0) {
			rb.drag = 1f;
		} else {
			rb.drag = 0;
		}

		SteerHelper ();
		ApplyDrive (accel * 2f, brake);
		AddDownForce ();

		// Speed limitation
		if (rb.velocity.magnitude > maximumSpeed) {
			rb.drag = 2f;
		} 
	}

	// Apply movement to wheels
	void ApplyDrive (float accel, float brake)
	{
		// 2 and 3 in the wheels array are the BL and BR wheels
		float thrustTorque = accel * (fullTorqueOverAllWheels / 2);
		wheels [2].motorTorque = wheels [3].motorTorque = thrustTorque;

		for (int i = 0; i < 4; i++) {
			if (currentSpeed > 7f) {
				wheels [i].brakeTorque = brakeTorque * brake;
			} else if (brake > 0) {
				wheels [i].brakeTorque = 0;
				wheels [i].motorTorque = -reverseTorque * brake;
			}
		}
	}

	void SteerHelper()
	{
		// this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
		if (Mathf.Abs(oldRotation - transform.eulerAngles.y) < 10f)
		{
			var turnadjust = (transform.eulerAngles.y - oldRotation) * steerHelper;
			Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
			rb.velocity = velRotation * rb.velocity;
		}
		oldRotation = transform.eulerAngles.y;
	}

	// Add more grip in relation to speed
	void AddDownForce ()
	{
		rb.AddForce (-transform.up * downForce * rb.velocity.magnitude);
	}
		
	void StartFireFx()
	{
		foreach (var fireFx in fireFxs) {
			var fireEmission = fireFx.emission;
			if (!fireFx.isPlaying) {
				fireFx.Play ();
				fireEmission.enabled = true;
				BurningFireAudioSource.clip = BurningFireSFX;
				StartCoroutine(AudioFade.FadeIn (BurningFireAudioSource, 5f));
				BurningFireAudioSource.loop = true;
			}
		}
	}
		
	void StopFireFx()
	{
		foreach (var fireFx in fireFxs) {
			var fireEmission = fireFx.emission;
			if (fireFx.isPlaying) {
				fireFx.Stop ();
				fireEmission.enabled = false;
				StartCoroutine (AudioFade.FadeOut (BurningFireAudioSource, 5f));
				BurningFireAudioSource.loop = false;
			}
		}
	}
		
	void StartBubbleFx()
	{
		var bubbleEmission = bubbleTrailer.emission;
		bubbleTrailer.Play ();
		bubbleEmission.enabled = true;
	}

	void StopBubbleFx()
	{
		var bubbleEmission = bubbleTrailer.emission;
		bubbleTrailer.Stop ();
		bubbleEmission.enabled = false;
	}
	#endregion

	#region Firing & Damage & Collisions

	// Shoot cannonballs
	[Client]
	public void Fire ()
	{
		if (!eventSystem.IsPointerOverGameObject () || Input.mousePosition.x < Screen.width * .8f && Input.mousePosition.y > Screen.height * .3f) {
			if (Input.GetMouseButtonDown (0) && Time.time > elapsedTimeOnLeft && canShoot) {
				elapsedTimeOnLeft = Time.time + cooldown;
				CmdShootLeft ();
				CannonsFiringAudioSource.clip = CannonsFiringSFX;
				CannonsFiringAudioSource.Play ();

				if (this.hasAuthority) {
					userInterface.LeftCannonCooldown(cooldown);
				}
			}
		}
		if (Input.GetMouseButtonDown (1) && Time.time > elapsedTimeOnRight && canShoot) {
			elapsedTimeOnRight = Time.time + cooldown;
			CmdShootRight ();
			CannonsFiringAudioSource.clip = CannonsFiringSFX;
			CannonsFiringAudioSource.Play ();

			if (this.hasAuthority) {
				userInterface.RightCannonCooldown(cooldown);
			}
		}
	}

	// Fire left cannons on server
	[Command]
	void CmdShootLeft ()
	{
		StartCoroutine (InstantiateShotsOnLeft ());
		RpcShootLeft ();
	}

	// Fire right cannons on server
	[Command]
	void CmdShootRight ()
	{
		StartCoroutine (InstantiateShotsOnRight ());
		RpcShootRight ();
	}

	// Fire cannons on the left side of this ship
	[ClientRpc]
	void RpcShootLeft ()
	{
		if (!isServer) {
			StartCoroutine (InstantiateShotsOnLeft ());
		}
	}

	// Fire cannons on the right side of this ship
	[ClientRpc]
	void RpcShootRight ()
	{
		if (!isServer) {
			StartCoroutine (InstantiateShotsOnRight ());
		}
	}

	public void setCanShoot(bool ableToShoot)
	{
		canShoot = ableToShoot;
	}

	// Instantiate left cannonballs on both the server and the client
	IEnumerator InstantiateShotsOnLeft ()
	{
		foreach (Transform shotPosition in shotPositionLeft) {
			GameObject cannonBall = (GameObject)Instantiate (cannonBallPrefab, shotPosition.position, Quaternion.identity);
			if (damageMultiplier > 1f) {
				cannonBall.GetComponent<TrailRenderer> ().material = cannonBall.GetComponent<TrailRenderer> ().materials [0];
			} else {
				cannonBall.GetComponent<TrailRenderer> ().material = cannonBall.GetComponent<TrailRenderer> ().materials [1];
			}
			CannonBall cannonScript = cannonBall.GetComponent<CannonBall> ();
			cannonScript.ballDamage = damage * damageMultiplier;
			cannonBall.GetComponent<Rigidbody> ().velocity = -transform.right * projectileSpeed;
			yield return new WaitForSeconds (projectilesOffset);
		}
		StopCoroutine (InstantiateShotsOnLeft ());
	}

	// Instantiate right cannonballs on both the server and the client
	IEnumerator InstantiateShotsOnRight ()
	{
		foreach (Transform shotPosition in shotPositionRight) {
			GameObject cannonBall = (GameObject)Instantiate (cannonBallPrefab, shotPosition.position, Quaternion.identity);
			if (damageMultiplier > 1f) {
				cannonBall.GetComponent<TrailRenderer> ().material = cannonBall.GetComponent<TrailRenderer> ().materials [0];
			} else {
				cannonBall.GetComponent<TrailRenderer> ().material = cannonBall.GetComponent<TrailRenderer> ().materials [1];
			}
			CannonBall cannonScript = cannonBall.GetComponent<CannonBall> ();
			cannonScript.ballDamage = damage * damageMultiplier;
			cannonBall.GetComponent<Rigidbody> ().velocity = transform.right * projectileSpeed;
			yield return new WaitForSeconds (projectilesOffset);
		}
		StopCoroutine (InstantiateShotsOnRight ());
	}

	[Server]
	public void TakeDamage(float amount) {
		currentHealth -= amount;
	}

	// Delay collision damage
	[Server]
	public IEnumerator DamageTimerCollision() {
		yield return new WaitForSeconds (1.5f);
		canCollideDamage = true;
	}

	[Everywhere]
	void OnCollisionEnter(Collision other)
	{
		if (!other.collider.CompareTag ("Water")) {
			if (other.collider.CompareTag ("OffMap")) {
				if (isServer) {
					TakeDamage (3000f);
				}
			}

			// Shake the camera when hit
			if (hasAuthority) {
				if (magnitudeHit > 0f) {
					EZCameraShake.CameraShaker.Instance.ShakeOnce (1f * magnitudeHit / 2f, 1f, .3f, .5f);
				}
				EZCameraShake.CameraShaker.Instance.ShakeOnce (1f * rb.velocity.magnitude / 2f, 1f, .3f, .5f);
			}

			// Collision damage and effect when this ship collides to the target
			if ((magnitudeHit > unsafeSpeedLevel || rb.velocity.magnitude > unsafeSpeedLevel) && canCollideDamage) {
				// Calculate the damage recevied from collision
				if (isServer) {
					// Collides to terrain
					if (other.collider.CompareTag ("TerrainCollider") || other.collider.CompareTag ("Tower")) {
						// Front collision
						if (magnitudeHit > unsafeSpeedLevel) {
							TakeDamage (collisionDamage * magnitudeHit * 0.5f);
							magnitudeHit = 0f;
							canCollideDamage = false;
							StartCoroutine (DamageTimerCollision ());
						}
						// Side collision
						if (rb.velocity.magnitude > unsafeSpeedLevel && magnitudeHit < unsafeSpeedLevel) {
							TakeDamage (collisionDamage * rb.velocity.magnitude * 0.3f);
							canCollideDamage = false;
							StartCoroutine (DamageTimerCollision ());
						}
					}
				}

				// Collision damage dealt to other's ship
				if (other.collider.CompareTag ("Player")) {
					if (isServer) {
						other.gameObject.GetComponent<Ship> ().TakeDamage (collisionDamage * magnitudeHit);
						TakeDamage (collisionDamage * magnitudeHit * 0.5f);
					}
					canCollideDamage = false;
					StartCoroutine (DamageTimerCollision ());
				}

				// Ship bounces off whenever it collides with terrain(s) or other ship(s)
				if (!other.collider.CompareTag ("CannonBall")) {
					var knockBackForceDirection = other.transform.position - transform.position;
					rb.AddForce (-knockBackForce * knockBackForceDirection.normalized, ForceMode.Impulse);
				}
			}
		}
	}


	#endregion

	#region Health

	// Get the current health
	void UpdateHealth (float newHealth)
	{
		currentHealth = newHealth;

		if (this.hasAuthority) {
			userInterface.UpdateHealth (newHealth / maxHealth);
		}
	}

	#endregion

	#region Upgrades

	// Increase the maximum amount of health
	void UpdateHealthUpgrade (int upgradeLevel)
	{
		if (this.hasAuthority && this.OnUpgradeAuthority != null) {
			this.OnUpgradeAuthority (this, Upgrade.Health, upgradeLevel);
		}
	}

	// Increase the maximum amount of damage dealt
	void UpdateDamageUpgrade (int upgradeLevel)
	{
		if (this.hasAuthority && this.OnUpgradeAuthority != null) {
			this.OnUpgradeAuthority (this, Upgrade.Damage, upgradeLevel);
		}
	}

	// Increase the maximum speed
	void UpdateSpeedUpgrade (int upgradeLevel)
	{
		if (this.hasAuthority && this.OnUpgradeAuthority != null) {
			this.OnUpgradeAuthority (this, Upgrade.Speed, upgradeLevel);
		}
	}

	// Select a ship upgrade
	[Client]
	public void UpgradeShip (Upgrade upgradeType)
	{
		CmdUpgradeShip (upgradeType);
	}

	// Perform upgrade and deduct gold
	[Command]
	void CmdUpgradeShip (Upgrade upgradeType)
	{
		int cost;

		// Upgrade appropriate stat
		switch (upgradeType) {
		case Upgrade.Health:
			cost = Upgrades.UpgradeCost (healthUpgrades, upgradeType, shipType);
			break;
		case Upgrade.Damage:
			cost = Upgrades.UpgradeCost (damageUpgrades, upgradeType, shipType);
			break;
		case Upgrade.Speed:
			cost = Upgrades.UpgradeCost (speedUpgrades, upgradeType, shipType);
			break;
		default:
			Debug.Log ("Invalid upgrade type in Ship class");
			return;
		}

		if (cost == -1) {
			return;
		}

		// If  the player can afford the upgrade
		if (player.Gold - cost >= 0) {
			player.Gold -= cost;

			GoldAudioSource.clip = GoldSFX;
			GoldAudioSource.Play();

			// Upgrade the ship
			switch (upgradeType) {
			case Upgrade.Health:
				healthUpgrades++;
				maxHealth += HEALTH_UPGRADE_INCREMENT;
				currentHealth += HEALTH_UPGRADE_INCREMENT;
				Color healthColor = new Color (1f, 0.8f, 0f); 
				RpcUpgradeFX (healthColor);
				break;
			case Upgrade.Damage:
				damageUpgrades++;
				damage += DAMAGE_UPGRADE_INCREMENT;
				Color damageColor = new Color (1f, 0.149f, 0f);
				RpcUpgradeFX (damageColor);
				break;
			case Upgrade.Speed:
				speedUpgrades++;
				maximumSpeed += SPEED_UPGRADE_INCREMENT;
				fullTorqueOverAllWheels += ACCELERATION_UPGRADE;
				Color speedColor = new Color (0.082f, 0.913f, 0.913f);
				RpcUpgradeFX (speedColor);
				break;
			}
		}
	}

	[ClientRpc]
	private void RpcUpgradeFX(Color color)
	{
		UpgradeFXPrefab.Clear ();
		var main = UpgradeFXPrefab.main;
		main.startColor = color;
		UpgradeFXPrefab.Play ();
	}

	#endregion

	#region Sinking

	// Rotates and dives the ship beneath the water upon death
	[Client]
	IEnumerator SinkRolloverRoutine ()
	{
		ShipMovementAudioSource.clip = SinkingBubblesSFX;
		ShipMovementAudioSource.Play();

		BurningFireAudioSource.Stop ();

		for (float f = 0f; f <= 180f; f++) {
			this.gameObject.transform.Rotate (0, 0, 1.15f, Space.World);
			this.gameObject.transform.Translate (0, -0.335f, 0, Space.World);
			yield return new WaitForFixedUpdate();
		}
	}

	#endregion

	#region Chests

	// If the ship collides with a chest, pick it up
	[Everywhere]
	void OnTriggerEnter (Collider collision)
	{
		if (collision.gameObject.tag == "Chest") {
			Chest chest = collision.gameObject.GetComponent<Chest> ();

			if (isServer) {
				if (this.OnChestGetServer != null) {
					this.OnChestGetServer (chest);
				}
			}
		}
	}

	#endregion

	#region Items

	// Call and use selected item on server
	[Server]
	public void UseItem (PowerUps powerUp)
	{
		switch (powerUp) {
		case PowerUps.LemonJuice:
			LemonJuiceHeal ();
			break;
		case PowerUps.CannonShot:
			StartCoroutine (CannonShotRoutine ());
			break;
		case PowerUps.PowderKeg:
			DropPowderKeg ();
			break;
		case PowerUps.WindBucket:
			TargetApplyWindbag (this.gameObject.GetComponent<NetworkIdentity>().clientAuthorityOwner);
			break;
		default:
			Debug.Log ("Incorrect powerup type in Ship Class");
			break;
		}
	}

	// Cannon Shot duration routine
	[Server]
	IEnumerator CannonShotRoutine ()
	{
		damageMultiplier = CANNON_SHOT_MULTIPLIER;
		yield return new WaitForSeconds (10f);
		damageMultiplier = 1f;
		StopCoroutine (CannonShotRoutine ());
	}

	// Heal player
	[Server]
	public void LemonJuiceHeal ()
	{
		currentHealth += maxHealth * LEMON_JUICE_MULTIPLIER;

		if (currentHealth > maxHealth)
			currentHealth = maxHealth;
	}

	// Drop powder keg out of the ship
	[Server]
	private void DropPowderKeg ()
	{
		GameObject powderKeg = GameObject.Instantiate (powderKegPrefab, powderKegPosition.position, powderKegPrefab.transform.rotation);
		powderKeg.GetComponent<Rigidbody> ().velocity = -powderKegPosition.forward * barrelSpeed + powderKegPosition.up * -25f;
		powderKeg.GetComponent<Rigidbody> ().angularVelocity = -transform.right * 2f;
		NetworkServer.Spawn (powderKeg);
	}

	// Call Bucket O' Wind on Client
	[TargetRpc]
	void TargetApplyWindbag (NetworkConnection connection)
	{
		rb.AddRelativeForce (0f, 0f, 5f, ForceMode.VelocityChange);
		StartCoroutine (WindBagDelay());
	}
	IEnumerator WindBagDelay()
	{
		yield return new WaitForSeconds(Mathf.Epsilon);
		rb.AddRelativeForce (0f, 0f, 200f, ForceMode.VelocityChange);
	}

	#endregion

	#region Events

	[ClientRpc]
	public void RpcMakeSailsBlack()
	{
		var shipParts = GetComponentsInChildren<MeshRenderer> ();
		foreach (var part in shipParts) {
			if (part.gameObject.tag == "Sail") {
				oldSailMaterial = part.material;
				part.material = blackMaterial;
			}
		}
	}

	[ClientRpc]
	public void RpcMakeSailsWhite()
	{
		var shipParts = GetComponentsInChildren<MeshRenderer> ();
		foreach (var part in shipParts) {
			if (part.gameObject.tag == "Sail") {
				oldSailMaterial = part.material;
				part.material = bettyHwiteMaterial;
			}
		}
	}
		
	[ClientRpc]
	public void RpcRevertSailColor()
	{
		if (oldSailMaterial != null) {
			var shipParts = GetComponentsInChildren<MeshRenderer> ();
			foreach (var part in shipParts) {
				if (part.gameObject.tag == "Sail")
					part.material = oldSailMaterial;
			}
		}
	}

	public void setIsCarryingMissionObject(bool value)
	{
		isCarryingMissionObject = value;
		print (isCarryingMissionObject);
	}

	#endregion

	#region Player & PlayerName
	void HandlePlayerSet(GameObject newPlayerObject) {
		this.playerObject = newPlayerObject;
		this.player = newPlayerObject.GetComponent<Player> ();
	}

	// Set player's name across the network
	[ClientRpc]
	public void RpcSetPlayerName(string name)
	{
		Debug.Log ("Setting player name");
		playerName.text = name;
	}
	#endregion

	#endregion
}