using UnityEngine;
using UnityEngine.InputSystem;
using Sources.Code.Core.Singletones;
using Sources.Code.Interfaces;

namespace Sources.Managers
{
    public class InputManager : SingletonBehaviour<InputManager>, IInputManager
    {
        private int _lockCount;
        public bool IsLocked => _lockCount > 0;
        public void Lock() => _lockCount++;
        public void Unlock() => _lockCount = Mathf.Max(0, _lockCount - 1);


        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }

        public bool JumpPressed { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool ThrowPressed { get; private set; }
        public bool SprintPressed { get; private set; }
        public bool RagdollPressed { get; private set; }

        public bool LeftClickPressed { get; private set; }
        public bool RightClickPressed { get; private set; }
        public bool CancelPressed { get; private set; }
        public bool DropPressed { get; private set; }

        // Inventory Input
        public int SlotIndexPressed { get; private set; } 
        public float ScrollDelta { get; private set; }


        public void OnMove(InputAction.CallbackContext context)
        {            
            Vector2 move = context.ReadValue<Vector2>();
            Horizontal = move.x;
            Vertical = move.y;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed) JumpPressed = true;
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            SprintPressed = context.ReadValue<float>() > 0.5f;
        }
        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.performed) CrouchPressed = true;
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed) InteractPressed = true;
        }
    

        public void OnThrow(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                ThrowPressed = true;
            }
        }

        public void OnRagdoll(InputAction.CallbackContext context)
        {
            RagdollPressed = context.performed || context.started;
        }
        public void OnLeftClick(InputAction.CallbackContext context)
        {
            if (context.performed) LeftClickPressed = true;
        }
        public void OnRightClick(InputAction.CallbackContext context)
        {
            if (context.performed) RightClickPressed = true;
        }
        public void OnCancel(InputAction.CallbackContext context)
        {
            if (context.performed) CancelPressed = true;
        }
        public void OnDrop(InputAction.CallbackContext context)
        {
            if (context.performed) DropPressed = true;
        }

        //Inventory Input
        public void OnScroll(InputAction.CallbackContext context)
        {
            ScrollDelta = context.ReadValue<Vector2>().y;
        }

        public void OnSlot1(InputAction.CallbackContext context)
        {
            if (context.performed) SlotIndexPressed = 1;
        }

        public void OnSlot2(InputAction.CallbackContext context)
        {
            if (context.performed) SlotIndexPressed = 2;
        }

        public void OnSlot3(InputAction.CallbackContext context)
        {
            if (context.performed) SlotIndexPressed = 3;
        }

        public void OnSlot4(InputAction.CallbackContext context)
        {
            if (context.performed) SlotIndexPressed = 4;
        }



        private void LateUpdate()
        {
            JumpPressed = false;
            CrouchPressed = false;
            InteractPressed = false;
            ThrowPressed = false;
            LeftClickPressed = false;
            RightClickPressed = false;
            CancelPressed = false;
            DropPressed = false;
            SlotIndexPressed = 0;
            ScrollDelta = 0f;
        }



        // ===== CONSUME METHODS =====

        public bool ConsumeJump()
        {
            if (!JumpPressed)
                return false;

            JumpPressed = false;
            return true;
        }

        public bool ConsumeCrouch()
        {
            if (!CrouchPressed)
                return false;

            CrouchPressed = false;
            return true;
        }

        public bool ConsumeInteract()
        {
            if (!InteractPressed)
                return false;

            InteractPressed = false;
            return true;
        }

        public bool ConsumeThrow()
        {
            if (!ThrowPressed)
                return false;

            ThrowPressed = false;
            return true;
        } 

        public bool ConsumeLeftClick()
        {
            if (!LeftClickPressed)
                return false;

            LeftClickPressed = false;
            return true;
        }

        public bool ConsumeRightClick()
        {
            if (!RightClickPressed)
                return false;

            RightClickPressed = false;
            return true;
        }
        public bool ConsumeCancel()
        {
            if (!CancelPressed)
                return false;

            CancelPressed = false;
            return true;
        }
        public bool ConsumeDrop()
        {
            if (!DropPressed)
                return false;

            DropPressed = false;
            return true;
        }
    }
}