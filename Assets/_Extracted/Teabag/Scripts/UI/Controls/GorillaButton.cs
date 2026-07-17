using Teabag.Player;
using System;
using System.Collections;
using Squido.JungleXRKit.Core;
using UnityEngine;
using UnityEngine.Events;
using Teabag.Core;
using TMPro;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.UI
{
    public abstract class GorillaButton : MonoBehaviour
    {
        public new Renderer renderer;
        public Material invalidMaterial;
        public Material defaultMaterial;
        public Material pressedMaterial;
        public AdvancedAudioClip pressClip;
        public UnityEvent onClick;
        [NonSerialized]
        public bool visual = true;
        [NonSerialized]
        public float delay = 0.25f;
        //[NonSerialized]
        bool _interactable = true;
        public bool interactable
        {
            get
            {
                return _interactable;
            }
            set
            {
                _interactable = value;
                SetMaterial(false);
            }
        }
        bool canPress = true;
        public bool awaitingDelay
        {
            get
            {
                return canPress;
            }
        }

        Grabbable grabbable;

        public virtual void Awake()
        {
            if (renderer == null)
                renderer = GetComponent<Renderer>();

            foreach (Transform t in GetComponentInChildren<Transform>(true))
                t.gameObject.layer = 15;

            grabbable = GetComponentInParent<Grabbable>();
        }

        [ContextMenu("Press")]
        public bool Press()
        {
            if (!isActiveAndEnabled) return false;

            if (grabbable)
            {
                // If the object is holstered
                if (grabbable.grabber && !grabbable.grabber.hand)
                    return false;
            }

            if (canPress && interactable)
            {
                OnPress();
                canPress = false;
                if (gameObject.activeInHierarchy)
                    StartCoroutine(Wait());
                if (pressClip != null)
                {
                    var audioService = ServiceLocator.Get<IAudioService>();
                    audioService.Play(pressClip, transform.position);
                }

                //PopupManager.Display("Press", transform.position);

                if (onClick != null)
                    onClick.Invoke();

                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnEnable()
        {
            canPress = false;
            if (visual)
                SetMaterial(false);
            StartCoroutine(Wait(true));
            if (visual)
                SetMaterial(false);
        }

        IEnumerator Wait(bool isFirst = false)
        {
            if (visual && !isFirst)
                SetMaterial(true);
            yield return new WaitForSeconds(delay);
            canPress = true;
            if (visual && !isFirst)
                SetMaterial(false);
        }

        public void SetMaterial(bool pressed)
        {
            if (renderer == null)
                renderer = GetComponent<Renderer>();

            if (invalidMaterial != null && !interactable)
            {
                renderer.sharedMaterial = invalidMaterial;
                return;
            }

            if (defaultMaterial == null || pressedMaterial == null)
                return;

            //Renderer renderer = GetComponent<Renderer>();

            if (renderer == null)
                return;

            if (!pressed)
                renderer.sharedMaterial = defaultMaterial;
            else
                renderer.sharedMaterial = pressedMaterial;
        }

        public abstract void OnPress();
    }
}
