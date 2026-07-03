using UnityEngine;
using UnityEngine.XR;

namespace ProtocoleZero
{
    /// <summary>
    /// Proximity-triggered elevator scare: when the player approaches, one elevator door
    /// slides open, a silhouette is revealed with a stinger, then it hides and the door closes.
    /// Fires once.
    /// Expressive pass: the figure lunges toward the player with convulsive jitter and a
    /// scale pulse, lit by a flickering red light spawned at runtime. On desktop (no VR
    /// headset) the camera briefly locks and zooms onto the figure so the scare is fully
    /// readable; in VR the head stays free (no forced camera) to avoid motion sickness.
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

        [Header("Expressivite de la silhouette")]
        [Tooltip("Distance de la ruee vers le joueur au moment du cri.")]
        [SerializeField, Min(0f)] private float lungeDistance = 0.7f;
        [SerializeField, Min(0.05f)] private float lungeSeconds = 0.3f;
        [Tooltip("Amplitude des convulsions (degres).")]
        [SerializeField, Min(0f)] private float shakeDegrees = 10f;
        [SerializeField, Range(0f, 0.3f)] private float scalePulse = 0.06f;
        [SerializeField] private Color scareLightColor = new Color(0.95f, 0.25f, 0.14f);
        [SerializeField, Min(0f)] private float scareLightIntensity = 2.4f;

        [Header("Zoom camera (PC uniquement, jamais en VR)")]
        [SerializeField] private bool desktopZoom = true;
        [Tooltip("Laisser vide pour utiliser Camera.main.")]
        [SerializeField] private Camera desktopCamera;
        [SerializeField, Range(20f, 60f)] private float zoomFieldOfView = 30f;
        [SerializeField, Min(0.1f)] private float zoomRecoverSeconds = 0.45f;

        [Header("Chasse finale (batterie du PC vide)")]
        [Tooltip("Quand la batterie tombe a zero, le gardien quitte l'ascenseur et court vers le joueur : contact = defaite.")]
        [SerializeField] private bool chaseWhenBatteryEmpty = true;
        [SerializeField] private BatteryTimer batteryTimer;
        [SerializeField, Min(0f)] private float chaseSpeed = 2.2f;

        private Vector3 doorClosedPos;
        private bool triggered;
        private bool opening;
        private float timer;

        private float scareElapsed;
        private float noiseSeed;
        private Vector3 figureStartPosition;
        private Quaternion figureStartRotation;
        private Vector3 figureStartScale;
        private Vector3 lungeDirection = Vector3.forward;
        private Vector3 focusLocalOffset = Vector3.up * 1.5f;
        private Light scareLight;
        private EntityContactScare contact;
        private bool chasing;
        private bool guarding;

        private Camera zoomCamera;
        private bool zoomActive;
        private bool zoomRecovering;
        private float zoomRecoverT;
        private Quaternion camStartLocalRotation;
        private Quaternion camHideLocalRotation;
        private float camStartFov;
        private float camHideFov;

        private void Awake()
        {
            if (door != null)
            {
                doorClosedPos = door.localPosition;
            }

            if (figure != null)
            {
                figure.SetActive(false);

                // Gardien tangible : on ne peut pas le traverser, et le toucher
                // rejoue le cri (sans faire perdre la partie, sauf chasse finale).
                contact = figure.GetComponent<EntityContactScare>();
                if (contact == null)
                {
                    contact = figure.AddComponent<EntityContactScare>();
                }

                // Le cri du stinger sert aussi de hurlement de mort pendant la
                // chasse finale (le runner le joue plein volume, non spatialise).
                contact.Configure(stinger != null ? stinger.clip : null, 1f, false, 2.2f, ReplayStinger);
            }

            if (batteryTimer == null)
            {
                batteryTimer = FindFirstObjectByType<BatteryTimer>();
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
            // Sequence de defaite en cours : tout se fige, le runner a la main.
            if (EntityContactScare.DeathSequenceActive)
            {
                return;
            }

            if (!chasing && chaseWhenBatteryEmpty && batteryTimer != null && batteryTimer.IsEmpty)
            {
                BeginChase();
            }

            if (chasing)
            {
                UpdateChase();
                UpdateZoom();
                return;
            }

            if (!triggered && player != null
                && Vector3.Distance(player.position, transform.position) < triggerDistance)
            {
                BeginScare();
            }

            if (!triggered)
            {
                return;
            }

            if (door != null)
            {
                // Apres le premier jumpscare la porte reste ouverte : le gardien
                // monte la garde dans l'encadrement, visible en permanence.
                Vector3 target = opening || guarding ? doorClosedPos + doorOpenOffset : doorClosedPos;
                door.localPosition = Vector3.MoveTowards(door.localPosition, target, openSpeed * Time.deltaTime);
            }

            if (opening)
            {
                AnimateFigure();
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    EndScare();
                }
            }

            UpdateZoom();
        }

