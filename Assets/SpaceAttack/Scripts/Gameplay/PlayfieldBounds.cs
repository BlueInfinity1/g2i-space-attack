using UnityEngine;

namespace SpaceAttack
{
    public sealed class PlayfieldBounds : MonoBehaviour
    {
        [Min(1f)] public float halfWidth = 9f;
        public float playerY = -6.4f;
        public float dangerLineY = -6.1f;
        public float escapeBottomY = -8.4f;
        public float escapeTopY = 8.4f;
        [Min(0f)] public float escapeSideMargin = 1f;

        public Vector2 ClampPlayer(Vector2 position) => new Vector2(
            Mathf.Clamp(position.x, -halfWidth, halfWidth), playerY);

        public bool IsOutside(Vector2 position) => position.y < escapeBottomY ||
            position.y > escapeTopY || Mathf.Abs(position.x) > halfWidth + escapeSideMargin;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(0f, (escapeTopY + escapeBottomY) / 2f, 0f),
                new Vector3(halfWidth * 2f, escapeTopY - escapeBottomY, 0f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(-halfWidth, dangerLineY, 0f), new Vector3(halfWidth, dangerLineY, 0f));
        }
    }
}
