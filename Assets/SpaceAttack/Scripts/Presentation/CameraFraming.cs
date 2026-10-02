using UnityEngine;

namespace SpaceAttack
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class CameraFraming : MonoBehaviour
    {
        [SerializeField] private float minimumHalfHeight = 9f;
        [SerializeField] private float minimumHalfWidth = 10.5f;
        private void LateUpdate()
        {
            Camera view = GetComponent<Camera>();
            view.orthographicSize = Mathf.Max(minimumHalfHeight, minimumHalfWidth / Mathf.Max(0.1f, view.aspect));
        }
    }
}
