using UnityEngine;
using GameSystem.Interaction;

namespace GameSystem.Player
{
    [System.Serializable]
    public class PlayerInteraction
    {
        [SerializeField] private float interactionRange = 3f; [SerializeField] private LayerMask interactableMask;
        [SerializeField] private CanvasGroup reticleCanvasGroup;
        [SerializeField] private float reticleTransitionSpeed = 8f;

        private Transform cameraTransform;
        private IInteractable currentTarget;

        public void Initialize(Transform camTransform)
        {
            cameraTransform = camTransform;
            reticleCanvasGroup.alpha = 0.3f;
        }

        public void CheckInteraction()
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableMask))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    currentTarget = interactable;
                    reticleCanvasGroup.alpha = Mathf.Lerp(reticleCanvasGroup.alpha, 1f, Time.deltaTime * reticleTransitionSpeed);

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        currentTarget.Interact();
                    }
                    return;
                }
            }

            currentTarget = null;
            reticleCanvasGroup.alpha = Mathf.Lerp(reticleCanvasGroup.alpha, 0.3f, Time.deltaTime * reticleTransitionSpeed);
        }
    }
}