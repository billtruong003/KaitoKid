using UnityEngine;

namespace Shmackle.Player
{
    public interface IPushConfig
    {
        PushConfig PushConfig => default;
    }
    
    [System.Serializable]
    public struct PushConfig
    {
        [SerializeField]
        private float[] _pushForcePerCharge;
        [SerializeField]
        private float _maxChargeTime;
        [SerializeField, Tooltip("Multiplier for the jump force when the player is charging.")]
        private float _chargeJumpMultiplier;
        [SerializeField]
        private float _cooldown;
        
        public float[] PushForcePerCharge => _pushForcePerCharge;
        public float MaxChargeTime => _maxChargeTime;
        public float Cooldown => _cooldown;
        public float ChargeJumpMultiplier => _chargeJumpMultiplier;

        public PushConfig(float[] pushForcePerCharge, float maxChargeTime, float cooldown, float chargeJumpMultiplier)
        {
            _pushForcePerCharge = pushForcePerCharge;
            _maxChargeTime = maxChargeTime;
            _cooldown = cooldown;
            _chargeJumpMultiplier = chargeJumpMultiplier;
        }
    }
}