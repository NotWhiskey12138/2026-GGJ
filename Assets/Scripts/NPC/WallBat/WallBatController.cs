using UnityEngine;
using NPCSystem.Frog;

namespace NPCSystem.WallBat
{
    public class WallBatController : FrogController
    {
        [Header("WallBat Gravity")]
        [SerializeField] private Vector2 gravityDirection = Vector2.down;
        [SerializeField] private float gravityStrength = 9.8f;
        [SerializeField] private bool zeroDefaultGravity = true;
        [SerializeField] private bool debugPatrol = false;

        private bool suspendPatrol;
        private Rigidbody2D cachedRb;
        private bool patrolForward = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            cachedRb = GetComponent<Rigidbody2D>();
            if (cachedRb != null && zeroDefaultGravity)
            {
                cachedRb.gravityScale = 0f;
            }
        }

        protected override void FixedUpdate()
        {
            ApplyCustomGravity();
            base.FixedUpdate();
        }

        public void SetGravityDirection(Vector2 direction)
        {
            gravityDirection = direction == Vector2.zero ? Vector2.down : direction.normalized;
            UpdateIdleDirectionFromGravity();
            UpdateRotationFromGravity();
        }

        public void SetPatrolSuspended(bool suspended)
        {
            suspendPatrol = suspended;
            if (debugPatrol)
            {
                Debug.Log($"[{name}] Patrol suspended = {suspendPatrol}");
            }
        }

        protected override void UpdateMovement()
        {
            if (suspendPatrol)
            {
                if (debugPatrol)
                {
                    Debug.Log($"[{name}] Patrol skipped (suspended).");
                }
                return;
            }

            base.UpdateMovement();
            if (debugPatrol)
            {
                Debug.Log($"[{name}] Patrol tick (idleSpeed={GetIdleSpeedViaReflection():0.###})");
            }
        }

        private void ApplyCustomGravity()
        {
            if (cachedRb == null) return;
            Vector2 dir = gravityDirection == Vector2.zero ? Vector2.down : gravityDirection.normalized;
            cachedRb.AddForce(dir * gravityStrength, ForceMode2D.Force);
        }

        private void UpdateIdleDirectionFromGravity()
        {
            Vector2 dir = gravityDirection == Vector2.zero ? Vector2.down : gravityDirection.normalized;
            Vector2 tangent = new Vector2(-dir.y, dir.x);
            if (!patrolForward)
            {
                tangent = -tangent;
            }
            if (debugPatrol)
            {
                Debug.Log($"[{name}] Gravity dir={dir} -> idle tangent={tangent}");
            }
            SetIdleDirectionViaReflection(tangent);
        }

        private void UpdateRotationFromGravity()
        {
            Vector2 dir = gravityDirection == Vector2.zero ? Vector2.down : gravityDirection.normalized;
            transform.rotation = Quaternion.FromToRotation(Vector2.down, dir);
            if (debugPatrol)
            {
                Debug.Log($"[{name}] Rotation updated for gravity dir={dir}");
            }
        }

        private void SetIdleDirectionViaReflection(Vector2 direction)
        {
            var field = typeof(FrogController).GetField("frogDomain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogWarning($"[{name}] frogDomain field not found on FrogController.");
                return;
            }

            var domain = field.GetValue(this);
            if (domain == null)
            {
                Debug.LogWarning($"[{name}] frogDomain is null.");
                return;
            }

            var method = domain.GetType().GetMethod("SetIdleDirection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(domain, new object[] { direction });
            }
            else
            {
                Debug.LogWarning($"[{name}] SetIdleDirection not found on FrogDomain.");
            }
        }

        private float GetIdleSpeedViaReflection()
        {
            var field = typeof(FrogController).GetField("frogDomain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) return -1f;
            var domain = field.GetValue(this);
            if (domain == null) return -1f;

            var prop = domain.GetType().GetProperty("IdleSpeed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (prop == null) return -1f;
            object value = prop.GetValue(domain);
            return value is float f ? f : -1f;
        }


        public override void FlipIdleDirection()
        {
            patrolForward = !patrolForward;
            UpdateIdleDirectionFromGravity();

            Vector3 scale = transform.localScale;
            scale.x = -scale.x;
            transform.localScale = scale;

            if (debugPatrol)
            {
                Debug.Log($"[{name}] FlipIdleDirection (wallbat) -> patrolForward={patrolForward}");
            }
        }
    }
}
