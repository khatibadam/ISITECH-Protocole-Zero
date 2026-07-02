using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Cheap dynamic cable visual: draws a sagging curve between a fixed anchor on
    /// the electrical panel and the tail of a grabbable plug, so the cable stays
    /// visibly attached to the panel while the plug is carried around in VR.
    /// Quest-friendly: one LineRenderer, ~14 segments, no physics.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class CableVisual : MonoBehaviour
    {
        [SerializeField] private Transform panelAnchor;
        [SerializeField] private Transform plugTail;
        [SerializeField, Min(2)] private int segments = 14;
        [SerializeField, Min(0f)] private float restLength = 1.1f;
        [SerializeField, Min(0f)] private float extraSag = 0.1f;

        private LineRenderer line;
        private Vector3[] points;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            EnsureBuffer();
        }

        private void EnsureBuffer()
        {
            if (points == null || points.Length != segments + 1)
            {
                points = new Vector3[segments + 1];
                line.positionCount = segments + 1;
            }
        }

        private void LateUpdate()
        {
            if (panelAnchor == null || plugTail == null)
            {
                return;
            }

            EnsureBuffer();
            Vector3 a = panelAnchor.position;
            Vector3 b = plugTail.position;
            float dist = Vector3.Distance(a, b);

            // The closer the plug is to the panel, the more slack hangs in the cable.
            float sag = extraSag + Mathf.Max(0f, restLength - dist) * 0.5f;
            Vector3 mid = (a + b) * 0.5f + Vector3.down * sag;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                points[i] = Vector3.Lerp(Vector3.Lerp(a, mid, t), Vector3.Lerp(mid, b, t), t);
            }

            line.SetPositions(points);
        }
    }
}
