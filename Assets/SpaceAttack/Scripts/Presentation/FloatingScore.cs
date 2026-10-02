using UnityEngine;
using UnityEngine.UI;

namespace SpaceAttack
{
    public sealed class FloatingScore : MonoBehaviour
    {
        private Text label;
        private RectTransform rect;
        private Canvas canvas;
        private Camera view;
        private Vector3 worldPosition;
        private float remaining = 0.85f;
        public void Initialize(int score, Vector3 position, Camera camera, Canvas owner)
        {
            canvas = owner;
            view = camera;
            worldPosition = position;
            rect = GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160f, 42f);
            label = GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "+" + score;
            label.color = Color.white;
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            var outline = GetComponent<Outline>();
            outline.effectColor = new Color(0.015f, 0.025f, 0.05f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Place();
        }

        private void LateUpdate()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f) { Destroy(gameObject); return; }
            worldPosition += Vector3.up * (0.65f * Time.deltaTime);
            Place();
            Color c = label.color;
            c.a = Mathf.Clamp01(remaining / 0.3f);
            label.color = c;
        }

        private void Place()
        {
            Vector2 screen = view.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, screen,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : view, out Vector2 local);
            rect.anchoredPosition = local;
        }
    }
}
