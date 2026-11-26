using UnityEngine;
using UnityEngine.InputSystem;

namespace Webbox.Training
{
    public sealed class TrainingArena : MonoBehaviour
    {
        private const float Width = 960f;
        private const float Height = 540f;
        private static readonly Rect TrainingButton = new Rect(70, 312, 326, 56);
        private static readonly Color Paper = new Color32(253, 235, 203, 255);
        private static readonly Color Muted = new Color32(173, 153, 161, 255);
        private static readonly Color Ink = new Color32(7, 4, 13, 247);
        private static readonly Color Line = new Color32(47, 24, 44, 255);
        private static readonly Color Brass = new Color32(118, 48, 36, 255);
        private static readonly Color Mint = new Color32(114, 240, 198, 255);
        private static readonly Color Amber = new Color32(255, 191, 108, 255);
        private static readonly Color Coral = new Color32(255, 115, 100, 255);
        private static readonly Color MenuShade = new Color32(7, 4, 13, 132);

        [SerializeField] private Camera stageCamera;
        [SerializeField] private TrainingFighter fighter;
        [SerializeField] private TrainingTarget target;
        [SerializeField] private Transform leftBoundary;
        [SerializeField] private Transform rightBoundary;

        private bool isTraining;
        private bool menuClickArmed;
        private bool showBoxes;
        private GUIStyle label;
        private Rect viewport;

        private void Start() => UpdateViewport();

        private void Update()
        {
            UpdateViewport();

            Keyboard keyboard = Keyboard.current;
            if (!isTraining)
            {
                UpdateMenu(keyboard);
                return;
            }

            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                isTraining = false;
                return;
            }

            if (keyboard.hKey.wasPressedThisFrame)
                showBoxes = !showBoxes;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ResetTraining();
                return;
            }

            int direction = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float deltaTime = Time.deltaTime;
            if (!fighter.HitPaused)
                target.Step(deltaTime);

