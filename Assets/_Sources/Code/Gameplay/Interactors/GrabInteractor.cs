using Sources.Code.Interfaces;
using UnityEngine;

namespace Sources.Code.Gameplay.Grab
{
    public class GrabInteractor : MonoBehaviour
    {
        [Header("Screen Center Hold")]
        [SerializeField] private Transform screenCenterSocket;

        [Header("Physics Hold")]
        [SerializeField] private float jointAnchorDistance = 2.5f;
        [SerializeField] private float throwingForce = 10f;

        [Header("Joint Settings")]
        [SerializeField] private float drag = 10f;
        [SerializeField] private float angularDrag = 5f;
        [SerializeField] private float damper = 4f;
        [SerializeField] private float spring = 100f;
        [SerializeField] private float massScale = 1f;
        [SerializeField] private float breakingDistance = 3f;

        private GrabInteractible current;
        public bool IsHolding => current != null;

        private IInputManager _input;

        public void Construct(IInputManager input)
        {
            _input = input;
        }

        private void Update()
        {
            if (_input == null || _input.IsLocked)
                return;

            if (IsHolding && _input.ConsumeDrop())
            {
                Drop();
                return;
            }

            if (!IsHolding)
                return;

            Vector3 anchor = screenCenterSocket.position;

            if (!current.Follow(anchor))
                Drop();
        }

        public void Grab(GrabInteractible target)
        {
            if (IsHolding || target == null)
                return;

            current = target;

            current.Lock(new JointCreationSettings
            {
                drag = drag,
                angularDrag = angularDrag,
                damper = damper,
                spring = spring,
                massScale = massScale,
                breakingDistance = breakingDistance
            });
        }

        public void Drop(bool throwObject = false)
        {
            if (!IsHolding)
                return;

            current.Unlock();

            if (throwObject)
                current.Push(transform.forward * throwingForce);

            current = null;
        }

        public void Throw() => Drop(true);
    }
}
