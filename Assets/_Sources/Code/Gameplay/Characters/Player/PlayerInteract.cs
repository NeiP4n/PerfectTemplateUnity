using UnityEngine;
using System;
using Sources.Code.Interfaces;

namespace Sources.Code.Gameplay.Interaction
{
    public class PlayerInteract : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private LayerMask interactMask;

        private OutlineObject currentOutline;

        private IInputManager input;
        private IInteractable current;

        public event Action<IInteractable> OnFocusChanged;

        public void Construct(IInputManager inputManager)
        {
            input = inputManager;
        }

        public void UpdateInteract()
        {
            if (input == null)
                return;

            UpdateDetection();

            if (input.ConsumeInteract() && current != null && current.CanInteract)
            {
                if (current is IInteractableContext context)
                    context.Interact(this);
                else
                    current.Interact(); 
            }
        }

        private void UpdateDetection()
        {
            Ray ray = new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

            IInteractable detected = null;

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
            {
                detected = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (detected != current)
            {
                if (currentOutline != null)
                    currentOutline.DisableOutline();

                current = detected;
                currentOutline = null;

                if (current != null)
                {
                    currentOutline = (current as Component)
                        .GetComponentInParent<OutlineObject>();

                    if (currentOutline != null)
                        currentOutline.EnableOutline();
                }

                OnFocusChanged?.Invoke(current);
            }
        }
    }
}
