using Sources.Characters;
using Sources.Controllers;
using Game.Gameplay.Characters;
using Sources.Code.Interfaces;
using Sources.Code.Gameplay.Interaction;
using Sources.Code.Gameplay.Grab;
using Sources.Code.Gameplay.Inventory;
using UnityEngine;

namespace Sources.Code.Gameplay.Characters
{
    public class PlayerCharacter : Entity
    {
        [SerializeField] Transform handSocket;

        private GroundMover     _mover;
        private PlayerInteract  _interact;
        private CameraController _camera;
        private InventorySystem  _inventory;
        private GrabInteractor   _grabInteractor;

        private IInputManager _input;

        public PlayerInteract  Interact  => _interact;
        public InventorySystem Inventory => _inventory;
        public Transform HandSocket
        {
            get => handSocket;
            set => handSocket = value;
        }


        void Awake()
        {
            _mover         = GetComponent<GroundMover>();
            _interact      = GetComponentInChildren<PlayerInteract>();
            _camera        = GetComponentInChildren<CameraController>();
            _inventory     = GetComponentInChildren<InventorySystem>();
            _grabInteractor = GetComponentInChildren<GrabInteractor>();

            if (_inventory != null && handSocket != null)
                _inventory.HandSocket = handSocket;
            
        }

        public void Construct(IInputManager input)
        {
            _input = input;

            _inventory.Construct(input);

            _mover.Construct(input, _inventory);
            _camera.Construct(input);
            _interact.Construct(input);
            _grabInteractor.Construct(input);
        }

        void Update()
        {
            if (_input == null || _input.IsLocked)
                return;

            _interact.UpdateInteract();
        }
    }
}
