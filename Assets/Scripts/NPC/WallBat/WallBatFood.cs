using UnityEngine;

namespace NPCSystem.WallBat
{
    public class WallBatFood : MonoBehaviour
    {
        [SerializeField] private Vector2 gravityDirection = Vector2.up;

        public Vector2 GravityDirection => gravityDirection;
    }
}
