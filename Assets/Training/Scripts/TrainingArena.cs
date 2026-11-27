using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Webbox.Training
{
    public sealed class TrainingArena : MonoBehaviour
    {
        private enum ScreenMode { MainMenu, MapSelect, FighterSelect, Training }
        private enum MenuAction { None, Choose, Back, Confirm }

        [Serializable]
        private struct TrainingMap
        {
            [SerializeField] private string displayName;
            [SerializeField] private Sprite backdrop;

            public Sprite Backdrop => backdrop;
            public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? backdrop.name : displayName;
        }

        private const float Width = 960f;
        private const float Height = 540f;
        private const float MapMargin = 126f;
        private const float MapPreviewInset = 9f;
        private const float MapCaptionHeight = 49f;
        private static readonly Rect TrainingButton = new Rect(48, 246, 300, 60);
        private static readonly Rect TwoPlayerButton = new Rect(48, 318, 300, 60);
        private static readonly Rect BackButton = new Rect(32, 448, 146, 48);
        private static readonly Rect StartButton = new Rect(716, 448, 212, 48);
        private static readonly Rect MapBackButton = new Rect(MapMargin, 429, 128, 42);
        private static readonly Rect MapContinueButton = new Rect(Width - MapMargin - 176, 429, 176, 42);
        private static readonly Rect FullSprite = new Rect(0, 0, 1, 1);
        private static readonly Rect PortraitCrop = new Rect(0.34f, 0.48f, 0.43f, 0.49f);
        private static readonly Rect TargetCrop = new Rect(0.22f, 0, 0.56f, 1);
        private static readonly Color Paper = new Color32(253, 235, 203, 255);
        private static readonly Color Muted = new Color32(173, 153, 161, 255);
        private static readonly Color Ink = new Color32(7, 4, 13, 247);
        private static readonly Color Line = new Color32(47, 24, 44, 255);
        private static readonly Color Brass = new Color32(118, 48, 36, 255);
        private static readonly Color Mint = new Color32(114, 240, 198, 255);
        private static readonly Color Amber = new Color32(255, 191, 108, 255);
        private static readonly Color Coral = new Color32(255, 115, 100, 255);
        private static readonly Color MenuBackground = new Color32(10, 8, 15, 255);
        private static readonly Color MenuButtonBackground = ((Color)new Color32(26, 30, 48, 255)).linear;
        private static readonly Color MenuButtonHover = ((Color)new Color32(70, 38, 47, 255)).linear;
        private static readonly Color MenuButtonBorder = ((Color)new Color32(211, 153, 88, 255)).linear;
        private static readonly Color MenuLockedBorder = ((Color)new Color32(64, 55, 76, 255)).linear;
        private static readonly Color MenuKeyBackground = ((Color)new Color32(14, 17, 30, 255)).linear;
        private static readonly Color PreviewBackground = new Color32(28, 20, 41, 255);

        [SerializeField] private Camera stageCamera;
        [SerializeField] private TrainingFighter fighter;
        [SerializeField] private TrainingTarget target;
        [SerializeField] private Transform leftBoundary;
        [SerializeField] private Transform rightBoundary;
        [SerializeField] private FighterSpritePack[] selectableFighters;
        [SerializeField] private SpriteRenderer menuBackdrop;
        [SerializeField] private SpriteRenderer stageBackdrop;
        [SerializeField] private TrainingMap[] selectableMaps;

        private FighterSpritePack[] roster;
        private TrainingMap[] maps;
        private int selectedFighter;
        private int selectedMap;
        private ScreenMode screen;
        private (MenuAction Action, int Index) mousePress;
        private bool showBoxes;
        private GUIStyle label;
        private Rect viewport;

        private string CurrentMapName => maps.Length > 0 ? maps[selectedMap].DisplayName : "NIGHT MARKET ROOFTOP";
        private Rect CurrentBackButton => screen == ScreenMode.MapSelect ? MapBackButton : BackButton;
        private Rect CurrentConfirmButton => screen == ScreenMode.MapSelect ? MapContinueButton : StartButton;

        private void Start()
        {
            roster = selectableFighters == null ? Array.Empty<FighterSpritePack>()
                : Array.FindAll(selectableFighters, pack => pack != null);
            maps = selectableMaps == null || stageBackdrop == null ? Array.Empty<TrainingMap>()
                : Array.FindAll(selectableMaps, map => map.Backdrop != null);
            selectedFighter = Array.IndexOf(roster, fighter.SpritePack);
            if (selectedFighter < 0 || roster[selectedFighter].IdlePose == null)
                selectedFighter = Array.FindIndex(roster, pack => pack.IdlePose != null);
            if (menuBackdrop != null && menuBackdrop.sprite != null)
            {
                Vector2 size = menuBackdrop.sprite.bounds.size;
                float viewHeight = 2f * stageCamera.orthographicSize;
                float scale = Mathf.Max(viewHeight * Width / Height / size.x, viewHeight / size.y);
                menuBackdrop.transform.localScale = new Vector3(scale, scale, 1f);
            }
            SetScreen(ScreenMode.MainMenu);
            UpdateViewport();
        }

        private void Update()
        {
            UpdateViewport();

            Keyboard keyboard = Keyboard.current;
            if (screen != ScreenMode.Training)
            {
                UpdateMenu(keyboard);
                return;
            }

            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetScreen(selectedFighter >= 0 ? ScreenMode.FighterSelect :
                    maps.Length > 0 ? ScreenMode.MapSelect : ScreenMode.MainMenu);
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
            if (keyboard != null)
            {
                if (screen == ScreenMode.MapSelect || screen == ScreenMode.FighterSelect)
                {
                    if (keyboard.escapeKey.wasPressedThisFrame)
                    {
                        ApplyMenuAction(MenuAction.Back, 0);
                        return;
                    }

                    int next = keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ? 1 : 0;
                    int previous = keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame ? 1 : 0;
                    if (screen == ScreenMode.MapSelect)
                        selectedMap = (selectedMap + next - previous + maps.Length) % maps.Length;
                    else if (next != previous)
                        MoveFighterSelection(next - previous);
                }

                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    ApplyMenuAction(MenuAction.Confirm, 0);
                    return;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.leftButton.wasPressedThisFrame)
                mousePress = GetMenuAction(mouse);

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                var release = GetMenuAction(mouse);
                if (mousePress == release)
                    ApplyMenuAction(release.Action, release.Index);
                mousePress = default;
            }
        }

        private void MoveFighterSelection(int direction)
        {
            int index = selectedFighter;
            do
            {
                index = (index + direction + roster.Length) % roster.Length;
            } while (roster[index].IdlePose == null);
            selectedFighter = index;
        }

        private (MenuAction Action, int Index) GetMenuAction(Mouse mouse)
        {
            if (mouse == null || viewport.width <= 0f)
                return default;

            Vector2 pointer = mouse.position.ReadValue();
            float scale = viewport.width / Width;
            Vector2 position = new Vector2(pointer.x - viewport.x, Screen.height - pointer.y - viewport.y) / scale;
            if (screen == ScreenMode.MainMenu)
                return TrainingButton.Contains(position) ? (MenuAction.Confirm, 0) : default;

            if (CurrentBackButton.Contains(position))
                return (MenuAction.Back, 0);
            if (CurrentConfirmButton.Contains(position))
                return (MenuAction.Confirm, 0);
            bool selectingMap = screen == ScreenMode.MapSelect;
            int choiceCount = selectingMap ? maps.Length : roster.Length;
            for (int i = 0; i < choiceCount; i++)
            {
                if (!selectingMap && roster[i].IdlePose == null)
                    continue;
                Rect choice = selectingMap ? MapCard(i) : FighterTile(i);
                if (choice.Contains(position))
                    return (MenuAction.Choose, i);
            }
            return default;
        }

        private void ApplyMenuAction(MenuAction action, int index)
        {
            switch (action)
            {
                case MenuAction.Choose:
                    if (screen == ScreenMode.MapSelect)
                        selectedMap = index;
                    else if (roster[index].IdlePose != null)
                        selectedFighter = index;
                    break;
                case MenuAction.Back:
                    SetScreen(screen == ScreenMode.FighterSelect && maps.Length > 0
                        ? ScreenMode.MapSelect : ScreenMode.MainMenu);
                    break;
                case MenuAction.Confirm:
                    if (screen == ScreenMode.MainMenu && maps.Length > 0)
                        SetScreen(ScreenMode.MapSelect);
                    else if (screen != ScreenMode.FighterSelect && selectedFighter >= 0)
                        SetScreen(ScreenMode.FighterSelect);
                    else
                        StartTraining();
                    break;
            }
        }

        private void StartTraining()
        {
            if (maps.Length > 0)
                stageBackdrop.sprite = maps[selectedMap].Backdrop;
            if (selectedFighter >= 0)
                fighter.SelectSpritePack(roster[selectedFighter]);
            ResetTraining();
            showBoxes = false;
            SetScreen(ScreenMode.Training);
        }

        private void SetScreen(ScreenMode next)
        {
            screen = next;
            mousePress = default;
            if (menuBackdrop != null)
                menuBackdrop.enabled = next == ScreenMode.MainMenu && menuBackdrop.sprite != null;
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

            Color letterbox = screen == ScreenMode.Training ? Ink : MenuBackground;
            Fill(0, 0, Screen.width, viewport.yMin, letterbox);
            Fill(0, viewport.yMax, Screen.width, Screen.height - viewport.yMax, letterbox);
            Fill(0, viewport.yMin, viewport.xMin, viewport.height, letterbox);
            Fill(viewport.xMax, viewport.yMin, Screen.width - viewport.xMax, viewport.height, letterbox);

            float scale = viewport.width / Width;
            GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y, 0), Quaternion.identity,
                new Vector3(scale, scale, 1f));

            if (screen == ScreenMode.Training)
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
            else if (screen == ScreenMode.FighterSelect)
            {
                DrawFighterSelect();
            }
            else if (screen == ScreenMode.MapSelect)
            {
                DrawMapSelect();
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
            if (menuBackdrop == null || !menuBackdrop.enabled)
                Fill(0, 0, Width, Height, MenuBackground);

            Fill(48, 144, 48, 3, Amber);
            Text(46, 153, 304, 76, "GRIDLINE", 54, Paper, true);

            bool hovered = GetMenuAction(Mouse.current).Action == MenuAction.Confirm;
            DrawModeButton(TrainingButton, "TRAINING RANGE", "ENTER", true, hovered);
            DrawModeButton(TwoPlayerButton, "TWO PLAYERS", "LOCKED", false, false);
        }

        private void DrawModeButton(Rect button, string title, string hint, bool available, bool hovered)
        {
            Fill(button.x, button.y, button.width, button.height, available ? MenuButtonBorder : MenuLockedBorder);
            Fill(button.x + 3, button.y + 2, button.width - 5, button.height - 4,
                hovered ? MenuButtonHover : MenuButtonBackground);
            Text(button.x + 16, button.y, button.width - 96, button.height, title, 19,
                available ? Paper : Muted, true);
            Fill(button.xMax - 81, button.center.y - 13, 65, 26, MenuKeyBackground);
            Text(button.xMax - 81, button.center.y - 13, 65, 26, hint, 12, available ? Amber : Muted, true,
                TextAnchor.MiddleCenter);
        }

        private Rect FighterTile(int index)
        {
            const float gap = 8f;
            int columns = Mathf.Min(6, roster.Length);
            int rows = (roster.Length + columns - 1) / columns;
            float size = Mathf.Min(68f, (148f - gap * (rows - 1)) / rows);
            float stride = size + gap;
            float totalWidth = columns * stride - gap;
            return new Rect((Width - totalWidth) / 2 + index % columns * stride, 340 + index / columns * stride,
                size, size);
        }

        private Rect MapCard(int index)
        {
            const float gap = 34f;
            float width = Mathf.Min(337f, (Width - 2 * MapMargin - gap * (maps.Length - 1)) / maps.Length);
            float totalWidth = maps.Length * (width + gap) - gap;
            float previewHeight = (width - 2 * MapPreviewInset) * Height / Width;
            return new Rect((Width - totalWidth) / 2 + index * (width + gap), 138f, width,
                MapPreviewInset + previewHeight + MapCaptionHeight);
        }

        private void DrawMapSelect()
        {
            DrawSelectionHeader("CHOOSE A STAGE", "TRAINING RANGE", MapMargin, 33f);
            var hover = GetMenuAction(Mouse.current);
            for (int i = 0; i < maps.Length; i++)
            {
                Rect card = MapCard(i);
                bool selected = i == selectedMap;
                bool hovered = hover.Action == MenuAction.Choose && hover.Index == i;
                Fill(card.x, card.y, card.width, card.height, selected ? Amber : hovered ? Muted : Line);
                Fill(card.x + 2, card.y + 2, card.width - 4, card.height - 4, Ink);
                float previewWidth = card.width - 2 * MapPreviewInset;
                float captionTop = card.yMax - MapCaptionHeight;
                DrawSpritePreview(maps[i].Backdrop,
                    new Rect(card.x + MapPreviewInset, card.y + MapPreviewInset, previewWidth,
                        card.height - MapPreviewInset - MapCaptionHeight), FullSprite);
                Fill(card.x + MapPreviewInset, captionTop + 4, previewWidth, 1, selected ? Brass : Line);
                Text(card.x + 15, captionTop + 5, card.width - 30, MapCaptionHeight - 7, maps[i].DisplayName, 17,
                    selected ? Paper : Muted, true);
            }

            DrawSelectionButtons("ENTER   CONTINUE", hover.Action, 477f);
        }

        private void DrawFighterSelect()
        {
            DrawSelectionHeader("CHOOSE A FIGHTER", CurrentMapName);

            FighterSpritePack selection = roster[selectedFighter];
            DrawPreviewPanel(new Rect(32, 94, 548, 222), selection.IdlePose, PortraitCrop,
                selection.DisplayName, true);
            DrawPreviewPanel(new Rect(594, 94, 334, 222), target.PreviewSprite, TargetCrop,
                "TRAINING TARGET", false);

            Fill(32, 330, 896, 191, PreviewBackground);
            Fill(32, 330, 896, 2, Line);
            var hover = GetMenuAction(Mouse.current);
            for (int i = 0; i < roster.Length; i++)
            {
                Rect tile = FighterTile(i);
                bool selected = i == selectedFighter;
                bool hovered = hover.Action == MenuAction.Choose && hover.Index == i;
                Sprite portrait = roster[i].IdlePose;
                Fill(tile.x, tile.y, tile.width, tile.height, selected ? Amber : hovered ? Muted : Line);
                Fill(tile.x + 2, tile.y + 2, tile.width - 4, tile.height - 4, MenuBackground);
                if (portrait != null)
                    DrawSpritePreview(portrait, new Rect(tile.x + 4, tile.y + 4, tile.width - 8, tile.height - 8),
                        PortraitCrop);
                else
                    Text(tile.x, tile.y, tile.width, tile.height, "EMPTY", 10, Muted, true, TextAnchor.MiddleCenter);
                if (selected)
                    DrawP1(tile.x + 2, tile.y + 2, 24, 19, 12);
            }

            DrawSelectionButtons("ENTER   START TRAINING", hover.Action, 491f);
        }

        private void DrawSelectionHeader(string heading, string context, float margin = 32f, float top = 23f)
        {
            Fill(0, 0, Width, Height, MenuBackground);
            Text(margin, top, Width - 2 * margin - 276, 54, heading, 33, Paper, true);
            Text(Width - margin - 308, top + 1, 308, 53, context, 15, Amber, true, TextAnchor.MiddleRight);
            Fill(margin, top + 55, Width - 2 * margin, 2, Brass);
        }

        private void DrawSelectionButtons(string confirmLabel, MenuAction hover, float hintY)
        {
            DrawSelectButton(CurrentBackButton, "ESC   BACK", hover == MenuAction.Back, false);
            DrawSelectButton(CurrentConfirmButton, confirmLabel, hover == MenuAction.Confirm, true);
            Text(258, hintY, 444, 26, "LEFT / RIGHT  OR  A / D", 12, Muted, true, TextAnchor.MiddleCenter);
        }

        private void DrawPreviewPanel(Rect panel, Sprite sprite, Rect crop, string title, bool player)
        {
            const float captionHeight = 57f;
            float captionTop = panel.yMax - captionHeight;
            Fill(panel.x, panel.y, panel.width, panel.height, PreviewBackground);
            Fill(panel.x, panel.y, panel.width, 3, player ? Amber : Line);
            DrawSpritePreview(sprite,
                new Rect(panel.x + 12, panel.y + 14, panel.width - 24, panel.height - captionHeight - 16), crop);
            Fill(panel.x, captionTop - 2, panel.width, 2, player ? Brass : Line);
            Fill(panel.x, captionTop, panel.width, captionHeight, Ink);
            Text(panel.x + 12, captionTop, panel.width - 24, captionHeight, title, player ? 27 : 20,
                player ? Paper : Muted, true, TextAnchor.MiddleCenter);
            if (player)
                DrawP1(panel.x + 16, panel.y + 17, 48, 30, 17);
        }

        private void DrawP1(float x, float y, float width, float height, int size)
        {
            Fill(x, y, width, height, Amber);
            Text(x, y, width, height, "P1", size, Ink, true, TextAnchor.MiddleCenter);
        }

        private static void DrawSpritePreview(Sprite sprite, Rect area, Rect crop)
        {
            if (sprite == null)
                return;

            Rect source = sprite.rect;
            source = new Rect(source.x + crop.x * source.width, source.y + crop.y * source.height,
                crop.width * source.width, crop.height * source.height);
            Texture2D texture = sprite.texture;
            float scale = Mathf.Min(area.width / source.width, area.height / source.height);
            float width = source.width * scale;
            float height = source.height * scale;
            Rect destination = new Rect(area.center.x - width / 2f, area.yMax - height, width, height);
            Rect uv = new Rect(source.x / texture.width, source.y / texture.height,
                source.width / texture.width, source.height / texture.height);
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(destination, texture, uv);
        }

        private void DrawSelectButton(Rect button, string title, bool hovered, bool primary)
        {
            Fill(button.x, button.y, button.width, button.height, hovered || primary ? Amber : Brass);
            if (!primary || !hovered)
                Fill(button.x + 2, button.y + 2, button.width - 4, button.height - 4, primary ? Brass : Line);
            Text(button.x + 4, button.y, button.width - 8, button.height, title, 14,
                primary && hovered ? Ink : Paper, true, TextAnchor.MiddleCenter);
        }

        private void DrawHeader()
        {
            Fill(28, 23, 50, 3, Amber);
            Fill(81, 23, 12, 3, Brass);
            Text(30, 35, 308, 43, "GRIDLINE", 35, Ink, true);
            Text(27, 32, 308, 43, "GRIDLINE", 35, Paper, true);
            Fill(28, 76, 206, 22, Ink);
            Fill(28, 76, 2, 22, Amber);
            Text(37, 76, 190, 22, CurrentMapName, 13, Amber, true);

            Fill(683, 25, 249, 73, Ink);
            Fill(680, 22, 252, 3, Brass);
            Fill(680, 22, 3, 73, Amber);
            Fill(683, 95, 249, 2, Brass);
            Fill(808, 106, 124, 22, Ink);
            Text(808, 106, 124, 22, selectedFighter >= 0 || maps.Length > 0 ? "ESC   SELECT" : "ESC   MAIN MENU", 11, Paper, true,
                TextAnchor.MiddleCenter);
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
