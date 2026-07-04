using System.Collections;
using UnityEngine;

namespace ProtocoleZero
{
    public sealed class SubtitleManager : MonoBehaviour
    {
        [SerializeField] private TextMesh textTarget;
        [SerializeField] private float defaultDuration = 3.5f;

        private Coroutine hideRoutine;
        private bool subtitlesEnabled = true;

        public bool SubtitlesEnabled => subtitlesEnabled;

        public void SetEnabled(bool enabled)
        {
            subtitlesEnabled = enabled;
            if (!enabled && textTarget != null)
            {
                textTarget.text = string.Empty;
            }
        }

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
            if (!subtitlesEnabled)
            {
                return;
            }

            if (textTarget == null)
            {
                Debug.Log("[Subtitle] " + line);
                return;
            }

            textTarget.text = Wrap(line);
            textTarget.gameObject.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideAfter(Mathf.Max(0.5f, duration)));
        }

        // TextMesh ne fait aucun retour a la ligne : une longue replique deborde
        // des bords de l'ecran sur PC (champ de vision plus etroit qu'en VR).
        // Coupe aux espaces pour ne jamais depasser ~38 caracteres par ligne.
        private const int MaxCharsPerLine = 38;

        private static string Wrap(string line)
        {
            if (string.IsNullOrEmpty(line) || line.Length <= MaxCharsPerLine)
            {
                return line;
            }

            var builder = new System.Text.StringBuilder(line.Length + 4);
            int lineLength = 0;
            string[] words = line.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                int wordLength = words[i].Length;
                if (lineLength > 0 && lineLength + 1 + wordLength > MaxCharsPerLine)
                {
                    builder.Append('\n');
                    lineLength = 0;
                }
                else if (lineLength > 0)
                {
                    builder.Append(' ');
                    lineLength++;
                }

                builder.Append(words[i]);
                lineLength += wordLength;
            }

            return builder.ToString();
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
