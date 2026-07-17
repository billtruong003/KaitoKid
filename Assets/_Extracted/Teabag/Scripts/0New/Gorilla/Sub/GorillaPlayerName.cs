using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;

namespace Teabag.Player
{
    public class GorillaPlayerName : MonoBehaviour
    {
        [SerializeField] bool isLookCamera = false;
        [SerializeField] Transform nameParent;
        Transform _camera;
        public TMP_Text text;
        CosmeticSetter setter;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        private void Awake()
        {
            setter = GetComponentInParent<CosmeticSetter>();
        }

        private void Start()
        {
            if (isLookCamera)
            {
                var cam = Camera.main;
                if (cam != null) _camera = cam.transform;
            }
        }

        private void Update()
        {
            var rig = LocalHardwareRig;
            if (rig == null) return;
            if (Vector3.Distance(transform.position, rig.Headset.Position) > 16)
                return;
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            text.transform.localPosition = new Vector3(0, 0, setter.GetNameOffset());
        }

        private void LateUpdate()
        {
            if (!isLookCamera || _camera == null)
            {
                return;
            }
            nameParent.LookAt(_camera);
        }
    }
}
