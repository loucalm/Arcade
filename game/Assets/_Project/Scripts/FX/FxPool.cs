using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>
    /// Particules légères maison : un pool de SpriteRenderers animés à la main (pas de ParticleSystem,
    /// pas d'allocation en jeu). Budget : capacity particules actives max (GPU intégré de la borne).
    /// </summary>
    public class FxPool : MonoBehaviour
    {
        public static FxPool Instance { get; private set; }

        [SerializeField] Sprite square;
        [SerializeField] Material additive;
        [SerializeField, Min(16)] int capacity = 256;

        struct Particle
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life, maxLife, size, spin, gravity;
            public Color color;
        }

        Particle[] particles;
        int next;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            particles = new Particle[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("P");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = square;
                sr.sortingOrder = 40;
                if (additive != null) sr.sharedMaterial = additive;
                go.SetActive(false);
                particles[i] = new Particle { transform = go.transform, renderer = sr };
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Gerbe de particules. speed en unités/s, life en secondes.</summary>
        public void Burst(Vector2 position, Color color, int count, float speed = 6f, float size = 0.18f,
                          float life = 0.5f, float gravity = 12f, float upBias = 0.3f)
        {
            for (int i = 0; i < count; i++)
            {
                ref var p = ref particles[next];
                next = (next + 1) % particles.Length;
                Vector2 dir = Random.insideUnitCircle.normalized;
                dir.y += upBias;
                p.velocity = dir.normalized * speed * Random.Range(0.4f, 1f);
                p.maxLife = p.life = life * Random.Range(0.6f, 1.1f);
                p.size = size * Random.Range(0.6f, 1.3f);
                p.spin = Random.Range(-720f, 720f);
                p.gravity = gravity;
                p.color = color;
                p.transform.position = new Vector3(position.x, position.y, 0f);
                p.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                p.transform.gameObject.SetActive(true);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (p.life <= 0f) continue;
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.transform.gameObject.SetActive(false);
                    continue;
                }
                p.velocity.y -= p.gravity * dt;
                p.velocity *= 1f - 1.5f * dt;
                p.transform.position += (Vector3)(p.velocity * dt);
                p.transform.Rotate(0f, 0f, p.spin * dt);
                float t = p.life / p.maxLife;
                float s = p.size * t;
                p.transform.localScale = new Vector3(s, s, 1f);
                var c = p.color;
                c.a *= t;
                p.renderer.color = c;
            }
        }
    }
}
