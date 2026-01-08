using UnityEngine;

namespace Sources.Code.Gameplay.Grab
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class GrabInteractible : MonoBehaviour
    {
        private Rigidbody rb;
        private SpringJoint joint;

        private float prevAngularDamping;
        private float prevLinearDamping;
        private float prevSleepThreshold;
        private RigidbodyInterpolation prevInterpolation;

        private float breakingDistance;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        // ===============================
        // PHYSICS HOLD
        // ===============================

        public void Lock(JointCreationSettings settings)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.minDistance = 0f;
            joint.maxDistance = 0f;
            joint.anchor = Vector3.zero;
            joint.damper = settings.damper;
            joint.spring = settings.spring;
            joint.massScale = settings.massScale;

            prevAngularDamping = rb.angularDamping;
            prevLinearDamping = rb.linearDamping;
            prevSleepThreshold = rb.sleepThreshold;
            prevInterpolation = rb.interpolation;

            rb.angularDamping = settings.angularDrag;
            rb.linearDamping = settings.drag;
            rb.sleepThreshold = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            breakingDistance = settings.breakingDistance;
        }

        public void Unlock()
        {
            if (joint != null)
                Destroy(joint);

            joint = null;

            rb.angularDamping = prevAngularDamping;
            rb.linearDamping = prevLinearDamping;
            rb.sleepThreshold = prevSleepThreshold;
            rb.interpolation = prevInterpolation;
        }

        public bool Follow(Vector3 anchor)
        {
            if (joint == null)
                return false;

            joint.connectedAnchor = anchor;
            return Vector3.Distance(transform.position, anchor) <= breakingDistance;
        }

        // ===============================
        // HAND HOLD
        // ===============================

        public void AttachToHand(Transform hand)
        {
            Unlock();

            rb.isKinematic = true;
            rb.detectCollisions = false;

            transform.SetParent(hand);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void DetachFromHand()
        {
            transform.SetParent(null);

            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        // ===============================
        // THROW
        // ===============================

        public void Push(Vector3 force)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}
