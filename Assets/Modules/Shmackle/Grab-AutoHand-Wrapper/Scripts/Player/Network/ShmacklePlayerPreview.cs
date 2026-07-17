using Autohand;
using TMPro;
using UnityEngine;

public class ShmacklePlayerPreview : MonoBehaviour
{
    [SerializeField]
    private Transform _leftController;
    [SerializeField]
    private Transform _rightController;
    [SerializeField]
    private Transform _leftHandFollower;
    [SerializeField]
    private Transform _rightHandFollower;
    [SerializeField]
    private Hand _autoLeftHand;
    [SerializeField]
    private Hand _autoRightHand;
    [SerializeField]
    private Rigidbody _autoHandLeftRigidbody;
    [SerializeField]
    private Rigidbody _autoHandRightRigidbody;
    [Space]
    [SerializeField]
    private Transform _characterIK;
    [SerializeField]
    private Transform _networkRig;
    [SerializeField] private Transform _vendingMachineCenter;
    [Space]
    [SerializeField] private TMP_Text _playerName;
    [Space]
    [SerializeField] private float _angleForceSpeed = 18f;
    [SerializeField]
    private float _lerpSpeed = 15f; // Speed for smooth position interpolation
    
    private ShmacklePlayerController _player;
    private ShmackleNetworkRig       _playerNetworkRig;
    private float                    _angleForce;
    private float                    _initialForce;
    private float                    _initalAngle;
    
    private void Update()
    {
        if (_player == null)
        {
            return;
        }
        
        // Position
        _leftController.localPosition   = _player.LeftController.transform.localPosition;
        _rightController.localPosition = _player.RightController.transform.localPosition;
        
        _autoLeftHand.transform.localPosition = _player.autoHandLeft.transform.localPosition;
        _autoRightHand.transform.localPosition = _player.autoHandRight.transform.localPosition;
        
        _leftHandFollower.localPosition  = _player.leftHandPosition.localPosition;
        _rightHandFollower.localPosition = _player.rightHandPosition.localPosition;
        
        // Rotation
        _leftController.localRotation = _player.LeftController.transform.localRotation;
        _rightController.localRotation = _player.RightController.transform.localRotation;
        
        _autoLeftHand.transform.localRotation = _player.autoHandLeft.transform.localRotation;
        _autoRightHand.transform.localRotation = _player.autoHandRight.transform.localRotation;
        
        _leftHandFollower.localRotation  = _player.leftHandPosition.localRotation;
        _rightHandFollower.localRotation = _player.rightHandPosition.localRotation;
        
        // Body
        _networkRig.position       = this.transform.position;
        _networkRig.rotation       = this.transform.rotation;
        
        _characterIK.localPosition = _playerNetworkRig.characterIK.transform.localPosition;
        _characterIK.localRotation = _playerNetworkRig.characterIK.transform.localRotation;
        
        _autoLeftHand.enableMovement  = false;
        _autoRightHand.enableMovement = false;

        _autoHandRightRigidbody.isKinematic = true;
        _autoHandLeftRigidbody.isKinematic  = true;
        
        // Use world position of characterIK to recenter preview
        Vector3 ikWorldPos = _characterIK.position;
        Vector3 worldOffset = ikWorldPos - transform.position;
        // Smoothly interpolate position towards the target
        Vector3 targetPosition = _vendingMachineCenter.position - worldOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, _lerpSpeed * Time.deltaTime);

        // Rotation: model yaw follows camera yaw relative to looking straight at machine center
        // 1) Compute yaw of the direction from camera to machine center (perfect look)
        Vector3 camToCenter = _vendingMachineCenter.position - _player.HeadCamera.transform.position;
        camToCenter.y = 0f;
        camToCenter.Normalize();
        float lookCenterYaw = Mathf.Atan2(camToCenter.x, camToCenter.z) * Mathf.Rad2Deg;

        // 2) Camera world yaw
        float cameraYaw = _player.HeadCamera.transform.eulerAngles.y;

        // 3) How far camera has deviated from looking straight at center
        float yawDelta = Mathf.DeltaAngle(lookCenterYaw, cameraYaw);

        // 4) Baseline model yaw so it faces outward from machine
        Vector3 machineForward = _vendingMachineCenter.forward;
        machineForward.y = 0f;
        machineForward.Normalize();
        float baseYaw = Mathf.Atan2(machineForward.x, machineForward.z) * Mathf.Rad2Deg + 180f;

        // 5) Apply the same yawDelta (inverted to face outward)
        // Compensate 90° left offset so forward lines up correctly
        var compensateAngle = 90f;
        #if UNITY_EDITOR
        compensateAngle = -180f;
        #endif
        transform.rotation = Quaternion.Euler(0f, baseYaw + yawDelta + compensateAngle + _angleForce, 0f);
    }

    public void SetPlayer(ShmackleNetworkRig player)
    {
        _playerNetworkRig = player;
        _player = player.playerController;
        _playerName.SetText(_playerNetworkRig.playerNameText.text);
    }

    public void OnStartRoll(float progressValue)
    {
        _initalAngle  = progressValue;
        _initialForce = _angleForce;
    }

    public void SetAngleForce(float progressValue)
    {
        _angleForce = _initialForce - (progressValue - _initalAngle) * _angleForceSpeed;
    }
}