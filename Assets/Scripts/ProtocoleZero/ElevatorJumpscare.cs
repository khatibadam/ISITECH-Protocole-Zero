using UnityEngine;

namespace ProtocoleZero
{
    /// <summary>
    /// Proximity-triggered elevator scare: when the player approaches, one elevator door
    /// slides open, a silhouette is revealed with a stinger, then it hides and the door closes.
    /// Fires once. No forced camera movement (player keeps full control).
    /// </summary>
    public sealed class ElevatorJumpscare : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform door;
        [SerializeField] private GameObject figure;
        [SerializeField] private AudioSource stinger;
        [SerializeField] private float triggerDistance = 3.0f;
        [SerializeField] private Vector3 doorOpenOffset = new Vector3(0f, -2.25f, 0f);
        [SerializeField] private float openSpeed = 6f;
        [SerializeField] private float showSeconds = 1.6f;

        private Vector3 doorClosedPos;
        private bool triggered;
        private bool opening;
        private float timer;

        private void Awake()
        {
            if (door != null)
            {
                doorClosedPos = door.localPosition;
            }

            if (figure != null)
            {
                figure.SetActive(false);
            }

            if (player == null)
            {
                GameObject tagged = GameObject.FindWithTag("Player");
                if (tagged != null)
                {
                    player = tagged.transform;
                }
            }
        }

        private void Update()
        {
            if (!triggered && player != null)
            {
                if (Vector3.Distance(player.position, transform.position) < triggerDistance)
                {
                    triggered = true;
                    opening = true;
                    timer = showSeconds;
                    if (figure != null)
                    {
                        figure.SetActive(true);
                    }

                    if (stinger != null)
                    {
                        stinger.Stop();
                        stinger.Play();
                    }
                }
            }

            if (!triggered || door == null)
            {
                return;
            }

            Vector3 target = opening ? doorClosedPos + doorOpenOffset : doorClosedPos;
            door.localPosition = Vector3.MoveTowards(door.localPosition, target, openSpeed * Time.deltaTime);

            if (opening)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    opening = false;
                    if (figure != null)
                    {
                        figure.SetActive(false);
                    }
                }
            }
        }
    }
}
