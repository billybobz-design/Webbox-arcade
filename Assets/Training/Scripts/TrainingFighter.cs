using System;
using UnityEngine;

namespace Webbox.Training
{
    public sealed class TrainingFighter : MonoBehaviour
    {
        public enum PunchPhase { Ready, Windup, Active, Recovery }
        public enum PunchKind { None, Light, Hook, Heavy }
        public enum Stance { Idle, Walk, Guard, Crouch, Jump, Land }

        private const float ComboCancelPoint = 0.6f;
        private const float ComboDisplaySeconds = 0.9f;
        private const float CrouchPoseSeconds = 0.09f;

        [Serializable]
        private struct Punch
        {
            [Min(0.01f)] public float windup;
            [Min(0.01f)] public float active;
            [Min(0.01f)] public float recovery;
            [Min(1)] public int damage;
            [Min(0f)] public float hitStop;
            public Vector2 offset;
            public Vector2 size;

            public Punch(float windup, float active, float recovery, int damage, float hitStop,
                Vector2 offset, Vector2 size)
            {
                this.windup = windup;
                this.active = active;
                this.recovery = recovery;
                this.damage = damage;
                this.hitStop = hitStop;
                this.offset = offset;
                this.size = size;
            }
        }

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4.2f;
        [SerializeField, Min(0f)] private float bodyHalfWidth = 0.42f;
        [SerializeField, Min(0.1f)] private float jumpSeconds = 0.58f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.25f;
        [SerializeField, Min(0.01f)] private float landingSeconds = 0.1f;

        [Header("Sprites")]
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private FighterSpritePack spritePack;
        [SerializeField, Min(1f)] private float idleFramesPerSecond = 3f;
        [SerializeField, Min(1f)] private float walkFramesPerSecond = 12f;

        [Header("Light punch")]
        [SerializeField, Min(0.01f)] private float windupSeconds = 0.08f;
        [SerializeField, Min(0.01f)] private float activeSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float recoverySeconds = 0.15f;
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField, Min(0f)] private float lightHitStop = 0.035f;
        [SerializeField] private Vector2 hitOffset = new Vector2(1.04f, 1.55f);
        [SerializeField] private Vector2 hitSize = new Vector2(0.88f, 0.46f);

        [Header("Follow-up punches")]
        [SerializeField] private Punch hook = new Punch(0.10f, 0.085f, 0.16f, 12, 0.05f,
            new Vector2(0.98f, 1.36f), new Vector2(0.78f, 0.52f));
        [SerializeField] private Punch heavy = new Punch(0.17f, 0.08f, 0.25f, 20, 0.075f,
            new Vector2(1.15f, 1.67f), new Vector2(0.83f, 0.62f));

        private Vector3 spawnPosition;
        private Sprite fallbackSprite;
        private SpriteRenderer airShadow;
        private Punch punch;
        private int facing = 1;
        private float punchTime;
        private float poseTime;
        private float hitStopTime;
        private float comboDisplayTime;
        private bool followUpQueued;
        private bool landed;

        public PunchPhase Phase { get; private set; }
        public PunchKind CurrentPunch { get; private set; }
        public Stance CurrentStance { get; private set; }
        public int ComboHits { get; private set; }
        internal FighterSpritePack SpritePack => spritePack;
        public string LastResult { get; private set; }
        public bool HitPaused => hitStopTime > 0f;
        public bool FollowUpQueued => followUpQueued;
        public string FollowUpKey => NextLink == PunchKind.Hook ? "J" : "K";
        public bool CanLink => landed && NextLink != PunchKind.None &&
            (Phase == PunchPhase.Active || Phase == PunchPhase.Recovery);
        public Rect Hitbox => new Rect(
            (Vector2)transform.position + new Vector2(facing * punch.offset.x, punch.offset.y) - punch.size / 2f,
            punch.size);

        private PunchKind NextLink => CurrentPunch switch
        {
            PunchKind.Light => PunchKind.Hook,
            PunchKind.Hook => PunchKind.Heavy,
            _ => PunchKind.None
        };

        private void Awake()
        {
            spawnPosition = transform.position;
            fallbackSprite = artwork.sprite;
            SelectSpritePack(spritePack);
            ResetFighter();
        }

