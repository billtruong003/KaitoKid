using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Shmackle.Minigames.BloodJman;
using _Shmackle.Scripts.Minigames;
using _Shmackle.Scripts.Player;
using Autohand;
using Autohand.Demo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using Fusion.Addons.Physics;
using NaughtyAttributes;
using Photon.Voice.Unity;
using Shmackle;
using Shmackle.Analytics;
using Shmackle.Data;
using Shmackle.Minigames.PropHunter;
using Shmackle.PlayFab;
using Shmackle.Runtime;
using Shmackle.Sound;
using Shmackle.Utils;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Hand = Autohand.Hand;
using CrazyMinnow.SALSA;

public struct NetworkedPose : INetworkStruct
{
	public Vector3 Position;
	public Quaternion Rotation;

	// Copy from a Unity Transform
	public static NetworkedPose FromTransform(Transform transform)
	{
		return new NetworkedPose
		{
			Position = transform.position,
			Rotation = transform.rotation
		};
	}

	// Apply to a Unity Transform
	public void ApplyTo(Transform transform, float lerpSpeed)
	{
		transform.position = Vector3.Lerp(transform.position, Position, Time.deltaTime * lerpSpeed);
		transform.rotation = Quaternion.Slerp(transform.rotation, Rotation, Time.deltaTime * lerpSpeed);
	}
}

[DefaultExecutionOrder(EXECUTION_ORDER)]
public class ShmackleNetworkRig : NetworkBehaviour
{
	public const int EXECUTION_ORDER = 100;

	const float positionThreshold = 0.015f;
	const float rotationThreshold = 0.015f;

	#region #----- Fields -----#

	public float lerpSpeed = 15;

	public GameObject playerRootObject;
	public ShmacklePlayerController playerController;
	public GameObject localRig;
	public GameObject bodyRenderLogic;
	public GameObject characterIK;
	public GameObject characterRender;
	public GameObject headTarget;
	public GameObject leftControllerNetwork;
	public GameObject rightControllerNetwork;
	public GameObject physicsRig;
	public GameObject playerMobile;
	public Animator phoneAnimator;
	public Grabbable leftHandleGrabbable;
	public Grabbable rightHandleGrabbable;
	public GameObject mobileController;
	public GameObject[] watchersUI;
	public PlayerHealthSimple playerHealth;
	public GameObject gunAttachment;

	[HorizontalLine]
	public Transform leftArmAttachment;

	[HorizontalLine]
	public GameObject autoHandLeft;
	public GameObject autoHandRight;
	[Space]
	public GameObject autoHandLeftCollider;
	public GameObject autoHandRightCollider;
	[Space] public float maxArmLength = 1.5f;
	public bool isFreezeHand;

	[HorizontalLine]
	public GameObject playerCamera;
	public ParticleSystem peeEffect;
	public AudioSource peeAudio;

	[HorizontalLine]
	public Recorder voiceChatRecorder;
	public AudioSource voiceChatAudio;
	public Speaker voiceChatSpeaker;
	[SerializeField] private SkinnedMeshRenderer bodySkinned; 
	//[Networked] public float talkingBlendShapeNetwork { get; set; }
	//[Networked] public float showTeethBlendShapeNetwork { get; set; }
	//[SerializeField]private bool isBlendShapeVaild;

	//public Salsa salsaComponent;

	[HorizontalLine]
	public bool IsLocalNetworkRig;
	public PlayerRef PlayerRef { get; private set; } = PlayerRef.None;
	public TextMeshPro playerNameText;
	public RectTransform playerNameRectTransform;

	[HorizontalLine]
	public DripManager dripManager;
	public GearManager gearManager;

	// public NetworkTransform[] networkTransforms;
	public Transform characterTransform;
	public Transform headTransform;
	public Transform leftControllerTransform;
	public Transform rightControllerTransform;
	public Transform characterIkTransform;
	[Networked] public NetworkedPose characterNetworkPose { get; set; }
	[Networked] public NetworkedPose headNetworkPose { get; set; }
	[Networked] public NetworkedPose leftControlerNetworkPose { get; set; }
	[Networked] public NetworkedPose rightControllerNetworkPose { get; set; }
	[Networked] public NetworkedPose characterIkNetworkPose { get; set; }

	[HorizontalLine]
	[Header("Player Name Pos")]
	[SerializeField] private Vector3 _playerNameDefaultPosition = new(-0.0007f, 0.0008f, 0.0014f);
	[SerializeField] private Color _playerNameDefaultColor = Color.white;
	[HorizontalLine] private float _armorZOffset = 0.00222f;

	private PlayerGameModule _playerGameModule;
	private RemoteInventory _playerInventory = new();

	private Rigidbody _playerRigidbody;

	public bool IsPeeing { get; private set; }
	public int playerID;
	public event Action<bool> onSpawned;

	[Header("Remote Disable Objects")]
	[SerializeField]
	private List<Collider> _remoteDisablerColliders;

	[SerializeField] public EffectHub EffectHub;

	public bool isUseLeftAutohandPhysics
	{
		get
		{
			return _isUseLeftAutohandPhysics;
		}
		set
		{
			_isUseLeftAutohandPhysics = value;
			if (_isUseLeftAutohandPhysics)
			{
				playerController.autoHandLeft.enableMovement = true;
				playerController.autoHandLeftRigidbody.isKinematic = false;
			}
			else
			{
				autoHandLeft.transform.rotation = playerController.leftHandPosition.rotation;

				playerController.autoHandLeft.enableMovement = false;

				playerController.autoHandLeftRigidbody.isKinematic = true;
			}
		}
	}
	
	public bool isUseRightAutohandPhysics
	{
		get
		{
			return _isUseRightAutohandPhysics;
		}
		set
		{
			_isUseRightAutohandPhysics = value;
			if (_isUseRightAutohandPhysics)
			{
				playerController.autoHandRight.enableMovement = true;
				playerController.autoHandRightRigidbody.isKinematic = false;
			}
			else
			{

				autoHandRight.transform.rotation = playerController.rightHandPosition.rotation;

				playerController.autoHandRight.enableMovement = false;

				playerController.autoHandRightRigidbody.isKinematic = true;
			}
		}
	}

	[Header(" Wrist Controller")]
	public Transform wristTransform; // Assign your wrist or controller Transform
	public Transform objectToMove;   // Assign the object to move
	public float moveSpeed = 1f;     // Movement speed multiplier
	public float deadZone = 5f;      // Degrees to ignore small wrist rotations

	[Header("Xray")]
	public XRayController xRayController;

	public bool IsSyncedPlayerData
	{
		get
		{
			return _isSyncedPlayerData;
		}
	}

	private bool _isSyncedPlayerData = false;
	//[Networked, Capacity(20)] public string ID {set; get; }
	private bool _isInitializeDrip = false; //True after all drip data is loaded.
	public bool isInitializeDrip => _isInitializeDrip;
	private bool _isInitializeGear = false;
	
	private Dictionary<string, NetworkObject> _addditionalGrabableItems = new();
	#endregion

	public int finderPriority = ChasingFinderController.DEFAULT_PRIORITY; // Using for john toe to check priority

	public string DisplayName = string.Empty;

	[Networked]
	public string networkPlayerName { get; set; }
	
	[Networked]
	public string networkPlayfabId { get; set; }
	
	[Networked]
	public NetworkBool isInvisible { get; set; }
	[Networked]
	public NetworkBool isMonster { get; set; }
	[Networked]
	public NetworkBool isFreeze { get; set; }
	
	[Networked] public float PropRotationY { get; set; }
	
	[Networked] public NetworkBool isDripTransformation { get; set; } = false;

	public bool isInGertGame { get; set; }

	public bool isInBloodJmanGame { get; set; }

	public bool CanDripTransformation => 
		isInGertGame == false && 
		isInBloodJmanGame == false &&
		playerHealth.IsDead == false &&
		isMonster == false &&
		dripManager.CanTransform;
	
	#region #----- Network Methods ------#
	private float _priorityTimer = 0f;
	
	private bool _isUseLeftAutohandPhysics;
	private bool _isUseRightAutohandPhysics;

	public event Action<bool> onChangedMonster;

