using Sources.Code.Gameplay.Inventory;
using Sources.Code.Interfaces;
using UnityEngine;

namespace Sources.Characters 
{
    public class GroundMover : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] private float forwardSpeed = 6f;
        [SerializeField] private float backwardSpeed = 4f;
        [SerializeField] private float strafeSpeed = 5f;
        [SerializeField] private float speedRun = 10f;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private CharacterController player;
        public float MaxSpeed => speedRun;

        [Header("Jump and gravity specifics")]
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float fallMultiplierJump = 2f;
        [SerializeField] private float fallMultiplierFall = 1.2f;
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private float maxFallSpeed = 50f;
        [SerializeField] private float maxGroundAngle = 60f;
        [SerializeField] private bool active = true;
        private float verticalVelocity = -2f;
        public bool IsGrounded { get; private set; }

        private IInputManager _input;
        private InventorySystem _inventory;
        private float speedMultiplier = 1f;
        private bool sprintEnabled = true;
        private bool movementEnabled = true;
        private bool jumpEnabled = true;

        public float SpeedMultiplier => speedMultiplier;
        public bool SprintEnabled => sprintEnabled;
        public bool MovementEnabled => movementEnabled;
        public bool JumpEnabled => jumpEnabled;

        public void Construct(IInputManager input, InventorySystem inventory)
        {
            _input = input;
            _inventory = inventory;
        }

        void Update()
        {
            if (_input == null)
                return;

            if (active)
                ApplyGravity(); 

            if (_input.IsLocked || !active)
                return;

            DoMove();
        }


        public void DoMove()
        {            
            if (!movementEnabled) return;
            
            Vector2 input = new Vector2(_input.Horizontal, _input.Vertical);
            bool running = _input.SprintPressed && sprintEnabled;

            MovePlayer(running, input);
        }

        private void MovePlayer(bool isRunning, Vector2 input)
        {
            if (player == null || playerCamera == null) return;

            Vector3 forward = playerCamera.transform.forward;
            Vector3 right = playerCamera.transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            float speedY = 0f;
            if (input.y > 0)
                speedY = isRunning ? speedRun : forwardSpeed;
            else if (input.y < 0)
                speedY = isRunning ? speedRun : backwardSpeed;

            float speedX = input.x != 0 ? (isRunning ? speedRun : strafeSpeed) : 0f;

            Vector3 moveDirection = forward * input.y * speedY + right * input.x * speedX;

            float weightMultiplier = 1f;
            if (_inventory != null)
                weightMultiplier = Mathf.Clamp(1f - (_inventory.TotalWeight / 20f), 0.3f, 1f);

            moveDirection *= speedMultiplier * weightMultiplier;

            player.Move(moveDirection * Time.deltaTime);
        }

        public void ApplyGravity()
        {
            if (!active) return;
            if (player == null) return;
            if (!player.enabled) return;

            IsGrounded = CheckGrounded();

            if (IsGrounded)
            {
                if (verticalVelocity < 0f) 
                    verticalVelocity = -2f;
            }
            else
            {
                if (verticalVelocity > 0f)
                    verticalVelocity -= gravity * fallMultiplierJump * Time.deltaTime;
                else
                    verticalVelocity -= gravity * fallMultiplierFall * Time.deltaTime;
            }

            verticalVelocity = Mathf.Clamp(verticalVelocity, -maxFallSpeed, maxFallSpeed);

            Vector3 move = Vector3.up * verticalVelocity * Time.deltaTime;
            player.Move(move);
        }

        public bool CheckGrounded()
        {
            if (player == null) return false;

            Vector3 origin = player.transform.position + Vector3.up * 0.05f;
            float radius = player.radius * 0.9f;
            Vector3[] offsets = 
            { 
                Vector3.zero, 
                Vector3.forward * radius, 
                Vector3.back * radius, 
                Vector3.left * radius, 
                Vector3.right * radius 
            };

            foreach (var offset in offsets)
            {
                Vector3 rayOrigin = origin + offset;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (Vector3.Angle(hit.normal, Vector3.up) <= maxGroundAngle)
                        return true;
                }
            }

            return false;
        }

        public float CurrentSpeed
        {
            get
            {
                if (player == null) return 0f;
                Vector3 v = player.velocity;
                v.y = 0f;
                return v.magnitude;
            }
        }

        public void SetSpeedMultiplier(float value)
        {
            speedMultiplier = Mathf.Clamp(value, 0.1f, 5f);
        }

        public void SetSprintEnabled(bool value)
        {
            sprintEnabled = value;
        }

        public void SetJumpEnabled(bool value)
        {
            jumpEnabled = value;
        }

        public void SetMovementEnabled(bool value)
        {
            movementEnabled = value;
        }
    }
}
