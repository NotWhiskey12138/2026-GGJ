using UnityEngine;
using NPCSystem.Controller;

namespace NPCSystem.Abilities
{
    public abstract class NpcAbility : MonoBehaviour
    {
        protected NpcController controller;
        protected bool isActive;

        protected virtual void Awake()
        {
            controller = GetComponentInParent<NpcController>();
        }

        public virtual void OnPossessedStart()
        {
            isActive = true;
        }

        public virtual void OnPossessedEnd()
        {
            isActive = false;
        }

        public virtual void OnTriggerEnter2D(Collider2D other)
        {
        }

        public virtual void OnTriggerStay2D(Collider2D other)
        {
        }
    }
}
