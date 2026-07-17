using Teabag.Player.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Teabag.Player;

namespace Teabag.Player.Cosmetics
{
    public class InteractiveCosmetic : MonoBehaviour
    {
        public Gorilla gorilla
        {
            get
            {
                return GetComponentInParent<Gorilla>();
            }
        }
        [NonSerialized] public bool preview = false;
        public bool overrideDefault = true;
        public float nameOffset = 0;
        public float healthOffset = 0;
        public List<TMP_Text> names = new List<TMP_Text>();
        public bool isSpeaking
        {
            get
            {
                VRRigMouthMovement movement = transform.root.GetComponentInChildren<VRRigMouthMovement>();

                if (movement == null)
                    return false;

                return movement.isSpeaking;
                //return false;
                //return rig.mouth.isSpeaking;
            }
        }

        private void Awake()
        {
            try
            {
                Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public virtual void Refresh()
        {
            foreach (TMP_Text text in names)
            {
                text.text = gorilla != null ? gorilla.playerName : Teabag.Core.PlayerData.displayName;
            }
        }
    }
}
