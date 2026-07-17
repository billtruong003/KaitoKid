using System;
using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;

namespace GorillaLocomotion
{
    public class Splasher : Floater
    {
        public float size = 1;
        new Collider collider;
        //DateTime lastSplash;
        float lastSplash = 0;
        bool first;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        private new void Awake()
        {
            base.Awake();
            collider = GetComponent<Collider>();
        }

        public new void Update()
        {
            base.Update();

            var rig = LocalHardwareRig;
            if(rig != null)
            {
                if (Vector3.Distance(transform.position, rig.Headset.Position) > 20)
                    return;

                if (Time.time - lastSplash < 0.5f)
                    return;

        foreach (Water water in Water.Waters)
        {
            if (water.gameObject.activeSelf)
            {
                if (TouchingSurface(water, collider))
                {
                    //Debug.Log("Touching water: " + water.name);
                    Splash(water);
                    return;
                }
            }
        }

                first = true;
            }
        }

        public bool TouchingSurface(Water water)
        {
            return water.IsInWater(transform.position) && transform.position.y > water.transform.position.y - 0.1f;
        }

        public bool TouchingSurface(Water water, Collider targetCollider)
        {
            if (targetCollider == null)
                return TouchingSurface(water);

            Vector3 targetPoint = water.transform.position;
            targetPoint.x = targetCollider.transform.position.x;
            targetPoint.z = targetCollider.transform.position.z;

            return Vector3.Distance(targetCollider.bounds.ClosestPoint(targetPoint), targetPoint) < 0.1f && water.IsInWater(targetCollider);
        }

        public void Splash(Water water)
        {
            var rig = LocalHardwareRig;
            if (rig == null || Vector3.Distance(transform.position, rig.Headset.Position) > 20)
                return;

            // TODO: SWIMMING INSTANCE HAS REMOVED, UPDATE OTHER
            // GameObject go = PoolObject.Get(Swimming.instance.splash.gameObject, new Vector3(transform.position.x, water.transform.position.y + 0.01f, transform.position.z), Quaternion.Euler(90, 0, 0));
            // Splash splash = go.GetComponent<Splash>();
            // splash.audio.enabled = first;
            // splash.transform.localScale = Vector3.one * 0.1f * size;
            // splash.water = water;
            // splash.Initialised();

            first = false;
            lastSplash = Time.time;
        }
    }
}