        internal void SelectSpritePack(FighterSpritePack pack)
        {
            spritePack = pack;
            Sprite shadowSprite = pack != null ? pack.AirShadow : null;
            if (airShadow == null && shadowSprite != null)
            {
                airShadow = new GameObject("Air Shadow").AddComponent<SpriteRenderer>();
                airShadow.transform.SetParent(transform, false);
                airShadow.sharedMaterial = artwork.sharedMaterial;
                airShadow.sortingLayerID = artwork.sortingLayerID;
                airShadow.sortingOrder = artwork.sortingOrder - 1;
            }
            if (airShadow != null)
                airShadow.sprite = shadowSprite;
        }

        public void ResetFighter()
        {
            transform.position = spawnPosition;
            facing = 1;
            punchTime = poseTime = hitStopTime = comboDisplayTime = 0f;
            followUpQueued = landed = false;
            Phase = PunchPhase.Ready;
            CurrentPunch = PunchKind.None;
            CurrentStance = Stance.Idle;
            ComboHits = 0;
            LastResult = "";
            UpdateSprite();
        }

        public void Step(float deltaTime, int direction, bool lightPressed, bool heavyPressed, bool jumpPressed,
            bool crouchHeld, bool guardHeld, float leftEdge, float rightEdge, TrainingTarget target)
        {
            comboDisplayTime = Mathf.Max(0f, comboDisplayTime - deltaTime);
            if (Phase == PunchPhase.Ready && comboDisplayTime == 0f)
                ComboHits = 0;

            if (Phase != PunchPhase.Ready)
            {
                if (CanLink && (NextLink == PunchKind.Hook ? lightPressed : heavyPressed))
                    followUpQueued = true;
                AdvancePunch(deltaTime, target);
            }
            else
            {
                AdvanceStance(deltaTime, direction, leftEdge, rightEdge, lightPressed, heavyPressed,
                    jumpPressed, crouchHeld, guardHeld);
            }

            UpdateSprite();
        }

        private void AdvanceStance(float deltaTime, int direction, float leftEdge, float rightEdge,
            bool lightPressed, bool heavyPressed, bool jumpPressed, bool crouchHeld, bool guardHeld)
        {
            if (CurrentStance == Stance.Jump)
            {
                float airStep = Mathf.Min(deltaTime, jumpSeconds - poseTime);
                MoveHorizontally(direction * moveSpeed * 0.85f * airStep, leftEdge, rightEdge);
                poseTime += airStep;
                float progress = Mathf.Clamp01(poseTime / jumpSeconds);
                Vector3 position = transform.position;
                position.y = spawnPosition.y + 4f * jumpHeight * progress * (1f - progress);
                transform.position = position;
                if (progress >= 1f)
                    SetStance(Stance.Land, deltaTime - airStep);
                return;
            }

            if (CurrentStance == Stance.Land)
            {
                poseTime += deltaTime;
                if (poseTime < landingSeconds)
                    return;
                SetStance(Stance.Idle, 0f);
            }

            if (direction != 0)
                facing = direction;

            if (guardHeld || crouchHeld)
                SetStance(crouchHeld ? Stance.Crouch : Stance.Guard, deltaTime);
            else if (jumpPressed)
                SetStance(Stance.Jump, 0f);
            else if (lightPressed || heavyPressed)
            {
                ComboHits = 0;
                StartPunch(heavyPressed ? PunchKind.Heavy : PunchKind.Light);
            }
            else
            {
                bool moved = MoveHorizontally(direction * moveSpeed * deltaTime, leftEdge, rightEdge);
                SetStance(moved ? Stance.Walk : Stance.Idle, deltaTime);
            }
        }

        private bool MoveHorizontally(float distance, float leftEdge, float rightEdge)
        {
            Vector3 position = transform.position;
            float next = Mathf.Clamp(position.x + distance, leftEdge + bodyHalfWidth, rightEdge - bodyHalfWidth);
            if (Mathf.Approximately(next, position.x))
                return false;

            position.x = next;
            transform.position = position;
            return true;
        }

        private void SetStance(Stance stance, float deltaTime)
        {
            poseTime = CurrentStance == stance ? poseTime + deltaTime : deltaTime;
            CurrentStance = stance;
        }

