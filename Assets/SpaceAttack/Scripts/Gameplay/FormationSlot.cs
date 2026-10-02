using UnityEngine;

namespace SpaceAttack
{
    public sealed class FormationSlot : MonoBehaviour
    {
        public EnemyKind kind;
        private void OnDrawGizmos()
        {
            Gizmos.color = kind == EnemyKind.Red ? Color.red : kind == EnemyKind.Green ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.65f, 0.4f, 0.1f));
        }
    }
}
