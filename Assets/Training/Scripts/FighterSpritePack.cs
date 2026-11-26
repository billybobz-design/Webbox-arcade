using UnityEngine;

namespace Webbox.Training
{
    [CreateAssetMenu(menuName = "Webbox/Fighter Sprite Pack", fileName = "FighterSpritePack")]
    public sealed class FighterSpritePack : ScriptableObject
    {
        [Header("Stances · face right, pivot at feet")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] guardFrames;
        [SerializeField] private Sprite[] crouchFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] landFrames;
        [SerializeField] private Sprite airShadow;

        [Header("Light punch")]
        [SerializeField] private Sprite[] windupFrames;
        [SerializeField] private Sprite[] activeFrames;
        [SerializeField] private Sprite[] recoveryFrames;

        [Header("Hook")]
        [SerializeField] private Sprite[] hookWindupFrames;
        [SerializeField] private Sprite[] hookActiveFrames;
        [SerializeField] private Sprite[] hookRecoveryFrames;

        [Header("Heavy punch")]
        [SerializeField] private Sprite[] heavyWindupFrames;
        [SerializeField] private Sprite[] heavyActiveFrames;
        [SerializeField] private Sprite[] heavyRecoveryFrames;

        internal Sprite IdlePose => idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null;
        internal Sprite AirShadow => airShadow;

        internal Sprite[] GetStanceFrames(TrainingFighter.Stance stance) => stance switch
        {
            TrainingFighter.Stance.Walk => walkFrames,
            TrainingFighter.Stance.Guard => guardFrames,
            TrainingFighter.Stance.Crouch => crouchFrames,
            TrainingFighter.Stance.Jump => jumpFrames,
            TrainingFighter.Stance.Land => landFrames,
            _ => idleFrames
        };

        internal Sprite[] GetPunchFrames(TrainingFighter.PunchKind kind, TrainingFighter.PunchPhase phase)
        {
            var frames = (kind, phase) switch
            {
                (TrainingFighter.PunchKind.Hook, TrainingFighter.PunchPhase.Windup) => hookWindupFrames,
                (TrainingFighter.PunchKind.Hook, TrainingFighter.PunchPhase.Active) => hookActiveFrames,
                (TrainingFighter.PunchKind.Hook, TrainingFighter.PunchPhase.Recovery) => hookRecoveryFrames,
                (TrainingFighter.PunchKind.Heavy, TrainingFighter.PunchPhase.Windup) => heavyWindupFrames,
                (TrainingFighter.PunchKind.Heavy, TrainingFighter.PunchPhase.Active) => heavyActiveFrames,
                (TrainingFighter.PunchKind.Heavy, TrainingFighter.PunchPhase.Recovery) => heavyRecoveryFrames,
                _ => null
            };
            if (frames != null && frames.Length > 0)
                return frames;

            return phase switch
            {
                TrainingFighter.PunchPhase.Windup => windupFrames,
                TrainingFighter.PunchPhase.Active => activeFrames,
                _ => recoveryFrames
            };
        }
    }
}
