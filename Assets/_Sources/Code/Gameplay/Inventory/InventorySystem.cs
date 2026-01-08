using Sources.Code.Interfaces;
using Sources.Code.Interfaces.Inventory;
using UnityEngine;

namespace Sources.Code.Gameplay.Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        [SerializeField] InventorySlot[] slots = new InventorySlot[4];
        [SerializeField] int selectedSlot;
        [SerializeField] Transform handSocket;

        public Transform HandSocket
        {
            get => handSocket;
            set => handSocket = value;
        }

        float totalWeight;
        IInputManager _input;
        InventoryItem _equippedItem;

        public float TotalWeight => totalWeight;
        public bool IsFull => GetFirstEmptySlotIndex() == -1;
        public int SelectedSlot => selectedSlot;

        public event System.Action OnWeightChanged;
        public event System.Action<int> OnSelectedSlotChanged;

        public void Construct(IInputManager input)
        {
            _input = input;
            SelectSlot(0);
        }

        void Update()
        {
            if (_input == null) return;
            HandleInput();
        }

        void HandleInput()
        {
            if (_input.SlotIndexPressed != 0)
                SelectSlot(_input.SlotIndexPressed - 1);

            if (Mathf.Abs(_input.ScrollDelta) > 0.01f)
            {
                int dir = _input.ScrollDelta > 0 ? -1 : 1;
                int next = (selectedSlot + dir + slots.Length) % slots.Length;
                SelectSlot(next);
            }

            if (_input.ConsumeDrop())
                DropSelected();
        }

        public bool TryAddSmart(IInventoryItem item, out int slotIndex)
        {
            slotIndex = -1;

            if (selectedSlot >= 0 && selectedSlot < slots.Length)
            {
                if (slots[selectedSlot].IsEmpty && slots[selectedSlot].TrySet(item))
                {
                    slotIndex = selectedSlot;
                    totalWeight += item.Weight;
                    OnWeightChanged?.Invoke();
                    EquipSelectedToHand();
                    return true;
                }
            }

            int idx = GetFirstEmptySlotIndex();
            if (idx == -1)
                return false;

            if (slots[idx].TrySet(item))
            {
                slotIndex = idx;
                totalWeight += item.Weight;
                OnWeightChanged?.Invoke();
                EquipSelectedToHand();
                return true;
            }

            return false;
        }

        public void DropSelected()
        {
            var slot = GetSlot(selectedSlot);
            if (slot == null || slot.IsEmpty) return;

            var item = slot.Item;
            slot.Clear();
            totalWeight -= item.Weight;
            OnWeightChanged?.Invoke();

            if (item is InventoryItem invItem)
            {
                if (_equippedItem == invItem)
                    _equippedItem = null;

                Vector3 originPos = handSocket != null ? handSocket.position : invItem.transform.position;
                Vector3 forward   = handSocket != null ? handSocket.forward   : Vector3.forward;

                invItem.DropFromHand(originPos, forward);
            }
        }

        void SelectSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            if (selectedSlot == index) return;

            selectedSlot = index;
            EquipSelectedToHand();
            OnSelectedSlotChanged?.Invoke(selectedSlot);
        }

        void EquipSelectedToHand()
        {
            if (_equippedItem != null && _equippedItem.IsInInventory)
            {
                _equippedItem.DetachFromHand();
                _equippedItem = null;
            }

            var slot = GetSlot(selectedSlot);
            if (slot == null || slot.IsEmpty) return;

            var item = slot.Item as InventoryItem;
            if (item == null) return;
            if (handSocket == null) return;

            _equippedItem = item;
            item.AttachToHand(handSocket);
        }

        int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsEmpty) return i;
            return -1;
        }

        public IInventoryItem GetSelectedItem()
        {
            if (selectedSlot < 0 || selectedSlot >= slots.Length)
                return null;
            return slots[selectedSlot].Item;
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Length)
                return null;
            return slots[index];
        }

        public float GetSlotWeight(int index)
        {
            if (index < 0 || index >= slots.Length)
                return 0f;
            return slots[index].Weight;
        }
    }
}