            fighter.Step(deltaTime, direction, keyboard.jKey.wasPressedThisFrame, keyboard.kKey.wasPressedThisFrame,
                keyboard.wKey.wasPressedThisFrame, keyboard.sKey.isPressed,
                keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed,
                leftBoundary.position.x, rightBoundary.position.x, target);
        }

        private void UpdateMenu(Keyboard keyboard)
        {
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                StartTraining();
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.leftButton.wasPressedThisFrame)
                menuClickArmed = IsOverTrainingButton(mouse);

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (menuClickArmed && IsOverTrainingButton(mouse))
                    StartTraining();

                menuClickArmed = false;
            }
        }

        private bool IsOverTrainingButton(Mouse mouse)
        {
            if (mouse == null || viewport.width <= 0f)
                return false;

            Vector2 pointer = mouse.position.ReadValue();
            float scale = viewport.width / Width;
            Vector2 position = new Vector2(pointer.x - viewport.x, Screen.height - pointer.y - viewport.y) / scale;
            return TrainingButton.Contains(position);
        }

        private void StartTraining()
        {
            ResetTraining();
            showBoxes = false;
            menuClickArmed = false;
            isTraining = true;
        }

        private void ResetTraining()
        {
            fighter.ResetFighter();
            target.ResetTarget();
        }

        private void UpdateViewport()
        {
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            float width = Width * scale;
            float height = Height * scale;
            viewport = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            if (Screen.width > 0 && Screen.height > 0 && stageCamera.pixelRect != viewport)
                stageCamera.pixelRect = viewport;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || viewport.width <= 0f)
                return;
            label ??= new GUIStyle(GUI.skin.label) { clipping = TextClipping.Clip };

            Matrix4x4 originalMatrix = GUI.matrix;
            Color originalColor = GUI.color;

            Fill(0, 0, Screen.width, viewport.yMin, Ink);
            Fill(0, viewport.yMax, Screen.width, Screen.height - viewport.yMax, Ink);
            Fill(0, viewport.yMin, viewport.xMin, viewport.height, Ink);
            Fill(viewport.xMax, viewport.yMin, Screen.width - viewport.xMax, viewport.height, Ink);

            float scale = viewport.width / Width;
            GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y, 0), Quaternion.identity,
                new Vector3(scale, scale, 1f));

            if (isTraining)
            {
                DrawHeader();
                DrawTargetMarker();
                if (showBoxes)
                {
                    DrawWorldBox(target.Hurtbox, Mint, "HURT");
                    if (fighter.Phase == TrainingFighter.PunchPhase.Active)
                        DrawWorldBox(fighter.Hitbox, Coral, "HIT");
                }
                DrawFooter();
            }
            else
            {
                DrawMenu();
            }

            GUI.matrix = originalMatrix;
            GUI.color = originalColor;
        }

        private void DrawMenu()
        {
            Fill(0, 0, Width, Height, MenuShade);
            Fill(46, 138, 374, 258, Ink);
            Fill(46, 138, 374, 2, Brass);
            Fill(46, 138, 3, 258, Amber);
            Fill(70, 181, 58, 3, Amber);
            Fill(132, 181, 13, 3, Brass);
            Text(70, 196, 326, 68, "GRIDLINE", 58, Paper, true);
            Text(73, 264, 300, 24, "BOXING", 16, Muted, true);

            Rect button = TrainingButton;
            bool hovered = IsOverTrainingButton(Mouse.current);
            Color outline = hovered ? Amber : Brass;
            Fill(button.x, button.y, button.width, button.height, outline);
            Fill(button.x + 3, button.y + 2, button.width - 5, button.height - 4, hovered ? Brass : Line);
            Fill(button.x + 3, button.y, 50, 2, Amber);
            Text(button.x + 14, button.y, 220, button.height, "TRAINING ROOM", 21, Paper, true);
            Fill(button.xMax - 79, button.y + 14, 66, 28, Ink);
            Text(button.xMax - 79, button.y + 14, 66, 28, "ENTER", 12, Amber, true, TextAnchor.MiddleCenter);
        }

        private void DrawHeader()
        {
            Fill(28, 23, 50, 3, Amber);
            Fill(81, 23, 12, 3, Brass);
            Text(30, 35, 308, 43, "GRIDLINE", 35, Ink, true);
            Text(27, 32, 308, 43, "GRIDLINE", 35, Paper, true);
            Fill(28, 76, 206, 22, Ink);
            Fill(28, 76, 2, 22, Amber);
            Text(37, 76, 190, 22, "NIGHT MARKET ROOFTOP", 13, Amber, true);

            Fill(683, 25, 249, 73, Ink);
            Fill(680, 22, 252, 3, Brass);
            Fill(680, 22, 3, 73, Amber);
            Fill(683, 95, 249, 2, Brass);
            Fill(808, 106, 124, 22, Ink);
            Text(808, 106, 124, 22, "ESC   MAIN MENU", 12, Paper, true, TextAnchor.MiddleCenter);
            Text(694, 28, 160, 20, "TARGET HEALTH", 13, Muted, true);
            Text(694, 46, 220, 24, $"{target.Health} / {target.MaxHealth}", 20, Paper, true,
                TextAnchor.MiddleRight);
            float healthRatio = (float)target.Health / target.MaxHealth;
            for (int segment = 0; segment < 10; segment++)
            {
                float x = 696 + segment * 22;
                float filled = 18 * Mathf.Clamp01(healthRatio * 10 - segment);
                Fill(x, 76, 18, 9, Line);
                Fill(x, 76, filled, 9, Amber);
                Fill(x, 83, filled, 2, Brass);
            }
        }

        private void DrawTargetMarker()
        {
            Rect hurtbox = target.Hurtbox;
            Vector2 anchor = Project(new Vector2(hurtbox.center.x, hurtbox.yMax));
            Fill(anchor.x - 29, anchor.y - 45, 58, 22, Ink);
            Text(anchor.x - 29, anchor.y - 45, 58, 22, "TARGET", 11, Amber, true, TextAnchor.MiddleCenter);
            Fill(anchor.x - 1, anchor.y - 23, 2, 10, Amber);
        }

        private void DrawFooter()
        {
            Fill(0, 465, Width, 75, Ink);
            Fill(0, 465, Width, 2, Brass);
            Key(28, 476, "A / D", "MOVE", 43);
            Key(160, 476, "J", "LIGHT PUNCH", 24);
            Key(304, 476, "K", "HEAVY PUNCH", 24);
            Key(448, 476, "R", "RESET", 24);
            Key(28, 506, "W", "JUMP", 43);
            Key(160, 506, "S", "CROUCH", 24);
            Key(304, 506, "SHIFT", "GUARD", 47);
            Key(448, 506, "H", "HITBOXES", 24, showBoxes);

            Fill(570, 479, 1, 46, Line);
            string punch = fighter.CurrentPunch switch
            {
                TrainingFighter.PunchKind.Light => "LIGHT PUNCH",
                TrainingFighter.PunchKind.Hook => "HOOK",
                TrainingFighter.PunchKind.Heavy => "HEAVY PUNCH",
                _ => "J  J  K   COMBO"
            };
            Text(592, 477, 145, 17, punch, 11, Muted, true);
            var phase = fighter.Phase;
            string state = phase == TrainingFighter.PunchPhase.Ready ? fighter.CurrentStance switch
            {
                TrainingFighter.Stance.Idle => "READY",
                TrainingFighter.Stance.Walk => "MOVE",
                _ => fighter.CurrentStance.ToString().ToUpperInvariant()
            } : phase.ToString().ToUpperInvariant();
            Color phaseColor = phase == TrainingFighter.PunchPhase.Active ? Coral : Mint;
            Text(592, 495, 155, 27, state, 18, phaseColor, true);
            for (int i = 1; i <= 3; i++)
                Fill(750 + (i - 1) * 25, 508, 21, 4, (int)phase == i ? phaseColor : Line);

            if (fighter.CanLink)
            {
                string followUp = fighter.FollowUpQueued ? "LINK QUEUED" : $"LINK   {fighter.FollowUpKey}";
                Text(745, 477, 120, 17, followUp, 11, fighter.FollowUpQueued ? Mint : Muted, true);
            }

            string result = fighter.ComboHits >= 2 ? $"{fighter.ComboHits} HITS" : fighter.LastResult;
            if (!string.IsNullOrEmpty(result))
                Text(839, 495, 93, 27, result, 18, result == "MISS" ? Muted : Amber, true,
                    TextAnchor.MiddleRight);
        }

        private void Key(float x, float y, string key, string action, float keyWidth, bool highlighted = false)
        {
            Color color = highlighted ? Mint : Paper;
            Fill(x, y, keyWidth, 22, Line);
            Fill(x + 1, y + 1, keyWidth - 2, 20, Ink);
            Text(x, y, keyWidth, 22, key, 12, color, true, TextAnchor.MiddleCenter);
            Text(x + keyWidth + 8, y, 110, 22, action, 12, highlighted ? Mint : Muted, true);
        }

        private void DrawWorldBox(Rect box, Color color, string name)
        {
            Vector2 topLeft = Project(new Vector2(box.xMin, box.yMax));
            Vector2 bottomRight = Project(new Vector2(box.xMax, box.yMin));
            Rect rect = Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
            Color fill = color;
            fill.a = 0.2f;
            Fill(rect.x, rect.y, rect.width, rect.height, fill);
            Fill(rect.x, rect.y, rect.width, 2, color);
            Fill(rect.x, rect.yMax - 2, rect.width, 2, color);
            Fill(rect.x, rect.y, 2, rect.height, color);
            Fill(rect.xMax - 2, rect.y, 2, rect.height, color);
            Fill(rect.x, rect.y - 19, 40, 17, Ink);
            Text(rect.x, rect.y - 19, 40, 17, name, 11, color, true, TextAnchor.MiddleCenter);
        }

        private Vector2 Project(Vector2 world)
        {
            Vector3 point = stageCamera.WorldToViewportPoint(world);
            return new Vector2(point.x * Width, (1 - point.y) * Height);
        }

        private static void Fill(float x, float y, float w, float h, Color color)
        {
            if (w <= 0f || h <= 0f)
                return;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        }

        private void Text(float x, float y, float w, float h, string text, int size, Color color,
            bool bold, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            GUI.color = Color.white;
            label.normal.textColor = color;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = anchor;
            GUI.Label(new Rect(x, y, w, h), text, label);
        }
    }
}