        private void StartPunch(PunchKind kind)
        {
            CurrentPunch = kind;
            punch = kind switch
            {
                PunchKind.Hook => hook,
                PunchKind.Heavy => heavy,
                _ => new Punch(windupSeconds, activeSeconds, recoverySeconds, damage, lightHitStop, hitOffset, hitSize)
            };
            if (spritePack != null)
                punch.offset.y += spritePack.GetHitHeightOffset(kind);
            SetStance(Stance.Idle, 0f);
            poseTime = punchTime = 0f;
            followUpQueued = landed = false;
            LastResult = "";
            Phase = PunchPhase.Windup;
        }

        private void AdvancePunch(float deltaTime, TrainingTarget target)
        {
            if (HitPaused)
            {
                float frozen = Mathf.Min(deltaTime, hitStopTime);
                hitStopTime -= frozen;
                deltaTime -= frozen;
                if (deltaTime <= 0f)
                    return;
            }

            float activeEnd = punch.windup + punch.active;
            float next = punchTime + deltaTime;
            // Check the interval even when a slow frame skips past the active pose.
            if (!landed && punchTime < activeEnd && next >= punch.windup && Hitbox.Overlaps(target.Hurtbox))
            {
                target.TakeHit(punch.damage, facing, CurrentPunch == PunchKind.Heavy);
                landed = true;
                ComboHits++;
                comboDisplayTime = ComboDisplaySeconds;
                LastResult = "HIT";
                if (punch.hitStop > 0f)
                {
                    punchTime = Mathf.Max(punchTime, punch.windup);
                    Phase = PunchPhase.Active;
                    hitStopTime = punch.hitStop;
                    return;
                }
            }

            punchTime = next;
            if (next < punch.windup)
                Phase = PunchPhase.Windup;
            else if (next < activeEnd)
                Phase = PunchPhase.Active;
            else
            {
                if (landed && followUpQueued && next >= activeEnd + punch.recovery * ComboCancelPoint)
                {
                    StartPunch(NextLink);
                    return;
                }

                LastResult = landed ? "HIT" : "MISS";
                if (!landed)
                    ComboHits = 0;

                Phase = next < activeEnd + punch.recovery ? PunchPhase.Recovery : PunchPhase.Ready;
                if (Phase == PunchPhase.Ready)
                    followUpQueued = false;
            }
        }

        private void UpdateSprite()
        {
            Sprite[] frames;
            float frame;
            if (Phase == PunchPhase.Ready)
            {
                frames = spritePack?.GetStanceFrames(CurrentStance);
                int count = frames?.Length ?? 0;
                frame = CurrentStance switch
                {
                    Stance.Walk => poseTime * walkFramesPerSecond % Mathf.Max(1, count),
                    Stance.Crouch => poseTime / CrouchPoseSeconds,
                    Stance.Jump => poseTime / jumpSeconds * count,
                    Stance.Land => poseTime / landingSeconds * count,
                    _ => poseTime * idleFramesPerSecond % Mathf.Max(1, count)
                };
            }
            else
            {
                frames = spritePack?.GetPunchFrames(CurrentPunch, Phase);
                float progress = Phase switch
                {
                    PunchPhase.Windup => punchTime / punch.windup,
                    PunchPhase.Active => (punchTime - punch.windup) / punch.active,
                    _ => (punchTime - punch.windup - punch.active) / punch.recovery
                };
                frame = progress * (frames?.Length ?? 0);
            }

            Sprite sprite = frames != null && frames.Length > 0
                ? frames[Mathf.Clamp(Mathf.FloorToInt(frame), 0, frames.Length - 1)]
                : null;
            Sprite idlePose = spritePack?.IdlePose;
            artwork.sprite = sprite ? sprite : idlePose ? idlePose : fallbackSprite;
            artwork.flipX = facing < 0;

            if (airShadow != null)
            {
                airShadow.enabled = CurrentStance == Stance.Jump;
                airShadow.transform.localPosition = Vector3.down * (transform.position.y - spawnPosition.y);
                airShadow.flipX = artwork.flipX;
            }
        }
    }
}