        private void BeginScare()
        {
            triggered = true;
            opening = true;
            timer = showSeconds;
            scareElapsed = 0f;
            noiseSeed = Random.value * 100f;

            if (figure != null)
            {
                figure.SetActive(true);
                figureStartPosition = figure.transform.position;
                figureStartRotation = figure.transform.rotation;
                figureStartScale = figure.transform.localScale;

                Vector3 toPlayer = player != null
                    ? player.position - figureStartPosition
                    : figure.transform.forward;
                toPlayer.y = 0f;
                lungeDirection = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector3.forward;

                focusLocalOffset = ComputeFocusOffset();
                CreateScareLight();
            }

            if (stinger != null)
            {
                // Tres fort : c'est le sursaut principal du jeu.
                stinger.volume = 1f;
                stinger.Stop();
                stinger.Play();
            }

            BeginZoom();
        }

        // Batterie vide : le gardien quitte son poste et court vers le joueur.
        // Le toucher devient une defaite (la partie redemarre).
        private void BeginChase()
        {
            if (opening)
            {
                EndScare();
            }

            chasing = true;
            triggered = true;

            if (figure != null)
            {
                figure.SetActive(true);
                if (figureStartScale == Vector3.zero)
                {
                    figureStartScale = figure.transform.localScale;
                }
            }

            contact?.SetRestartOnTouch(true);
            ReplayStinger();
        }

        private void UpdateChase()
        {
            // La porte reste grande ouverte pendant la chasse.
            if (door != null)
            {
                door.localPosition = Vector3.MoveTowards(
                    door.localPosition, doorClosedPos + doorOpenOffset, openSpeed * Time.deltaTime);
            }

            if (figure == null || player == null)
            {
                return;
            }

            Vector3 position = figure.transform.position;
            Vector3 toPlayer = player.position - position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance < 0.05f)
            {
                return;
            }

            Vector3 direction = toPlayer / distance;
            if (HasLineOfSightToPlayer(position + Vector3.up * 1.2f))
            {
                figure.transform.position = position + direction * (chaseSpeed * Time.deltaTime);
            }

