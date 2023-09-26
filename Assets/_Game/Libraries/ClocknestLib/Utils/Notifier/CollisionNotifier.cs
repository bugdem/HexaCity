using UnityEngine;
using UnityEngine.Events;

namespace ClocknestGames.Library.Utils
{
    [System.Serializable]
    public class CollisionEvent : UnityEvent<Collision> { }

    public class CollisionNotifier : MonoBehaviour
    {
        public LayerMask CollisionLayer = -1;
        public bool TriggerOnce = false;

        public CollisionEvent OnEnterEvent;
        public CollisionEvent OnStayEvent;
        public CollisionEvent OnExitEvent;

        private bool _triggeredBefore;

        private void OnCollisionEnter(Collision other)
        {
            if (!ConditionMet(other))
                return;

            OnEnterEvent?.Invoke(other);

            OnTrigger();
        }

        private void OnCollisionStay(Collision other)
        {
            if (!ConditionMet(other))
                return;

            OnStayEvent?.Invoke(other);
        }

        private void OnCollisionExit(Collision other)
        {
            if (!ConditionMet(other))
                return;

            OnExitEvent?.Invoke(other);
        }

        private bool ConditionMet(Collision other)
        {
            if (TriggerOnce && _triggeredBefore)
                return false;

            if (!CollisionLayer.Contains(other.collider.gameObject.layer))
                return false;

            return true;
        }

        private void OnTrigger()
        {
            _triggeredBefore = true;
        }
    }
}