	private bool isSpawned;	
	
	
	public override void Spawned()
	{
		try
		{
			ManuallySpawned();
			playerID = PlayerRef.PlayerId;

			if (IsLocalNetworkRig)
			{
				DOVirtual.DelayedCall(3, () =>
				{
					ShmackleGameManager.Instance.checkPlayerInParty();
				}).OnComplete(() =>
				{
					var playerInParty =
						ShmackleGameManager.Instance.playerInParty.FirstOrDefault(p => p.playfabID == Runner.UserId);
					//Debug.Log("playerInParty found " + playerInParty.playerName);
					if (playerInParty != null)
					{
						playerInParty.roomID = ShmackleGameManager.Instance.privateRoomCode;
						playerInParty.sceneName = SceneManager.GetActiveScene().name + "#" + ShmackleGameManager.Instance.privateRoomCode;
						Debug.Log("update scene name" + playerInParty.sceneName);
					}
					foreach (var player in ShmackleGameManager.Instance.playerInParty)
					{
						player.isChangeScene = false;
					}
				});
				Debug.Log("1 SUPDATE far clip plane: ----" + ShmackleConnectionManager.Instance.defaultFarClipPlane);
				
				DOVirtual.DelayedCall(0.5f, () =>
				{
					if (!ShmackleConnectionManager.Instance.EnableFogByDefault)
					{
						playerController.fogSphere.gameObject.SetActive(false);
						playerController.HeadCamera.clearFlags = CameraClearFlags.Skybox;
					}

					if (ShmackleConnectionManager.Instance.EnableOcclusionCulling)
					{
						playerController.HeadCamera.useOcclusionCulling = true;
					}

					if (ShmackleConnectionManager.Instance.EnableSkyBox)
					{
						Debug.Log("setup skybox");
						playerController.HeadCamera.clearFlags = CameraClearFlags.Skybox;
					}

					Debug.Log("UPDATE far clip plane: ----" + ShmackleConnectionManager.Instance.defaultFarClipPlane);
					
					if (ShmackleConnectionManager.Instance.defaultFarClipPlane != 0)
						playerController.HeadCamera.farClipPlane = ShmackleConnectionManager.Instance.defaultFarClipPlane;
				});
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}

		isSpawned = true;
	}

	private void ManuallySpawned()
	{
		Debug.Log("ManuallySpawned");

		if (playerController.runnerExpectations == ShmacklePlayerController.RunnerExpectations.Offline)
			return;
		PlayerRef = Object.InputAuthority;
		IsLocalNetworkRig = Object.HasInputAuthority;
		var parent = transform.parent;

		if (IsLocalNetworkRig)
		{
			playerController.playerInputListener.enabled = true;
			playerController.playerInputListener.EnableInputActions();

			playerController.enabled = true;
			localRig.SetActive(true);

			parent.gameObject.name = "Local Player #" + PlayerRef.PlayerId;


			var leftSync = autoHandRight.GetComponent<NetworkRigidbody3D>();

			if (leftSync)
			{
				leftSync.SyncParent = false;
				leftSync.RenderThresholds.Rotation = 0.1f;
				leftSync.RenderThresholds.Scale = 0.1f;
			}


			var rightSync = autoHandLeft.GetComponent<NetworkRigidbody3D>();
			if (rightSync)
			{
				rightSync.SyncParent = false;
				rightSync.RenderThresholds.Rotation = 0.1f;
				rightSync.RenderThresholds.Scale = 0.1f;
			}

			//autoHandRight.AddComponent<NetworkTransform>();
			//autoHandLeft.AddComponent<NetworkTransform>();

			if (RuntimeUserData.CacheUser != null)
			{
				_playerInventory.equipments = RuntimeUserData.CacheUser.inventory.equipments;
				_playerInventory.gearItems = RuntimeUserData.CacheUser.inventory.gearItems;
				_playerInventory.gearEquipments = RuntimeUserData.CacheUser.inventory.gearEquipments;
			}
			else
			{
				Debug.LogError($"RuntimeUserData.CachUser is null.");
			}

			ShmackleConnectionManager.Instance.voiceClient.PrimaryRecorder = voiceChatRecorder;
			ShmackleConnectionManager.Instance.voiceClient.SpeakerPrefab = voiceChatSpeaker.gameObject;

			physicsRig.SetActive(true);
			_playerRigidbody = playerController.GetComponent<Rigidbody>();

			if (ShmackleConnectionManager.Instance.IsOffShore())
			{
				for (int i = 0; i < watchersUI.Length; i++)
				{
					watchersUI[i].SetActive(true);
				}

			}
			else
			{
				for (int i = 0; i < watchersUI.Length; i++)
				{
					watchersUI[i].SetActive(false);
				}

			}

			if (voiceChatRecorder)
			{
				voiceChatRecorder.VoiceDetection = true;
				voiceChatRecorder.VoiceDetectionThreshold = 0.02f; // Tune for your mic
			}

			DOVirtual.DelayedCall(0.25f, () =>
			{
				playerController.autoHandLeft.body.interpolation = RigidbodyInterpolation.Extrapolate;
				playerController.autoHandRight.body.interpolation = RigidbodyInterpolation.Extrapolate;
			});

		}
		else
		{
			playerController.playerHealth.Init();

			if (peeEffect)
			{
				peeEffect.gameObject.SetActive(false);
			}

			playerController.fogSphere.SetActive(false);
			playerController.blackFadingEffect.SetActive(false);

			localRig.SetActive(false);
			physicsRig.SetActive(false);

			// DONT REMOVE RIGIBODY ON REMOTE PLAYER
			// Destroy(playerController.playerRigidbody);

			var playerMobileController = playerMobile.GetComponent<PlayerMobile>();
			playerMobileController.Ui_Holder.SetActive(false);
			playerMobileController.handleLeft.SetActive(false);
			playerMobileController.handleRight.SetActive(false);
			Destroy(playerMobileController);
			mobileController.SetActive(false);
			playerMobile.SetActive(false);

			for (int i = 0; i < watchersUI.Length; i++)
			{
				watchersUI[i].SetActive(false);
			}

			autoHandLeftCollider.gameObject.SetActive(false);
			autoHandRightCollider.gameObject.SetActive(false);

			playerController.playerAbilities.enabled = false;

			parent.gameObject.name = "Remote Player #" + PlayerRef.PlayerId;

			DisableAutoHandForClient();

			Destroy(playerCamera);
			playerController.enabled = false;

			foreach (var disableObj in _remoteDisablerColliders) disableObj.enabled = false;

			Destroy(playerController.autoHandLeftRigidbody);
			Destroy(playerController.autoHandRightRigidbody);

			playerController.playerRigidbody.isKinematic = true;
			playerController.playerRigidbody.useGravity = false;

			if (voiceChatRecorder)
			{
				var audioChangeHandler = voiceChatRecorder.GetComponent<AudioChangesHandler>();
				Destroy(audioChangeHandler);
				Destroy(voiceChatRecorder);
			}

			voiceChatAudio.volume = 100;
			voiceChatRecorder.DebugEchoMode = false;

		}

		DisableCollisionTracker();
		Debug.Log(">> Start: " + transform.parent.name);

		if (ShmackleGameManager.Instance) StartCoroutine(RegisterShmacklePlayer());

		if (HasStateAuthority) RegisterChangeData();

		if (PlayersManager.Instance)
		{
			var snr = GetComponent<ShmackleNetworkRig>();
			PlayersManager.Instance.AddPlayer(snr.PlayerRef.PlayerId, snr);
		}

		finderPriority = ChasingFinderController.DEFAULT_PRIORITY;
		onSpawned?.Invoke(IsLocalNetworkRig);

		// if (bodySkinned.sharedMesh.GetBlendShapeName(0) == "Talking" &&
		//     bodySkinned.sharedMesh.GetBlendShapeName(2) == "Teeth-Show")
		// {
		// 	isBlendShapeVaild = true;
		// }
		// else
		// {
		// 	isBlendShapeVaild = false;
		// }

	}

	private void DisableCollisionTracker()
	{
		var leftCollisionTracker = autoHandLeft.GetComponent<CollisionTracker>();
		var rightCollisionTracker = autoHandRight.GetComponent<CollisionTracker>();

		leftCollisionTracker.enabled = false;
		leftCollisionTracker.enabled = false;

		leftCollisionTracker.disableCollisionTracking = true;
		leftCollisionTracker.disableTriggersTracking = true;

		rightCollisionTracker.disableCollisionTracking = true;
		rightCollisionTracker.disableTriggersTracking = true;
	}

	private void DisableAutoHandForClient()
	{
		autoHandLeft.GetComponent<Hand>().body.isKinematic = true;
		autoHandRight.GetComponent<Hand>().body.isKinematic = true;


		autoHandLeft.GetComponent<HandFollow>().enabled = false;
		autoHandRight.GetComponent<HandFollow>().enabled = false;

		autoHandLeft.GetComponent<Hand>().enabled = false;
		autoHandRight.GetComponent<Hand>().enabled = false;


		autoHandLeft.GetComponent<HandGrabbableHighlighter>().enabled = false;
		autoHandRight.GetComponent<HandGrabbableHighlighter>().enabled = false;


		Destroy(autoHandRight.GetComponent<HandCollisionHaptics>());
		Destroy(autoHandLeft.GetComponent<HandCollisionHaptics>());

		autoHandLeft.GetComponent<OpenXRHandControllerLink>().enabled = false;
		autoHandRight.GetComponent<OpenXRHandControllerLink>().enabled = false;
		//
		// autoHandLeft.GetComponent<HandAnimator>().enabled = false;
		// autoHandRight.GetComponent<HandAnimator>().enabled = false;

		Destroy(autoHandRight.GetComponent<HandPublicEvents>());
		Destroy(autoHandLeft.GetComponent<HandPublicEvents>());
	}

	private IEnumerator RegisterShmacklePlayer()
	{
		yield return new WaitUntil(() => ShmackleGameManager.Instance != null &&
										 ShmackleConnectionManager.Instance != null &&
										 ShmackleConnectionManager.Instance.IsSceneReady);

		var player = ShmackleGameManager.Instance.shmacklePlayerList.FirstOrDefault(x => x.playerId == PlayerRef.PlayerId);
		if (player == null)
		{
			Debug.LogError($"[ShmackleNetworkRig] RegisterShmacklePlayer() player {PlayerRef.PlayerId} not found in PlayerList!");
			yield break;
		}
		
		Debug.Log($"[ShmackleNetworkRig] RegisterShmacklePlayer() {player.playerId} IsLocalNetworkRig {IsLocalNetworkRig} PlayerFabId {Runner.UserId}");
		
		player.networkPlayer = this;
		
		ShmackleGameManager.Instance.OnAddPlayerNetwork(this);
		ShmackleGameManager.Instance.OnNetworkRigInit?.Invoke(player.playerId, this);
		
		if (IsLocalNetworkRig)
		{
			playerController.audioListener.enabled = true;
			ShmackleGameManager.Instance.playerNetworkRig = this;
			ShmackleGameManager.Instance.onLocalPlayerInitialized?.Invoke(this);
			
			//Assign player name for local player
			DisplayName = RuntimeUserData.CacheUser?.displayName ?? "";
			
			player.playfabsId = Runner.UserId;
			player.playerName = DisplayName;
			
			networkPlayerName = DisplayName;
			networkPlayfabId  = Runner.UserId;
			
			playerNameText.SetText(DisplayName);
			
			//OnChangedPlayerName(DisplayName);
			//RegisterPlayfabsId(player.playerId, Runner.UserId);
			if (RuntimeUserData.CacheUser?.gameMaster == true) RegisterGameMaster(player.playerId);
			Debug.Log("Register Local Player success");
		}
		else
		{
			//OnRequestSyncPlayerData(player.playerId);

			while (string.IsNullOrEmpty(networkPlayfabId))
			{
				yield return null;
			}

			DisplayName       = networkPlayerName;
			playerNameText.SetText(DisplayName);

			player.playerName = networkPlayerName;
			player.playfabsId = networkPlayfabId;

			//yield return new WaitUntil(() => string.IsNullOrEmpty(player.playfabsId) == false);
			RequestRmotePlayerInventory(networkPlayfabId);
		}

		playerNameText.gameObject.SetActive(true);
	}

	public void OnUpdatePriority()
	{
		if (_priorityTimer <= ChasingFinderController.TIME_INCREASE_PRIORITY)
		{
			_priorityTimer += Time.deltaTime;
		}
		else
		{
			_priorityTimer = 0;
			finderPriority++;

			if (finderPriority >= ChasingFinderController.DEFAULT_PRIORITY)
			{
				finderPriority = ChasingFinderController.DEFAULT_PRIORITY;
			}
		}
	}

	public override void FixedUpdateNetwork()
	{
		if(!isSpawned && !HasStateAuthority)
			return;
		
		
		//talkingBlendShapeNetwork = bodySkinned.GetBlendShapeWeight(0); //mean talking blend shape
		//showTeethBlendShapeNetwork = bodySkinned.GetBlendShapeWeight(2); // mean show teeth blend shape
		
		
		if (!IsLocalNetworkRig)
			return;

		if (PoseChanged(characterNetworkPose, characterTransform))
			characterNetworkPose = NetworkedPose.FromTransform(characterTransform);

		if (PoseChanged(headNetworkPose, headTransform))
			headNetworkPose = NetworkedPose.FromTransform(headTransform);

		if (PoseChanged(leftControlerNetworkPose, leftControllerTransform))
			leftControlerNetworkPose = NetworkedPose.FromTransform(leftControllerTransform);

		if (PoseChanged(rightControllerNetworkPose, rightControllerTransform))
			rightControllerNetworkPose = NetworkedPose.FromTransform(rightControllerTransform);

		if (PoseChanged(characterIkNetworkPose, characterIkTransform))
			characterIkNetworkPose = NetworkedPose.FromTransform(characterIkTransform);
		
		
	}

	private static bool PoseChanged(NetworkedPose a, Transform b)
	{
		return Vector3.Distance(a.Position, b.position) > positionThreshold ||
			   1f - Mathf.Abs(Quaternion.Dot(a.Rotation, b.rotation)) > rotationThreshold;
	}

	#if UNITY_EDITOR
	private void Update()
	{
		if (UnityEngine.Input.GetKeyDown(KeyCode.T) && CanDripTransformation)
		{
			RPC_ApplyDripTransformation(!isDripTransformation);
		}
	}
	#endif

	private void FixedUpdate()
	{
		if(isFreezeHand)
			return;
		if (IsLocalNetworkRig)
		{
			Transform leftHandSrc = playerController.leftHandPosition;
			Transform rightHandSrc = playerController.rightHandPosition;

			if (isUseLeftAutohandPhysics)
			{
				// --- LEFT HAND POSITION ---
				Vector3 predictedLeftHandPos = leftHandSrc.position - playerController.autoHandLeft.body.position;
				playerController.autoHandLeft.body.linearVelocity = predictedLeftHandPos / Time.fixedDeltaTime;

				// --- LEFT HAND ROTATION ---
				Quaternion leftDeltaRot = leftHandSrc.rotation * Quaternion.Inverse(playerController.autoHandLeft.body.rotation);
				leftDeltaRot.ToAngleAxis(out float leftAngle, out Vector3 leftAxis);
				if (leftAngle > 180f) leftAngle -= 360f; // Keep it minimal
				if (Mathf.Abs(leftAngle) > 0.01f && !float.IsNaN(leftAngle))
				{
					Vector3 leftAngularVelocity = leftAxis * Mathf.Deg2Rad * leftAngle / Time.fixedDeltaTime;
					playerController.autoHandLeft.body.angularVelocity = leftAngularVelocity;
				}

			}

			if (isUseRightAutohandPhysics)
			{
				// --- RIGHT HAND POSITION ---
				Vector3 predictedRightHandPos = rightHandSrc.position - playerController.autoHandRight.body.position;
				playerController.autoHandRight.body.linearVelocity = predictedRightHandPos / Time.fixedDeltaTime;

				// --- RIGHT HAND ROTATION ---
				Quaternion rightDeltaRot = rightHandSrc.rotation * Quaternion.Inverse(playerController.autoHandRight.body.rotation);
				rightDeltaRot.ToAngleAxis(out float rightAngle, out Vector3 rightAxis);
				if (rightAngle > 180f) rightAngle -= 360f;
				if (Mathf.Abs(rightAngle) > 0.01f && !float.IsNaN(rightAngle))
				{
					Vector3 rightAngularVelocity = rightAxis * Mathf.Deg2Rad * rightAngle / Time.fixedDeltaTime;
					playerController.autoHandRight.body.angularVelocity = rightAngularVelocity;
				}
			}

		}
	}

	public override void Render()
	{
		if(isFreezeHand)
			return;
		// Cache hand targets for cleaner access
		Transform leftHandSrc = playerController.leftHandPosition;
		Transform rightHandSrc = playerController.rightHandPosition;

		// Local rig uses fresh hardware input to drive visuals directly
		if (IsLocalNetworkRig)
		{
			SetIfChanged(leftControllerNetwork.transform, playerController.LeftController.transform);
			SetIfChanged(rightControllerNetwork.transform, playerController.RightController.transform);
			SetIfChanged(characterIK.transform, playerController.BodyTarget.transform);
			SetIfChanged(headTarget.transform, playerController.HeadTarget.transform);

			if (!isUseRightAutohandPhysics)
			{
				autoHandRight.transform.SetPositionAndRotation(rightHandSrc.position, rightHandSrc.rotation);
			}
			
			
			if (!isUseLeftAutohandPhysics)
			{
				autoHandLeft.transform.SetPositionAndRotation(leftHandSrc.position, leftHandSrc.rotation);
			}
		}
		else
		{
#if UNITY_EDITOR
			// For update spawn position of debug player spawner
			if (!_isSyncedPlayerData)
			{
				return;
			}
#endif

			characterNetworkPose.ApplyTo(characterTransform, lerpSpeed);
			headNetworkPose.ApplyTo(headTransform, lerpSpeed);
			leftControlerNetworkPose.ApplyTo(leftControllerTransform, lerpSpeed);
			rightControllerNetworkPose.ApplyTo(rightControllerTransform, lerpSpeed);
			characterIkNetworkPose.ApplyTo(characterIkTransform, lerpSpeed);

			// Remote rigs still follow replicated IK/hand targets
			SetIfChanged(autoHandLeft.transform, leftHandSrc);
			SetIfChanged(autoHandRight.transform, rightHandSrc);
		}
	}

	private static void SetIfChanged(Transform target, Transform source)
	{

		//if ((target.position - source.position).sqrMagnitude > positionThreshold)
		{
			target.position = source.position;

		}

		//if (Quaternion.Angle(target.rotation, source.rotation) > rotationThreshold)
		{
			target.rotation = source.rotation;
		}
	}

	#endregion

	#region #----- RPC -----#
	[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
	public async void RPC_EquipDrip(string dripId)
	{
		Debug.Log($"[RPC_EquipDrip] dripId: {dripId}");
		
		var dripData = dripManager.dripDataContainer.Find(dripId);
		if (dripData == null)
		{
			Debug.Log($"[RPC_EquipDrip] dripData is null for dripId: {dripId}");
			return;
		}

		// if (IsLocalNetworkRig == false)
		// {
		// 	// retrieve current player
		// 	var player = ShmackleGameManager.Instance.GetShmacklePlayer(PlayerRef.PlayerId);
		// 	if (player == null)
		// 	{
		// 		Debug.LogWarning($"Player {PlayerRef.PlayerId} not found");
		// 		return;
		// 	}
		//
		// 	// check player own drip
		// 	var isOwn = await PlayFabClientAPIExtensions.IsPlayerOwnItem(player.playfabsId, dripId);
		// 	if (!isOwn)
		// 	{
		// 		Debug.LogWarning($"Player {player.playfabsId} has no drip owner: {dripId}");
		// 		return;
		// 	}
		// }

		var isGrabbableDrip = DripUtils.IsGrabbableDrip(dripData);
		var existingDripDataPack =
			isGrabbableDrip
				? dripManager.GetGrabbableDripData(dripData.pack)
				: dripManager.GetDripData(dripData.pack);
		var existingDripDataId = existingDripDataPack != null ? existingDripDataPack.id : string.Empty;

		if (IsLocalNetworkRig && dripData.pack == DripPack.Outfit)
		{
			isDripTransformation       = false;
		}

		var success = dripManager.EquipDrip(dripData);
		Debug.Log($"[RPC_EquipDrip] _dripManager.EquipDrip success: {success}");
		if (success)
		{
			var shouldSavePlayerData = false;
			
			if (!string.IsNullOrEmpty(existingDripDataId))
			{
				_playerInventory.equipments.Remove(existingDripDataId);
				shouldSavePlayerData = true;
				
				if (IsLocalNetworkRig && existingDripDataPack.hidePartFromCamera)
				{
					dripManager.EnablePartFromCamera(existingDripDataPack.HidePartFromCameraPartType, true);
				}
			}

			if (!_playerInventory.equipments.Contains(dripId))
			{
				_playerInventory.equipments.Add(dripId);
				shouldSavePlayerData = true;

				if (IsLocalNetworkRig)
				{
					AnalyticsHelper.RecordEquipDripItem(dripId, true);
				}
			}

			if (IsLocalNetworkRig && shouldSavePlayerData)
			{
				RuntimeUserData.CacheUser.inventory.equipments = _playerInventory.equipments;
				RuntimeUserData.CacheUser.Save().Forget();
			}

			if (!isGrabbableDrip)
			{
				UpdatePlayerNamePositionAndColor();
			}

			// if (IsLocalNetworkRig)
			// {
			// 	if (bodySkinned.sharedMesh.GetBlendShapeName(0) == "Talking" &&
			// 	    bodySkinned.sharedMesh.GetBlendShapeName(2) == "Teeth-Show")
			// 	{
			// 		isBlendShapeVaild = true;
			// 	}
			// 	else
			// 	{
			// 		isBlendShapeVaild = false;
			// 	}
			// }
			
			if (IsLocalNetworkRig && dripData.hidePartFromCamera)
			{
				dripManager.EnablePartFromCamera(dripData.HidePartFromCameraPartType, false);
			}
		}
	}

	[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
	public void RPC_UnequipDrip(string dripId)
	{
		Debug.Log($"[RPC_UnequipDrip] dripId: {dripId}");
		var dripData = dripManager.dripDataContainer.Find(dripId);
		if (dripData == null)
		{
			Debug.Log($"[RPC_UnequipDrip] dripData is null for dripId: {dripId}");
			return;
		}
		
		if (IsLocalNetworkRig && dripData.pack == DripPack.Outfit)
		{
			isDripTransformation       = false;
		}

		dripManager.UnequipDrip(dripData, checkForRefundOutfit: true);

		UpdatePlayerNamePositionAndColor();

		if (_playerInventory.equipments.Contains(dripId))
		{
			_playerInventory.equipments.Remove(dripId);

			if (IsLocalNetworkRig)
			{
				RuntimeUserData.CacheUser.inventory.equipments = _playerInventory.equipments;
				RuntimeUserData.CacheUser.Save().Forget();

				AnalyticsHelper.RecordEquipDripItem(dripId, false);
			}

			UpdatePlayerNamePositionAndColor();
		}
		
		if (IsLocalNetworkRig && dripData.hidePartFromCamera)
		{
			dripManager.EnablePartFromCamera(dripData.HidePartFromCameraPartType, true);
		}
	}

	[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
	public async void RPC_EquipGear(GearEquipmentSlot gearSlot, string gearId, string gearInstanceId)
	{
		if (!_playerInventory.TryGetGearItemWithInstanceId(gearInstanceId, out var gearItem))
		{
			Debug.LogError($"[RPC_EquipGear] gearInstanceId: {gearInstanceId} not found in _playerInventory");
			return;
		}

		// Check if gearInstanceId is currently in any slot?
		if (gearManager.TryGetGearSlot(gearInstanceId, out var gearSlotData))
		{
			if (gearSlotData == gearSlot)
			{
				return;
			}

			gearManager.UnequipGear(gearSlotData, IsLocalNetworkRig);

			_playerInventory.gearEquipments[gearSlotData] = null;

			if (IsLocalNetworkRig)
				RuntimeUserData.CacheUser.inventory.gearEquipments = _playerInventory.gearEquipments;
		}

		// Remove currently equip slot
		gearManager.UnequipGear(gearSlot, IsLocalNetworkRig);

		var success = gearManager.EquipGear(gearSlot, gearItem, IsLocalNetworkRig);
		if (success)
		{
			_playerInventory.gearEquipments[gearSlot] = gearInstanceId;
			if (IsLocalNetworkRig)
			{
				RuntimeUserData.CacheUser.inventory.gearEquipments = _playerInventory.gearEquipments;
				AnalyticsHelper.RecordEquipGearItem(gearInstanceId, true);
			}

			UpdatePlayerNamePositionAndColor();
		}
	}

	[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
	public void RPC_UnequipGear(GearEquipmentSlot gearSlot)
	{
		var existingGearItem = gearManager.GetGearSlotData(gearSlot);
		if (existingGearItem == null)
		{
			Debug.Log($"[RPC_UnequipGear] slot {gearSlot} is empty. Cannot unequip gear.");
			return;
		}

		if (!_playerInventory.TryGetGearItemWithInstanceId(existingGearItem.instanceId, out var gearItem))
		{
			return;
		}

		var unequipSuccess = gearManager.UnequipGear(gearSlot, IsLocalNetworkRig);
		if (unequipSuccess)
		{
			if (_playerInventory.gearEquipments.ContainsKey(gearSlot))
			{
				_playerInventory.gearEquipments[gearSlot] = null;

				if (IsLocalNetworkRig)
				{
					RuntimeUserData.CacheUser.inventory.gearEquipments = _playerInventory.gearEquipments;

					if (!string.IsNullOrEmpty(existingGearItem.instanceId))
					{
						AnalyticsHelper.RecordEquipGearItem(existingGearItem.instanceId, false);
					}
				}
			}

			UpdatePlayerNamePositionAndColor();
		}
	}

	public async void RequestRmotePlayerInventory(string playfabId)
	{
		if (_isSyncedPlayerData == true)
		{
			return;
		}

		Debug.Log($"RequestRmotePlayerInventory {playfabId}");

		var result = await PlayFabClientAPIExtensions.GetPlayerInfo(playfabId);
		if (result == null)
		{
			Debug.LogError($"[RequestRmotePlayerInventory] result inventory is null.");
			return;
		}

		_playerInventory = result;

		_isSyncedPlayerData = true;

		InitializeDrip();
		InitializeGear();

		UpdatePlayerNamePositionAndColor();

		//Check if Invisible Or Monster
		if (isInvisible || isMonster)
		{
			playerController.playerShooting.ActiveWeapons(false);
			playerController.playerShooting.ActiveDrone(false);
			playerController.playerShooting.ActiveArmors(false);

			dripManager.SetVisibleAndMonster(!isInvisible, isMonster, IsLocalNetworkRig);

			playerNameText.gameObject.SetActive(false);

			// Scaling blood jman for the user who joined later
			playerController.transform.localScale = isMonster ? Vector3.one * BloodJmanTransform.SCALE_SIZE : Vector3.one;
		}

		//Check if Freeze
		if (isFreeze)
		{
			dripManager.SetFreeze(isFreeze, IsLocalNetworkRig);
		}
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	private void RPC_StartPee()
	{
		if (IsPeeing)
			return;
		IsPeeing = true;

		if (peeEffect)
			peeEffect.Play();
		if (peeAudio != null && !peeAudio.isPlaying) peeAudio.Play();
	}


	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	private void RPC_StopPee()
	{
		if (!IsPeeing)
			return;
		IsPeeing = false;

		if (peeEffect)
			peeEffect.Stop();
		if (peeAudio != null && peeAudio.isPlaying) peeAudio.Stop();
	}


	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_PushPlayerBackwards()
	{
		if (HasStateAuthority)
			playerController.transform
							.DOMove(playerController.transform.position + playerController.transform.forward * -2, .2f);
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	private void RPC_OnChangedPlayerName(string newName)
	{
		Debug.Log($"[RPC_OnChangedPlayerName] PlayerRef.PlayerId {PlayerRef.PlayerId} newName {newName}");
		foreach (var player in ShmackleGameManager.Instance.shmacklePlayerList)
			if (PlayerRef.PlayerId == player.playerId)
			{
				player.playerName = newName;

				if (player.networkPlayer != null)
				{
					player.networkPlayer.DisplayName = newName;

					if (player.networkPlayer.IsLocalNetworkRig)
					{
						player.networkPlayer.networkPlayerName = newName;
					}
				}

				playerNameText.SetText(newName);
			}
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	private void RPC_OnRequestSyncPlayerName(int playerId)
	{
		Debug.Log($"[RPC_OnRequestSyncPlayerName] playerId {playerId}");

		
		foreach (var player in ShmackleGameManager.Instance.shmacklePlayerList)
		{
			if (playerId == player.playerId && HasStateAuthority)
			{
				OnChangedPlayerName(player.playerName);
				RegisterPlayfabsId(playerId, Runner.UserId);
			}
		}
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	private void RPC_RegisterPlayfabsId(int playerId, string id)
	{
		foreach (var player in ShmackleGameManager.Instance.shmacklePlayerList)
		{
			if (playerId == player.playerId)
			{
				player.playfabsId = id;
			}
		}
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	private void RPC_GameMaster(int playerId)
	{
		foreach (var player in ShmackleGameManager.Instance.shmacklePlayerList)
			if (playerId == player.playerId)
				player.isGameMaster = true;
		ShmackleGameManager.Instance.onGameMasterJoined.Invoke(ShmackleGameManager.Instance.IsHaveGameMaster);
	}

	// SOUNd BOARD
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_SoundBoardInvisible()
	{
		Debug.Log($"[ShmackleNetworkRig] RPC_SoundBoardInvisible");
		SetVisibleLocally(false);
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_SoundBoardVisible()
	{
		Debug.Log($"[ShmackleNetworkRig] RPC_SoundBoardVisible");

		SetVisibleLocally(true);
	}
	
	public void SetVisibleLocally(bool isVisible)
	{
		if (isVisible)
		{
			bool _isCombatMode = playerController.playerShooting.IsCombatActive;
			playerController.playerShooting.ActiveWeapons(!isMonster && _isCombatMode);
			playerController.playerShooting.ActiveDrone(!isMonster && _isCombatMode);
			playerController.playerShooting.ActiveArmors(!isMonster && _isCombatMode);
			dripManager.SetVisibleAndMonster(true, isMonster, IsLocalNetworkRig);
			playerNameText.gameObject.SetActive(!isMonster);
		}
		else
		{
			playerController.playerShooting.ActiveWeapons(false);
			playerController.playerShooting.ActiveDrone(false);
			playerController.playerShooting.ActiveArmors(false);
			dripManager.SetVisibleAndMonster(false, isMonster, IsLocalNetworkRig);
			playerNameText.gameObject.SetActive(false);
		}

		if (IsLocalNetworkRig)
		{
			isInvisible = !isVisible;
		}
	}

	public void SetMonsterLocally(bool isMonsterLocal)
	{
		if (isMonsterLocal)
		{
			playerController.playerShooting.ActiveWeapons(false);
			playerController.playerShooting.ActiveDrone(false);
			playerController.playerShooting.ActiveArmors(false);
			playerController.kissController.SetActiveEnabled(false);

			dripManager.SetVisibleAndMonster(isVisible: !isInvisible, true, IsLocalNetworkRig);

			playerNameText.gameObject.SetActive(false);
		}
		else
		{
			bool _isCombatMode = playerController.playerShooting.IsCombatActive;
			playerController.playerShooting.ActiveWeapons(!isInvisible && _isCombatMode);
			playerController.playerShooting.ActiveDrone(!isInvisible   && _isCombatMode);
			playerController.playerShooting.ActiveArmors(!isInvisible  && _isCombatMode);
			playerController.kissController.SetActiveEnabled(true);

			dripManager.SetVisibleAndMonster(isVisible: !isInvisible, false, IsLocalNetworkRig);

			playerNameText.gameObject.SetActive(!isInvisible);
		}

		if (IsLocalNetworkRig)
		{
			isMonster = isMonsterLocal;
		}
		
		onChangedMonster?.Invoke(isMonsterLocal);
	}
	

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_SoundBoardMonster()
	{
		Debug.Log($"[ShmackleNetworkRig] RPC_SoundBoardMonster");
		SetMonsterLocally(true);
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_SoundBoardUnmonster()
	{
		Debug.Log($"[ShmackleNetworkRig] RPC_SoundBoardUnmonster");
		SetMonsterLocally(false);
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_SoundBoardHideSagaJohnToe()
	{
		Debug.Log($"[ShmackleNetworkRig] RPC_SoundBoardHideSagaJohnToe");

		var monsters = FindObjectsByType<FinderController>(FindObjectsSortMode.None);
		for (int i = 0; i < monsters.Length; i++)
		{
			monsters[i].gameObject.SetActive(false);
		}

		foreach (var player in ShmackleGameManager.Instance.shmacklePlayerList)
		{
			var ragdoll = player.networkPlayer.GetComponent<RagdollController>();
			if (ragdoll != null)
			{
				ragdoll.DisableSound();
			}
		}
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_SoundBoardPlaySound(SoundBoardKey soundKey)
	{
		Debug.Log($"[ShmackleNetworkRig] RPC_SoundBoardPlaySound {soundKey}");
		SoundManager.Instance.PlaySoundBoard(soundKey);
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_RequestChangeBloodJmanCountdown(int time)
	{
		RPC_ChangeBloodJmanCountdown(time);
	}


	[Rpc(RpcSources.All, RpcTargets.All)]
	private void RPC_ChangeBloodJmanCountdown(int time)
	{
		BloodJmanGameManager.Instance.ReadyDuration = time;
		switch (time)
		{
			case 6:
				BloodJmanGameManager.Instance.CountdownAudio = BloodJmanGameManager.Instance.fiveSecondsCountdownAudio;
				break;
			case 11:
				BloodJmanGameManager.Instance.CountdownAudio = BloodJmanGameManager.Instance.tenSecondsCountdownAudio;
				break;
			case 16:
				BloodJmanGameManager.Instance.CountdownAudio = BloodJmanGameManager.Instance.tenSecondsCountdownAudio;
				break;
		}
	}


	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_RequestChangeBloodJmanGameDuration(int time)
	{
		RPC_ChangeBloodJmanGameDuration(time);
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	private void RPC_ChangeBloodJmanGameDuration(int time)
	{
		BloodJmanGameManager.Instance.GameDuration = time;
		switch (time)
		{
			case 120:
				BloodJmanGameManager.Instance.AmbientAs.clip = BloodJmanGameManager.Instance.twoMinutesMusic;
				break;
			case 180:
				BloodJmanGameManager.Instance.AmbientAs.clip = BloodJmanGameManager.Instance.threeMinutesMusic;
				break;
			case 300:
				BloodJmanGameManager.Instance.AmbientAs.clip = BloodJmanGameManager.Instance.fiveMinutesMusic;
				break;
		}
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void RPC_RequestRematchBloodJman()
	{
		RPC_RematchBloodJman();
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	private void RPC_RematchBloodJman()
	{
		BloodJmanGameManager.Instance.Rpc_ForceRematch();
	}
	
	//======== END SOUNDBOARD ========//
	
	//==== Player Mobile ========//
	public void GrabPhoneLeft()
	{
		if (IsLocalNetworkRig)
		{
			RPC_GrabPhoneLeft();
		}
	}
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_GrabPhoneLeft()
	{
		if (!IsLocalNetworkRig)
		{
			playerMobile.gameObject.SetActive(true);
			phoneAnimator.SetTrigger("isOpenL");
			playerMobile.transform.parent = autoHandLeft.transform;
			playerMobile.transform.rotation = playerController.playerMobileController.leftHandPhonePos.transform.rotation;
			playerMobile.transform.position = playerController.playerMobileController.leftHandPhonePos.transform.position;
			
			//Hand hand = autoHandLeft.GetComponent<Hand>();
			//hand.TryGrab(leftHandleGrabbable);
		}
	}

	public void GrabPhoneRight()
	{
		if (IsLocalNetworkRig)
		{
			RPC_GrabPhoneRight();
		}
	}
	
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_GrabPhoneRight()
	{
		if (!IsLocalNetworkRig)
		{
			playerMobile.gameObject.SetActive(true);
			phoneAnimator.SetTrigger("isOpenR");
			playerMobile.transform.parent = autoHandRight.transform;
			playerMobile.transform.rotation = playerController.playerMobileController.rightHandPhonePos.transform.rotation;
			playerMobile.transform.position = playerController.playerMobileController.rightHandPhonePos.transform.position;
			
			//Hand hand = autoHandLeft.GetComponent<Hand>();
			//hand.TryGrab(rightHandleGrabbable);
		}
		
	}

	public void ReleasePhone()
	{
		if (IsLocalNetworkRig)
		{
			RPC_ReleasePhone();
		}
	}
	
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_ReleasePhone()
	{
		if (!IsLocalNetworkRig)
		{
			playerMobile.transform.parent = null;
		}
	}
	
	public void ShutDownPhone()
	{
		if (IsLocalNetworkRig)
		{
			RPC_ShutDownPhone();
		}
	}
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_ShutDownPhone()
	{
		if (!IsLocalNetworkRig)
		{
			playerMobile.transform.parent = null;
			playerMobile.gameObject.SetActive(false);
		}
	}
	
	//==== End Player Mobile =====//

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_ApplyDripTransformation(bool isTransform, bool instantly = false)
	{
		if (dripManager.dripTransformData == null)
		{
			Debug.LogError($"Drip Transform data is null!");
			return;
		}
		
		Debug.Log($"[DripManager] ApplyTransformation isTransform {isTransform} instantly {instantly}");
		
		isDripTransformation       = isTransform;
		dripManager.ApplyDripTransformation(isTransform, instantly);
		
		UpdatePlayerNamePositionAndColor();
	}
	#endregion

	#region Player Party
	[Networked]public bool isInParty { get; set; }
	
	public void SyncPartyTo(string targetID)
	{
		// Convert list to JSON
		string json = JsonUtility.ToJson(new PartyWrapper { players = ShmackleGameManager.Instance.playerInParty });
		RPC_ReceiveParty(targetID, json);
	}
	
	
	
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	private void RPC_ReceiveParty(string targetID ,string json, RpcInfo info = default)
	{
		if (targetID == Runner.UserId ||
		    ShmackleGameManager.Instance.playerInParty.Any(p => p.playfabID == Runner.UserId))
		{
			var wrapper = JsonUtility.FromJson<PartyWrapper>(json);
			ShmackleGameManager.Instance.playerInParty = wrapper.players;
			ShmackleGameManager.Instance.checkPlayerInParty();

			DOVirtual.DelayedCall(0.5f, () =>
			{
				ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile.playerListController.leavePartyButton.SetActive(true);
				ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile.playerListController.friendListButton.SetActive(true);
				ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile.playerListController.UpdateData();
				ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile.playerListController.updateFriendListData();
			});
			
			
			Debug.Log("player in party " + ShmackleGameManager.Instance.playerInParty.Count);
			if (ShmackleGameManager.Instance.playerInParty.Count <= 1)
			{
				ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile.playerListController.leavePartyButton.SetActive(false);
				ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile.playerListController.friendListButton.SetActive(false);
				ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile.playerListController.scrollViewPartyMembers.SetActive(false);
				ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile.playerListController.scrollViewPlayers.SetActive(true);
			}
			
			
			if (Runner.UserId == targetID)
			{
				ShmackleGameManager.Instance.shmackleLocalPlayer.notificationPopUp.SetActive(true);
				ShmackleGameManager.Instance.shmackleLocalPlayer.notificationText.text = "you are now locked in";
				DOVirtual.DelayedCall(3, () =>
				{
					ShmackleGameManager.Instance.shmackleLocalPlayer.notificationPopUp.SetActive(false);
				});
			}
		}

		
	}


	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_PartyJoinLobby(string code)
	{
		if (ShmackleGameManager.Instance.playerInParty.Any(p => p.playfabID == Runner.UserId))
		{
			ShmackleGameManager.Instance.privateRoomCode = code;
			foreach (var player in ShmackleGameManager.Instance.playerInParty)
			{
				player.isChangeScene = true;
				player.roomID = code;
				player.sceneName = ShmackleConnectionManager.Instance._defaultLobby.SceneName + "#" + code;
			}
			ShmackleGameManager.Instance.ChangeRoom(ShmackleConnectionManager.Instance._defaultLobby, code);
		}
	}


	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_PartyChangeScene(string sceneName, string sceneCode)
	{
		if (ShmackleGameManager.Instance.playerInParty.Any(p => p.playfabID == Runner.UserId))
		{
			foreach (var player in ShmackleGameManager.Instance.playerInParty)
			{
				player.isChangeScene = true;
			}
			
			foreach (var connection in ShmackleGameManager.Instance.connectionDataList)
			{
				if (connection.SceneName == sceneName)
				{
					ShmackleGameManager.Instance.ChangeRoom(connection, sceneCode);
				}
			}
		}
	}
	#endregion 

	#region #----- BloodJman Abilities -----#

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void Rpc_BloodJmanAbilityFreeze(bool freeze)
	{
		Debug.Log($"[ShmackleNetworkRig] RPC_BloodJmanAbilityFreeze freeze {freeze}");
		SetFrozenLocally(freeze);
	}

	public void SetFrozenLocally(bool isFrozen)
	{
		if (IsLocalNetworkRig)
		{
			isFreeze = isFrozen;
			playerController.FreezePlayer(isFrozen, false);
			playerController.FreezeVirtualHand(isFrozen);
		}

		dripManager.SetFreeze(isFrozen, IsLocalNetworkRig);

		if (isFrozen)
		{
			MyPooler.ObjectPooler.Instance.GetFromPool("BloodJmanFreeze", transform);
		}
		
		if (playerHealth.BloodJmanPlayerRagdoll != null)
		{
			playerHealth.BloodJmanPlayerRagdoll.ReplicatePart(dripManager.GetPartController(), dripManager.GetSpineController());
		}
	}

	public void ResetBloodJmanAbility()
	{
		dripManager.GetPartController().bloodJmanTransform.StopCurrentRoutine();
	}

	public void ResetEffect()
	{
		Debug.Log($"[ShmackleNEtworkRig] ResetFreeze() {PlayerRef.PlayerId}");
		// if (IsLocalNetworkRig)
		// {
		// 	playerController.FreezePlayer(false);
		// }

		dripManager.ResetEffect();
		
		playerController.transform.localScale = Vector3.one;
	}
	#endregion

	#region #----- Methods ------#

	public void InitializeDrip()
	{
		if (_isInitializeDrip)
		{
			return;
		}

		Debug.Log($"[ShmackleNetworkRig] DripManager InitializeDrip {string.Join(", ", _playerInventory.equipments ?? new())}");

		// var equippedDripDatas = 
		// 	_playerInventory.equipments
		// 		.Select(x => dripManager.dripDataContainer.Find(x))
		// 		.Where(x => x != null)
		// 		.ToList();
		//
		// var dic = new Dictionary<DripPack, DripData>();
		// for (int i = equippedDripDatas.Count - 1; i >= 0; i--)
		// {
		// 	var dripData = equippedDripDatas[i];
		// 	dic[dripData.pack] = dripData;
		// }
		//
		// var equippedItems =
		// 	dic.Values
		// 	   .OrderByDescending(x => x.pack == DripPack.Outfit)
		// 	   .ToList();

		if (_playerInventory.equipments == null)
		{
			return;
		}

		foreach (var drip in _playerInventory.equipments)
		{
			var dripData = dripManager.dripDataContainer.Find(drip);
			dripManager.EquipDrip(dripData, false);
		}

		if (IsLocalNetworkRig && BloodJmanGameManager.Instance != null && BloodJmanGameManager.Instance.IsPlayerSpectator(PlayerRef.PlayerId))
		{
			dripManager.SetVisibleAndMonster(false, false, true, false);
		}

		if (IsLocalNetworkRig)
		{
			foreach (var drip in _playerInventory.equipments)
			{
				var dripData = dripManager.dripDataContainer.Find(drip);
				if (dripData != null)
				{
					if (DripUtils.IsGrabbableDrip(dripData))
					{
						foreach (var runtime in dripData.runtimeCollection)
						{
							if (runtime.AdditionalGrabbableItem != null)
							{
								SpawnAdditionalGrabbableItem(runtime);
							}
							else
							{
								Debug.LogError($"[ShmackleNetworkRig] InitializeDrip() dripPack {dripData.id} runtime {runtime.name} runtime.AdditionalGrabbableItem is null. ");
							}
						}
					}
					else
					{
						foreach (var runtime in dripData.runtimeCollection)
						{
							if (runtime.AdditionalGrabbableItem != null)
							{
								SpawnAdditionalGrabbableItem(runtime);
							}
						}
					}

					if (dripData.hidePartFromCamera)
					{
						dripManager.EnablePartFromCamera(dripData.HidePartFromCameraPartType, false);
					}
				}
			}
		}

		dripManager.RecalculateAndApplyBlendShape();

		if (dripManager.CanTransform && isDripTransformation)
		{
			dripManager.ApplyDripTransformation(true, true);
		}

		// If player is local. We set the layer of part Head and Face to HideFromCamera
		if (IsLocalNetworkRig)
		{
			var list = new List<(CharacterPartType partType, LayerMask layerMask)>()
					   {
							(CharacterPartType.Head, LayerMask.NameToLayer("HideFromPlayerCamera")),
							(CharacterPartType.Face, LayerMask.NameToLayer("HideFromPlayerCamera")),
							(CharacterPartType.Helmet, LayerMask.NameToLayer("HideFromPlayerCamera"))
					   };

			dripManager.SetPartLayers(list);
		}
		
		_isInitializeDrip = true;
	}

	public void InitializeGear()
	{
		if (_isInitializeGear)
		{
			return;
		}

		_isInitializeGear = true;

		if (_playerInventory.gearEquipments == null)
		{
			return;
		}

		var equippedItems = _playerInventory.gearEquipments;
		foreach (var kvp in equippedItems)
		{
			if (_playerInventory.TryGetGearItemWithInstanceId(kvp.Value, out var gearItem))
			{
				gearManager.EquipGear(kvp.Key, gearItem, IsLocalNetworkRig, false);
			}
			else
			{
				Debug.LogError($"[InitializeGear] No Gear Id for {kvp.Key} gearInstanceId {kvp.Value}.");
			}
		}
	}

	private async void RegisterChangeData()
	{
		if (dripManager != null)
		{
			dripManager.Initialize();
		}

		if (gearManager != null)
		{
			gearManager.Initialize();
		}

		await UniTask.WaitUntil(() => _isInitializeDrip && _isInitializeGear);
		UpdatePlayerNamePositionAndColor();
	}

	public void OnChangedPlayerName(string newName)
	{
		if (HasStateAuthority) RPC_OnChangedPlayerName(newName);
	}

	public void OnRequestSyncPlayerData(int playerId)
	{
		RPC_OnRequestSyncPlayerName(playerId);
	}

	public void RegisterPlayfabsId(int playerId, string id)
	{
		if (HasStateAuthority) RPC_RegisterPlayfabsId(playerId, id);
	}

	public void RegisterGameMaster(int playerId)
	{
		if (HasStateAuthority) RPC_GameMaster(playerId);
	}

	public void UpdatePlayerNamePositionAndColor()
	{
		var pos = _playerNameDefaultPosition;
		var color = _playerNameDefaultColor;

		var offsetMaxY = float.MaxValue;

		if (_playerInventory.equipments != null)
		{
			foreach (var equipment in _playerInventory.equipments)
			{
				var hasOverridePlayerName = false;
				var hasOverridePlayerColor = false;
				var dripData = dripManager.dripDataContainer.Find(equipment);
				foreach (var runtime in dripData.runtimeCollection)
				{
					if (runtime.overridePlayerNamePosition)
					{
						pos = runtime.playerNamePosition;

						if (hasOverridePlayerName == false)
						{
							hasOverridePlayerName = true;
						}
						else
						{
							Debug.LogError($"Already Override Player Name Position for {runtime.CharacterPartType}");
						}
					}

					if (runtime.overridePlayerNameColor)
					{
						color = runtime.playerNameColor;

						if (hasOverridePlayerColor == false)
						{
							hasOverridePlayerColor = true;
						}
						else
						{
							Debug.LogError($"Already Override Player Name Color for {runtime.CharacterPartType}");
						}
					}

					if (runtime.overridePlayerNameMaxOffsetY)
					{
						if (offsetMaxY > runtime.offsetMaxY)
						{
							offsetMaxY = runtime.offsetMaxY;
						}
					}

					if (runtime.DripDataTransformation != null && runtime.DripDataTransformation.overridePlayerNamePosition && isDripTransformation)
					{
						pos = runtime.DripDataTransformation.playerNamePosition;

						if (hasOverridePlayerName == false)
						{
							hasOverridePlayerName = true;
						}
					}
				}
			}
		}

		if (gearManager.isArmorEquiped)
		{
			pos.z = pos.z < _armorZOffset ? _armorZOffset : pos.z;
		}

		if (pos.y > offsetMaxY)
		{
			pos.y = offsetMaxY;
		}

		playerNameRectTransform.anchoredPosition3D = pos;
		playerNameText.color = color;
	}

	public void ApplyStun(float duration)
	{
		if (!HasStateAuthority)
			return;
		if (!_playerRigidbody)
			return;

		_playerRigidbody.useGravity = false;
		_playerRigidbody.isKinematic = true;
		Invoke(nameof(RemoveStun), duration);
	}

	private void RemoveStun()
	{
		if (!HasStateAuthority)
			return;
		_playerRigidbody.useGravity = true;
		_playerRigidbody.isKinematic = false;
	}

	public void SetPlayerGameModule(PlayerGameModule playerGameModule)
	{
		_playerGameModule = playerGameModule;
	}

	public PlayerGameModule GetPlayerGameModule()
	{
		return _playerGameModule;
	}

	private void OnDestroy()
	{
		if (PlayersManager.Instance)
			if (PlayersManager.Instance.Players.ContainsKey(PlayerRef.PlayerId))
				PlayersManager.Instance.RemovePlayer(PlayerRef.PlayerId);
	}

	public void MuteRemotePlayer()
	{
		if (voiceChatSpeaker != null)
		{
			Debug.Log("Mute Remote Player " + DisplayName);
			voiceChatSpeaker.enabled = false;
			voiceChatAudio.mute = true;
		}
		if (IsLocalNetworkRig && voiceChatRecorder != null)
		{
			voiceChatRecorder.enabled = false;
			voiceChatRecorder.RecordingEnabled = false;
		}
	}

	public void UnmuteRemotePlayer()
	{
		if (voiceChatSpeaker != null) 
		{
			voiceChatSpeaker.enabled = true;
			voiceChatAudio.mute = false;
		}
		if (IsLocalNetworkRig && voiceChatRecorder != null)
		{
			voiceChatRecorder.enabled = true;
			voiceChatRecorder.RecordingEnabled = true;
		}
	}



	public void StartPee()
	{
		if (HasStateAuthority)
			RPC_StartPee();
	}

	public void StopPee()
	{
		if (HasStateAuthority)
			RPC_StopPee();
	}

	public void LoadScene(ShmackleConnectionData sceneTarget, string roomCode)
	{
		ShmackleGameManager.Instance.ChangeRoom(sceneTarget, roomCode);
	}


	private AsyncOperation asyncLoad;

	private IEnumerator LoadSceneAsync(string sceneName)
	{
		// Start asynchronous loading of the scene
		asyncLoad = SceneManager.LoadSceneAsync(sceneName);

		if (asyncLoad == null)
		{
			Debug.LogError("Scene not found or could not be loaded!");
			yield break;
		}

		asyncLoad.allowSceneActivation = false;

		// Wait until the scene is loaded
		while (!asyncLoad.isDone)
		{
			Debug.Log($"Loading progress: {asyncLoad.progress * 100}%");

			if (asyncLoad.progress >= 0.9f)
			{
				if (HasStateAuthority)
					Runner.Despawn(ShmackleGameManager.Instance.shmackleLocalPlayer.GetComponent<NetworkObject>());

				// if (ShmackleGameManager.Instance.dontDestroyOnLoad)
				// {
				//     Destroy(ShmackleGameManager.Instance.gameObject);
				// }

				Destroy(ShmackleConnectionManager.Instance.gameObject);
				//
				// _= ShmackleConnectionManager.Instance.ShutdownRunner(
				//     ShmackleConnectionManager.Instance.GetLobbyConnection() , OnConnectedToServer);

				asyncLoad.allowSceneActivation = true;
				ShmackleConnectionManager.Instance.currentApp.GetComponent<ShmackleApp>().ShutdownLobbyRunner();
			}

			yield return null;
		}
	}


	private void OnConnectedToServer()
	{
		asyncLoad.allowSceneActivation = true; // Activate the scene
	}
	
	#endregion
	
	#region #----- DripData Additional Grabbable Item -----#
	
	public void SpawnAdditionalGrabbableItem(DripData_Runtime dripDataRuntime)
	{
		if (dripDataRuntime.AdditionalGrabbableItem == null)
		{
			return;
		}

		DespawnAdditionalGrabbableItem(dripDataRuntime);
            
		PlayerMobile playerMobile = ShmackleGameManager.Instance.shmackleLocalPlayer.playerMobile;

		var       item          = ShmackleGameManager.Instance._runner.Spawn(dripDataRuntime.AdditionalGrabbableItem);
		Transform itemTransform = item.GetComponent<Transform>();

		var grabble = itemTransform.GetComponent<Grabbable>();

		if (playerMobile.isHandRightHoldPhone)
		{
			itemTransform.position = playerMobile.mobileController.leftHandPhonePos.transform.position;
			playerMobile.mobileController.playerController.autoHandLeft.TryGrab(grabble);
            
			_addditionalGrabableItems.Add(dripDataRuntime.AdditionalGrabbableItem.name, item);
		}
		else
		{
			itemTransform.position = playerMobile.mobileController.rightHandPhonePos.transform.position;
			playerMobile.mobileController.playerController.autoHandRight.TryGrab(grabble);
            
			_addditionalGrabableItems.Add(dripDataRuntime.AdditionalGrabbableItem.name, item);
		}
		

	}

	public void DespawnAdditionalGrabbableItem(DripData_Runtime dripDataRuntime)
	{
		if (dripDataRuntime.AdditionalGrabbableItem == null)
		{
			return;
		}
            
		if (_addditionalGrabableItems.ContainsKey(dripDataRuntime.AdditionalGrabbableItem.name) == false)
		{
			return;
		}

		var item = _addditionalGrabableItems[dripDataRuntime.AdditionalGrabbableItem.name];
		_addditionalGrabableItems.Remove(dripDataRuntime.AdditionalGrabbableItem.name);

		if (item != null)
		{
			ShmackleGameManager.Instance._runner.Despawn(item);
		}
	}
	
	#endregion
	
	#region #----- Drip Transformation -----#

	public void CheckDripTransformationInput(PlayerInputListener playerInputListener, bool isForce = false)
	{
		if (!dripManager.CanTransform)
		{
			return;
		}
		
		var transformData = dripManager.dripTransformData?.transformData;
		if (transformData == null)
		{
			return;
		}
		
		if (transformData.transformInputType == DripTransformInputType.AllButton)
		{
			if (isForce || (playerInputListener.leftGripState             == PlayerInputListener.ButtonState.Holding
			            && playerInputListener.leftTriggerState       == PlayerInputListener.ButtonState.Holding
			            && playerInputListener.leftPrimaryButtonState == PlayerInputListener.ButtonState.Holding)
			    )
			{
				RPC_ApplyDripTransformation(!isDripTransformation);
			}
		}
	}
	
	#endregion
	
	#region #----- Prop -----#
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_EnablePropController(bool isEnable)
	{
		var propController = this.playerController.GetComponent<ShmacklePropController>();
		if (propController == null)
		{
			propController = this.playerController.AddComponent<ShmacklePropController>();
			
			var prefab = Resources.Load<PropRenderer>("PropRenderer");
			var cameraPrefab = Resources.Load<GameObject>("PropCamera");
			propController.Setup(this, prefab, cameraPrefab.transform);
			
		}
		
		propController.EnablePropController(isEnable);
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public void RPC_TransformToProp(string propId)
	{
		var propController = this.playerController.GetComponent<ShmacklePropController>();
		if (propController == null)
		{
			propController = this.playerController.AddComponent<ShmacklePropController>();
			
			var prefab       = Resources.Load<PropRenderer>("PropRenderer");
			var cameraPrefab = Resources.Load<GameObject>("PropCamera");
			propController.Setup(this, prefab, cameraPrefab.transform);
			
		}
		
		propController.HandleTransformToProp(propId);
	}
	#endregion
	
	#region DEBUG
	[Sirenix.OdinInspector.Button("Debug Freeze")]
	public void DebugFreeze(bool isFreeze)
	{
		Rpc_BloodJmanAbilityFreeze(isFreeze);
	}
	
	[Sirenix.OdinInspector.Button("Debug Equip Zombro")]
	public void DebugEquipZombro()
	{
		RPC_EquipDrip("FBD_Zombro");
	}
	
	[Sirenix.OdinInspector.Button("Debug Drip Transformation")]
	public void DebugDripTransformation(bool isFreeze)
	{
		if (CanDripTransformation)
		{
			CheckDripTransformationInput(null, true);
		}
	}

	[ContextMenu("Debug Set Display Name")]
	private void debugSetDisplayName()
	{
		PlayFabClientAPIExtensions.setDisplayName("jmancurly", msg => { Debug.Log($"Server message: {msg}"); });
	}

	#endregion
	
}
// Wrapper needed because JsonUtility can’t handle raw List<>
[System.Serializable]
public class PartyWrapper
{
	public List<PlayerInParty> players;
}