            // Meme convention d'orientation que l'entite errante.
            figure.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
        }

        private bool HasLineOfSightToPlayer(Vector3 from)
        {
            Vector3 target = player.position + Vector3.up * 1.5f;
            Vector3 to = target - from;
            float distance = to.magnitude;
            if (distance < 0.1f)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(from, to / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i].collider;
                if (figure != null && hit.transform.IsChildOf(figure.transform))
                {
                    continue;
                }

                if (hit is CharacterController || hit.GetComponentInParent<SimplePlayerController>() != null)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void AnimateFigure()
        {
            scareElapsed += Time.deltaTime;
            if (figure == null)
            {
                return;
            }

            // Ruee vers le joueur avec amorti (ease-out) : le gros du deplacement
            // arrive dans les premiers dixiemes de seconde, en meme temps que le cri.
            float lungeT = Mathf.Clamp01(scareElapsed / lungeSeconds);
            float ease = 1f - (1f - lungeT) * (1f - lungeT);
            Vector3 position = figureStartPosition + lungeDirection * (lungeDistance * ease);

            // Convulsions : bruit de Perlin rapide sur le lacet et le tangage.
            float yaw = (Mathf.PerlinNoise(noiseSeed, scareElapsed * 14f) - 0.5f) * 2f * shakeDegrees;
            float tilt = (Mathf.PerlinNoise(noiseSeed + 5f, scareElapsed * 11f) - 0.5f) * shakeDegrees;
            Quaternion rotation = figureStartRotation * Quaternion.Euler(tilt, yaw, 0f);

            float pulse = 1f + Mathf.Sin(scareElapsed * 24f) * scalePulse;

            figure.transform.SetPositionAndRotation(position, rotation);
            figure.transform.localScale = figureStartScale * pulse;

            if (scareLight != null)
            {
                scareLight.transform.position = position + focusLocalOffset + lungeDirection * 0.6f;
                float flicker = 0.45f + 0.55f * Mathf.PerlinNoise(noiseSeed + 9f, scareElapsed * 18f);
                scareLight.intensity = scareLightIntensity * flicker;
            }
        }

        private void EndScare()
        {
            opening = false;
            guarding = true;

            // Le gardien ne disparait plus : il reprend sa pose et reste en
            // faction devant la porte ouverte (immobile, solide, toucher = cri).
            if (figure != null)
            {
                figure.transform.SetPositionAndRotation(figureStartPosition, figureStartRotation);
                figure.transform.localScale = figureStartScale;
            }

            if (scareLight != null)
            {
                Destroy(scareLight.gameObject);
                scareLight = null;
            }

            if (zoomActive && zoomCamera != null)
            {
                zoomRecovering = true;
                zoomRecoverT = 0f;
                camHideLocalRotation = zoomCamera.transform.localRotation;
                camHideFov = zoomCamera.fieldOfView;
            }
        }

        private void ReplayStinger()
        {
            if (stinger != null)
            {
                stinger.Stop();
                stinger.Play();
            }
        }

        private Vector3 ComputeFocusOffset()
        {
            Renderer[] renderers = figure.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return Vector3.up * 1.5f;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Cadre la tete : le zoom se plante droit dans son visage.
            Vector3 focus = bounds.center + Vector3.up * (bounds.extents.y * 0.75f);
            return focus - figureStartPosition;
        }

        private void CreateScareLight()
        {
            var go = new GameObject("JumpscareScareLight");
            scareLight = go.AddComponent<Light>();
            scareLight.type = LightType.Point;
            scareLight.color = scareLightColor;
            scareLight.range = 7f;
            scareLight.intensity = 0f;
            scareLight.shadows = LightShadows.None;
            scareLight.transform.position = figureStartPosition + focusLocalOffset + lungeDirection * 0.6f;
        }

        private void BeginZoom()
        {
            if (!desktopZoom || XRSettings.isDeviceActive)
            {
                return;
            }

            zoomCamera = desktopCamera != null ? desktopCamera : Camera.main;
            if (zoomCamera == null)
            {
                return;
            }

            zoomActive = true;
            zoomRecovering = false;
            camStartLocalRotation = zoomCamera.transform.localRotation;
            camStartFov = zoomCamera.fieldOfView;
            SimplePlayerController.CameraOverrideActive = true;
        }

        private void UpdateZoom()
        {
            if (!zoomActive || zoomCamera == null)
            {
                return;
            }

            if (!zoomRecovering)
            {
                // Verrouille progressivement le regard sur la silhouette et resserre le champ.
                Vector3 focus = (figure != null ? figure.transform.position : figureStartPosition) + focusLocalOffset;
                Vector3 toFocus = focus - zoomCamera.transform.position;
                if (toFocus.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(toFocus.normalized, Vector3.up);
                    float chase = 1f - Mathf.Exp(-10f * Time.deltaTime);
                    zoomCamera.transform.rotation = Quaternion.Slerp(zoomCamera.transform.rotation, look, chase);
                }

                float fovChase = 1f - Mathf.Exp(-7f * Time.deltaTime);
                zoomCamera.fieldOfView = Mathf.Lerp(zoomCamera.fieldOfView, zoomFieldOfView, fovChase);
                return;
            }

            zoomRecoverT += Time.deltaTime / zoomRecoverSeconds;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(zoomRecoverT));
            zoomCamera.transform.localRotation = Quaternion.Slerp(camHideLocalRotation, camStartLocalRotation, s);
            zoomCamera.fieldOfView = Mathf.Lerp(camHideFov, camStartFov, s);

            if (zoomRecoverT >= 1f)
            {
                zoomCamera.transform.localRotation = camStartLocalRotation;
                zoomCamera.fieldOfView = camStartFov;
                zoomActive = false;
                SimplePlayerController.CameraOverrideActive = false;
            }
        }
    }
}
