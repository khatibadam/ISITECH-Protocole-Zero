using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace ProtocoleZero
{
    /// <summary>
    /// Deplacement et interactions clavier/souris quand le jeu tourne sans casque.
    /// ZQSD-WASD pour marcher, clic droit pour regarder, A/E pour pivoter, F pour
    /// interagir avec l'objet vise (boutons, vanne, casier, disjoncteur, cables,
    /// lampe torche...), T pour allumer la lampe, G pour poser l'objet porte.
    /// Un reticule et un libelle contextuel discrets s'affichent a l'ecran hors VR.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimplePlayerController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float mouseSensitivity = 1.4f;
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private bool lockCursorInPlay;
        [Tooltip("Distance a laquelle un cable porte flotte devant la camera.")]
        [SerializeField, Min(0.2f)] private float plugCarryDistance = 0.55f;
        [SerializeField, Min(0f)] private float helpAutoHideSeconds = 12f;

        /// <summary>Quand vrai (jumpscare par exemple), la souris ne pilote plus la camera.</summary>
        public static bool CameraOverrideActive;

        /// <summary>Quand vrai (sequence de defaite), le joueur ne peut plus bouger.</summary>
        public static bool ControlsLocked;

        private CharacterController controller;
        private XROrigin xrOrigin;
        private float pitch;
        private Vector3 lastMovedPosition;
        private bool hasMoved;
        private Vector3 lastGroundedPosition;
        private bool hasGroundedPosition;

        private CablePlug carriedPlug;
        private Flashlight carriedFlashlight;
        private Flashlight cachedFlashlight;
        private string hoverLabel;
        private bool showHelp = true;
        private float helpTimer;
        private static GUIStyle overlayStyle;
        private MusicAnchorController musicAnchor;
        private float musicAnchorRetryAt;

        private struct InteractTarget
        {
            public string Label;
            public System.Action Action;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            xrOrigin = GetComponent<XROrigin>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            // Statiques : survivent au rechargement de scene, on repart toujours propre.
            CameraOverrideActive = false;
            ControlsLocked = false;
        }

        // Returns the transform whose horizontal facing should drive movement/turn.
        // Prefer the camera (head) so movement follows where the player looks.
        private Transform Head => playerCamera != null ? playerCamera.transform : transform;

        private void Start()
        {
            if (lockCursorInPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            helpTimer = helpAutoHideSeconds;
        }

        private void Update()
        {
            SyncAfterExternalTeleport();
            RescueIfFallenOutOfWorld();
            Move();
            Look();
            SnapTurn();
            DesktopInteractionUpdate();
        }

        private void LateUpdate()
        {
            Transform cam = Head;

            // Le cable porte flotte devant la camera, legerement decale vers la main droite.
            if (carriedPlug != null)
            {
                float dist = ClampCarryDistance(cam, plugCarryDistance);
                Vector3 pos = cam.position
                    + cam.forward * dist
                    + cam.right * 0.18f
                    - cam.up * 0.12f;
                carriedPlug.transform.SetPositionAndRotation(pos, cam.rotation);
            }

            // La lampe portee se tient en bas a droite, faisceau aligne sur le regard.
            if (carriedFlashlight != null)
            {
                float dist = ClampCarryDistance(cam, 0.42f);
                Vector3 pos = cam.position
                    + cam.forward * dist
                    + cam.right * 0.2f
                    - cam.up * 0.16f;
                carriedFlashlight.transform.SetPositionAndRotation(pos, cam.rotation);
            }
        }

        // Rapproche l'objet porte quand un mur est juste devant, pour eviter qu'il
        // traverse visuellement la paroi (les colliders de l'objet sont coupes).
        private static float ClampCarryDistance(Transform cam, float desired)
        {
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit wall, desired + 0.15f, ~0, QueryTriggerInteraction.Ignore))
            {
                return Mathf.Max(0.22f, wall.distance - 0.12f);
            }

            return desired;
        }

        // Safety net: if anything ever pushes the rig through the floor (accumulated
        // gravity velocity + thin colliders), snap back to the last grounded spot
        // instead of falling forever.
        private void RescueIfFallenOutOfWorld()
        {
            // Tout le niveau est a y ~ 0 : passer sous -1.5 m est toujours un bug,
            // on ramene le joueur immediatement au dernier sol connu.
            if (transform.position.y > -1.5f)
            {
                if (controller.isGrounded)
                {
                    lastGroundedPosition = transform.position;
                    hasGroundedPosition = true;
                }

                return;
            }

            Vector3 safe = hasGroundedPosition ? lastGroundedPosition : new Vector3(0f, 0.05f, -1.7f);
            controller.enabled = false;
            transform.position = new Vector3(safe.x, Mathf.Max(safe.y, 0.05f), safe.z);
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        // XRI teleportation (XRBodyGroundPosition) writes the rig position directly on the
        // Transform, but the CharacterController keeps its own cached physics position: our
        // per-frame controller.Move() would snap the rig right back, silently cancelling every
        // teleport. Push the external Transform change into the physics engine first.
        private void SyncAfterExternalTeleport()
        {
            if (hasMoved && (transform.position - lastMovedPosition).sqrMagnitude > 0.0004f)
            {
                Physics.SyncTransforms();
            }
        }

        private void Move()
        {
            Keyboard keyboard = Keyboard.current;
            Vector3 input = Vector3.zero;
            if (keyboard != null && !ControlsLocked)
            {
                if (keyboard.aKey.isPressed)
                {
                    input.x -= 1f;
                }
                if (keyboard.dKey.isPressed)
                {
                    input.x += 1f;
                }
                if (keyboard.sKey.isPressed)
                {
                    input.z -= 1f;
                }
                if (keyboard.wKey.isPressed)
                {
                    input.z += 1f;
                }
            }

            input = Vector3.ClampMagnitude(input, 1f);

            // Camera-relative horizontal movement: walk where you look, not where the rig root points.
            Vector3 fwd = Head.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Head.right; right.y = 0f; right.Normalize();
            Vector3 horizontal = (fwd * input.z + right * input.x) * moveSpeed;

            // Garde anti-vide : on refuse d'avancer vers une zone sans sol (trou dans
            // les colliders, bord de map). Sonde a un pas devant, vers le bas.
            if (horizontal.sqrMagnitude > 0.0001f)
            {
                Vector3 probe = transform.position
                    + controller.center
                    + horizontal.normalized * (controller.radius + 0.3f);
                float probeLength = controller.height * 0.5f + 3f;
                if (!Physics.Raycast(probe, Vector3.down, probeLength, ~0, QueryTriggerInteraction.Ignore))
                {
                    horizontal = Vector3.zero;
                }
            }

            Vector3 motion = horizontal;
            motion.y = Physics.gravity.y * 0.15f;
            controller.Move(motion * Time.deltaTime);
            lastMovedPosition = transform.position;
            hasMoved = true;
        }

        // Yaw the rig WITHOUT translating it: rotate around the camera position so the
        // offset head does not orbit the pivot (which previously caused drift into the void).
        private void YawAroundHead(float degrees)
        {
            if (Mathf.Abs(degrees) < 0.0001f)
            {
                return;
            }

            if (xrOrigin != null)
            {
                xrOrigin.RotateAroundCameraUsingOriginUp(degrees);
            }
            else
            {
                transform.RotateAround(Head.position, Vector3.up, degrees);
            }
        }

        private void Look()
        {
            if (CameraOverrideActive)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (playerCamera == null || mouse == null || !mouse.rightButton.isPressed)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * (mouseSensitivity * 0.08f);
            float yaw = delta.x;
            float lookY = delta.y;
            YawAroundHead(yaw);
            pitch = Mathf.Clamp(pitch - lookY, -75f, 75f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void SnapTurn()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || ControlsLocked)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                YawAroundHead(-45f);
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                YawAroundHead(45f);
            }
        }

        private void DesktopInteractionUpdate()
        {
            // En casque, on laisse les interactions au systeme VR (mains/manettes).
            if (XRSettings.isDeviceActive)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Sequence de mort : plus aucune interaction ni libelle contextuel.
            if (ControlsLocked)
            {
                hoverLabel = null;
                return;
            }

            if (helpTimer > 0f)
            {
                helpTimer -= Time.deltaTime;
                if (helpTimer <= 0f)
                {
                    showHelp = false;
                }
            }

            if (keyboard.hKey.wasPressedThisFrame)
            {
                showHelp = !showHelp;
                helpTimer = 0f;
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                ToggleFlashlight();
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                DropCarriedPlug();
                DropCarriedFlashlight();
            }

            bool resolved = TryResolveTarget(out InteractTarget target);
            hoverLabel = resolved ? target.Label : (carriedPlug != null ? "F : reposer le cable" : null);

            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (resolved && target.Action != null)
                {
                    target.Action();
                }
                else if (carriedPlug != null)
                {
                    DropCarriedPlug();
                }
            }
        }

        // Resout ce que vise la camera en une cible interactive (libelle + action F).
        // Couvre toutes les interactions VR du jeu pour que le parcours soit finissable
        // au clavier : boutons, poignees, vanne, casier, disjoncteur, bouton poussoir,
        // cables et prises, indice inspectable, lampe torche.
        private bool TryResolveTarget(out InteractTarget target)
        {
            target = default;
            if (playerCamera == null)
            {
                return false;
            }

            // RaycastAll trie par distance : les zones declencheurs invisibles (stress,
            // capteurs...) ne masquent pas les objets interactifs situes derriere.
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                if (ResolveFromCollider(hits[i].collider, ref target))
                {
                    return true;
                }

                if (!hits[i].collider.isTrigger)
                {
                    // Surface pleine non interactive : elle bloque la visee.
                    return false;
                }
            }

            return false;
        }

        private bool ResolveFromCollider(Collider collider, ref InteractTarget target)
        {
            // Cable en main : seules les prises repondent (F ailleurs = reposer).
            if (carriedPlug != null)
            {
                ElectricalSocket socket = collider.GetComponentInParent<ElectricalSocket>();
                if (socket == null)
                {
                    return false;
                }

                if (socket.IsSolved)
                {
                    target.Label = "Prise deja alimentee";
                    return true;
                }

                ElectricalSocket s = socket;
                target.Label = "F : brancher le cable";
                target.Action = () => PlugCarriedInto(s);
                return true;
            }

            InteractableButton button = collider.GetComponentInParent<InteractableButton>();
            if (button != null)
            {
                target.Label = "F : utiliser";
                target.Action = button.Activate;
                return true;
            }

            TutorialOptionButton tutorialButton = collider.GetComponentInParent<TutorialOptionButton>();
            if (tutorialButton != null)
            {
                target.Label = "F : choisir";
                target.Action = tutorialButton.Press;
                return true;
            }

            ElevatorCallButton elevatorButton = collider.GetComponentInParent<ElevatorCallButton>();
            if (elevatorButton != null)
            {
                target.Label = "F : appeler l'ascenseur";
                target.Action = elevatorButton.Press;
                return true;
            }

            RestartGameButton restartButton = collider.GetComponentInParent<RestartGameButton>();
            if (restartButton != null)
            {
                target.Label = "F : recommencer";
                target.Action = restartButton.Press;
                return true;
            }

            Flashlight flashlight = collider.GetComponentInParent<Flashlight>();
            if (flashlight != null)
            {
                Flashlight fl = flashlight;
                target.Label = "F : prendre la lampe (T : allumer en main, G : poser)";
                target.Action = () => PickUpFlashlight(fl);
                return true;
            }

            TwoHandDoorHandle handle = collider.GetComponentInParent<TwoHandDoorHandle>();
            if (handle != null)
            {
                target.Label = "F : actionner la poignee (les deux en 2 s)";
                target.Action = handle.Pulse;
                return true;
            }

            CablePlug plug = collider.GetComponentInParent<CablePlug>();
            if (plug != null && !plug.IsLocked)
            {
                CablePlug p = plug;
                target.Label = "F : prendre le cable";
                target.Action = () => PickUpPlug(p);
                return true;
            }

            ElectricalSocket emptySocket = collider.GetComponentInParent<ElectricalSocket>();
            if (emptySocket != null && !emptySocket.IsSolved)
            {
                target.Label = "Il faut un cable a brancher ici";
                return true;
            }

            ValveWheelInteractable valve = collider.GetComponentInParent<ValveWheelInteractable>();
            if (valve != null && !valve.IsOpen)
            {
                target.Label = "F : tourner la vanne";
                target.Action = valve.ForceOpen;
                return true;
            }

            SlidingDrawerInteractable drawer = collider.GetComponentInParent<SlidingDrawerInteractable>();
            if (drawer != null && !drawer.IsOpen)
            {
                target.Label = "F : ouvrir le casier";
                target.Action = drawer.ForceOpen;
                return true;
            }

            BreakerLever breaker = collider.GetComponentInParent<BreakerLever>();
            if (breaker != null && !breaker.IsArmed)
            {
                target.Label = "F : rearmer le disjoncteur";
                target.Action = breaker.ForceArm;
                return true;
            }

            PokeRearmButton pokeButton = collider.GetComponentInParent<PokeRearmButton>();
            if (pokeButton != null)
            {
                target.Label = "F : appuyer sur le bouton";
                target.Action = pokeButton.Press;
                return true;
            }

            InspectableObject inspectable = collider.GetComponentInParent<InspectableObject>();
            if (inspectable != null)
            {
                InspectableObject i = inspectable;
                target.Label = "F : inspecter l'indice";
                target.Action = () => i.DesktopInspect();
                return true;
            }

            return false;
        }

        private void PickUpPlug(CablePlug plug)
        {
            carriedPlug = plug;
            plug.BeginDesktopCarry();
        }

        private void DropCarriedPlug()
        {
            CablePlug plug = carriedPlug;
            carriedPlug = null;
            if (plug != null)
            {
                plug.EndDesktopCarry();
            }
        }

        private void PlugCarriedInto(ElectricalSocket socket)
        {
            CablePlug plug = carriedPlug;
            carriedPlug = null;
            if (plug == null)
            {
                return;
            }

            socket.TryAccept(plug);
            // Mauvaise prise : EndDesktopCarry renvoie le cable a sa position de depart.
            plug.EndDesktopCarry();
        }

        private void PickUpFlashlight(Flashlight flashlight)
        {
            // Une seule chose en main a la fois : on repose d'abord ce qu'on porte.
            DropCarriedPlug();
            DropCarriedFlashlight();
            carriedFlashlight = flashlight;
            flashlight.BeginDesktopCarry();
        }

        private void DropCarriedFlashlight()
        {
            Flashlight flashlight = carriedFlashlight;
            carriedFlashlight = null;
            if (flashlight == null)
            {
                return;
            }

            // Pose au sol juste devant le joueur (sonde vers le bas pour ne pas
            // traverser le plancher ni rester en l'air).
            Transform cam = Head;
            Vector3 forward = cam.forward; forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
            Vector3 dropStart = cam.position + forward * 0.55f;
            Vector3 dropPosition = dropStart;
            if (Physics.Raycast(dropStart, Vector3.down, out RaycastHit floorHit, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                dropPosition = floorHit.point + Vector3.up * 0.08f;
            }

            flashlight.EndDesktopCarry(dropPosition, Quaternion.LookRotation(forward, Vector3.up));
        }

        private void ToggleFlashlight()
        {
            if (carriedFlashlight != null)
            {
                carriedFlashlight.Toggle();
                return;
            }

            if (cachedFlashlight == null)
            {
                cachedFlashlight = FindFirstObjectByType<Flashlight>();
            }

            cachedFlashlight?.Toggle();
        }

        private void OnGUI()
        {
            // Overlay purement PC : inutile (et invisible correctement) dans le casque.
            if (XRSettings.isDeviceActive || Keyboard.current == null)
            {
                return;
            }

            // Pendant un screamer ou la sequence de mort, l'ecran reste nu :
            // aucun texte d'interface ne doit casser le choc.
            if (CameraOverrideActive || ControlsLocked)
            {
                return;
            }

            DrawMusicBar();

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(cx - 1.5f, cy - 1.5f, 3f, 3f), Texture2D.whiteTexture);
            GUI.color = previous;

            if (!string.IsNullOrEmpty(hoverLabel))
            {
                // Bandeau sombre + texte gras : lisible quelle que soit la scene derriere.
                EnsureOverlayStyle(15, TextAnchor.MiddleCenter, true);
                Vector2 size = overlayStyle.CalcSize(new GUIContent(hoverLabel));
                Rect box = new Rect(cx - size.x * 0.5f - 10f, cy + 18f, size.x + 20f, size.y + 8f);
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(box, Texture2D.whiteTexture);
                GUI.color = previous;
                DrawShadowLabel(new Rect(box.x, box.y + 4f, box.width, size.y), hoverLabel, 15, TextAnchor.MiddleCenter, true);
            }

            if (showHelp)
            {
                const string help = "ZQSD / WASD : se deplacer\n"
                    + "Clic droit + souris : regarder\n"
                    + "A / E : pivoter\n"
                    + "F : interagir avec l'objet vise\n"
                    + "T : lampe torche  |  G : poser l'objet\n"
                    + "H : masquer l'aide";
                DrawShadowLabel(new Rect(18f, Screen.height - 140f, 460f, 124f), help, 12, TextAnchor.LowerLeft);
            }
            else
            {
                DrawShadowLabel(new Rect(18f, Screen.height - 32f, 200f, 20f), "H : aide", 11, TextAnchor.LowerLeft);
            }
        }

        // Jauge en haut de l'ecran : temps de musique restant avant la veille du PC.
        // Verte puis orange puis rouge ; quand la musique dort, alerte rouge clignotante.
        private void DrawMusicBar()
        {
            if (musicAnchor == null)
            {
                if (Time.unscaledTime < musicAnchorRetryAt)
                {
                    return;
                }

                musicAnchorRetryAt = Time.unscaledTime + 2f;
                musicAnchor = FindFirstObjectByType<MusicAnchorController>();
                if (musicAnchor == null)
                {
                    return;
                }
            }

            // Rien avant le vrai depart de la partie (ecran tutoriel).
            if (!musicAnchor.RunStarted)
            {
                return;
            }

            float width = Mathf.Min(320f, Screen.width * 0.4f);
            const float height = 13f;
            float x = (Screen.width - width) * 0.5f;
            const float y = 14f;

            float total = Mathf.Max(1f, musicAnchor.AwakeDurationSeconds);
            float remaining = Mathf.Clamp(musicAnchor.RemainingSeconds, 0f, total);
            float fill = musicAnchor.IsMusicAwake ? remaining / total : 0f;
            bool blinkOn = Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.55f;

            Color previous = GUI.color;

            // Fond sombre (cadre) toujours visible.
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, width + 4f, height + 4f), Texture2D.whiteTexture);

            if (musicAnchor.IsMusicAwake && fill > 0f)
            {
                Color barColor = fill > 0.5f
                    ? new Color(0.3f, 0.85f, 0.5f, 0.9f)
                    : fill > 0.25f
                        ? new Color(0.95f, 0.7f, 0.2f, 0.9f)
                        : new Color(0.9f, 0.25f, 0.2f, 0.9f);

                // Sous 15 % la barre clignote pour pousser au retour vers le PC.
                if (fill >= 0.15f || blinkOn)
                {
                    GUI.color = barColor;
                    GUI.DrawTexture(new Rect(x, y, width * fill, height), Texture2D.whiteTexture);
                }
            }
            else if (!musicAnchor.IsMusicAwake && blinkOn)
            {
                // Musique en veille : la barre entiere pulse en rouge sombre.
                GUI.color = new Color(0.75f, 0.15f, 0.12f, 0.55f);
                GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            }

            GUI.color = previous;

            string label = musicAnchor.IsMusicAwake
                ? "MUSIQUE " + MissionTimer.Format(remaining)
                : (blinkOn ? "MUSIQUE EN VEILLE - RELANCE-LA AU PC (PLAY)" : string.Empty);
            if (!string.IsNullOrEmpty(label))
            {
                DrawShadowLabel(new Rect(x - 80f, y + height + 3f, width + 160f, 18f), label, 12, TextAnchor.UpperCenter, true);
            }
        }

        private static void EnsureOverlayStyle(int fontSize, TextAnchor anchor, bool bold)
        {
            if (overlayStyle == null)
            {
                overlayStyle = new GUIStyle(GUI.skin.label) { wordWrap = false, richText = false };
            }

            overlayStyle.fontSize = fontSize;
            overlayStyle.alignment = anchor;
            overlayStyle.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }

        private static void DrawShadowLabel(Rect rect, string text, int fontSize, TextAnchor anchor, bool bold = false)
        {
            EnsureOverlayStyle(fontSize, anchor, bold);
            overlayStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, overlayStyle);
            overlayStyle.normal.textColor = Color.white;
            GUI.Label(rect, text, overlayStyle);
        }
    }
}
