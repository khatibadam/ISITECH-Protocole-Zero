using System.Collections;
using UnityEngine;

namespace ProtocoleZero
{
    public sealed class SubtitleManager : MonoBehaviour
    {
        [SerializeField] private TextMesh textTarget;
        [SerializeField] private float defaultDuration = 3.5f;

        private Coroutine hideRoutine;

        private void Awake()
        {
            if (textTarget != null)
            {
                textTarget.text = string.Empty;
            }
        }

        public void ShowLine(string line)
        {
            ShowLine(line, defaultDuration);
        }

        public void ShowLine(string line, float duration)
        {
            if (textTarget == null)
            {
                Debug.Log("[Subtitle] " + line);
                return;
            }

            textTarget.text = line;
            textTarget.gameObject.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideAfter(Mathf.Max(0.5f, duration)));
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (textTarget != null)
            {
                textTarget.text = string.Empty;
            }

            hideRoutine = null;
        }
    }
}
