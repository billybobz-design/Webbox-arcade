using UnityEngine;

namespace Webbox.Training
{
    public sealed class TrainingTarget : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private SpriteRenderer impact;
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(0.01f)] private float flashSeconds = 0.15f;
        [SerializeField] private Vector2 hurtOffset = new Vector2(0f, 1.08f);
        [SerializeField] private Vector2 hurtSize = new Vector2(1.02f, 1.98f);

        private Color baseColor;
        private Quaternion restRotation;
        private Vector3 impactScale;
        private float flashTime;
        private float recoil;

        public int MaxHealth => maxHealth;
        public int Health { get; private set; }
        internal Sprite PreviewSprite => artwork.sprite;
        public Rect Hurtbox => new Rect((Vector2)transform.position + hurtOffset - hurtSize / 2f, hurtSize);

        private void Awake()
        {
            baseColor = artwork.color;
            restRotation = artwork.transform.localRotation;
            impactScale = impact.transform.localScale;
            ResetTarget();
        }

        public void ResetTarget()
        {
            Health = MaxHealth;
            flashTime = 0f;
            recoil = 0f;
            impact.transform.localScale = impactScale;
            UpdateAppearance();
        }

        public void TakeHit(int amount, int attackDirection, bool heavy = false)
        {
            Health = Mathf.Max(0, Health - amount);
            flashTime = flashSeconds;
            recoil = attackDirection * (heavy ? 12f : 5f);
            impact.transform.localScale = impactScale * (heavy ? 1.2f : 1f);
            impact.flipX = attackDirection < 0;
            UpdateAppearance();
        }

        public void Step(float deltaTime)
        {
            if (flashTime <= 0f)
                return;

            flashTime = Mathf.Max(0f, flashTime - deltaTime);
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            float flash = flashTime / flashSeconds;
            artwork.color = Color.Lerp(baseColor, new Color(1f, 0.54f, 0.33f), flash);
            artwork.transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, -recoil * flash);
            impact.enabled = flashTime > 0f;
            impact.color = new Color(1f, 1f, 1f, flash);
        }
    }
}
