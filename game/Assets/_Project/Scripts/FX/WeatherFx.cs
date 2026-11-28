using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Météo du décor (ADR-047) : pluie, neige et braises derrière le niveau (devant le décor, sous la brume), plus les
    /// morceaux de lave et la fumée d'une éruption (BiomeBackdrop.Erupt). Décor pur, calé sur la musique : les gouttes
    /// tombent en double-croches, une rafale de neige part sur le 1 de chaque mesure, les braises montent sur les temps
    /// 1 et 3. Le mouvement reste continu (pas de crans sur le temps). Pool fixe, aucune allocation en jeu.
    /// Les particules vivent dans le repère de la vue et glissent avec la parallaxe de leur calque.
    /// </summary>
    public class WeatherFx : MonoBehaviour
    {
        [SerializeField] Sprite rain, snowSmall, snowBig, ember;
        [SerializeField] Sprite lavaBlob, smoke;
        [SerializeField, Min(16)] int capacity = 120;
        [SerializeField] Camera cam;
        [Tooltip("Parallaxe de la météo (comme le calque proche du décor).")]
        [SerializeField] float parallax = 0.7f;
        [SerializeField] Color rainColor = new(0.78f, 0.86f, 1f, 0.55f);
        [SerializeField] Color snowColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField] Color emberColor = new(1f, 0.75f, 0.45f, 1f);
        [Tooltip("Teinte du ciel sous une pluie pleine (multipliée à l'ambiance de la section).")]
        [SerializeField] Color rainSky = new(0.68f, 0.7f, 0.8f, 1f);
        [SerializeField] Color snowSky = new(0.9f, 0.95f, 1f, 1f);

        struct Particle
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 local, velocity;      // position par rapport au centre de la vue (u), vitesse (u/s)
            public float life, maxLife, gravity, sway, phase, parallax;
            public Color color;
        }

        Particle[] particles;
        int next;
        WeatherTracks tracks;
        int beatsPerBar = 4;
        double prevBeat;
        bool started;
        float lastCamX;

        /// <summary>Multiplicateur de la teinte du ciel et du décor (pluie : plus sombre ; neige : plus froid).</summary>
        public Color SkyTint { get; private set; } = Color.white;

        /// <summary>Intensité (0..1) d'un effet au temps `beat` ; 0 sans météo.</summary>
        public static float IntensityOf(WeatherTracks tracks, string kind, double beat)
        {
            var c = tracks?.For(kind);
            return c != null ? c.Evaluate(beat) : 0f;
        }

        /// <summary>Crée le pool (appelé par BiomeBackdrop) : rangé juste devant le décor, sous la brume.</summary>
        public void Init(Camera camera, int sortingOrder)
        {
            if (particles != null) return;
            if (cam == null) cam = camera;
            particles = new Particle[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("W");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = sortingOrder;
                go.SetActive(false);
                particles[i] = new Particle { transform = go.transform, renderer = sr };
            }
        }

        /// <summary>Météo du niveau (LevelBuilder.Weather) ; null = plus de météo, tout s'éteint (fin de partie, menus).</summary>
        public void SetTracks(WeatherTracks weather, int barBeats = 4)
        {
            tracks = weather;
            beatsPerBar = Mathf.Max(1, barBeats);
            started = false;
            if (weather == null) HideAll();
        }

        public void HideAll()
        {
            if (particles == null) return;
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].life = 0f;
                particles[i].transform.gameObject.SetActive(false);
            }
            SkyTint = Color.white;
        }

        /// <summary>
        /// Une particule à `world` (u), vitesse `velocity` (u/s), qui vit `life` s. `layerParallax` : parallaxe du calque
        /// qui la porte (0,12 pour la lave du volcan lointain). Reprend la plus ancienne si le pool est plein.
        /// </summary>
        public void Spawn(Sprite sprite, Vector2 world, Vector2 velocity, float life, Color color, float gravity = 0f,
                          float layerParallax = -1f, float sway = 0f)
        {
            if (particles == null || cam == null || sprite == null) return;
            ref var p = ref particles[next];
            next = WeatherMath.NextSlot(next, particles.Length);
            Vector2 c = cam.transform.position;
            p.local = world - c;
            p.velocity = velocity;
            p.maxLife = p.life = life;
            p.gravity = gravity;
            p.sway = sway;
            p.phase = Random.value * 6.28f;
            p.parallax = layerParallax >= 0f ? layerParallax : parallax;
            p.color = color;
            p.renderer.sprite = sprite;
            p.renderer.color = color;
            p.transform.position = new Vector3(PixelScreen.Snap(world.x), PixelScreen.Snap(world.y), 0f);
            p.transform.gameObject.SetActive(true);
        }

        public Sprite LavaBlob => lavaBlob;
        public Sprite Smoke => smoke;

        void Update()
        {
            if (particles == null || cam == null) return;   // scripts rechargés en pleine partie (Play mode)
            var conductor = Conductor.Instance;
            double beat = conductor != null ? conductor.SongBeat : 0;
            if (!started) { prevBeat = beat; lastCamX = cam.transform.position.x; started = true; }

            float r = IntensityOf(tracks, "rain", beat);
            float s = IntensityOf(tracks, "snow", beat);
            float e = IntensityOf(tracks, "embers", beat);
            SkyTint = Color.Lerp(Color.white, rainSky, r) * Color.Lerp(Color.white, snowSky, s);

            float halfH = cam.orthographicSize, half = halfH * cam.aspect;
            if (r > 0f)
                for (int t = WeatherMath.Ticks(prevBeat, beat, 0.25, 2); t > 0; t--)          // double-croches
                    for (int n = WeatherMath.SpawnCount(r, 6); n > 0; n--)
                        SpawnTop(rain, half, halfH, new Vector2(-1.6f, Random.Range(-15f, -12f)), 0.9f, rainColor, 0f);
            if (s > 0f)
            {
                for (int t = WeatherMath.Ticks(prevBeat, beat, 0.5, 2); t > 0; t--)           // croches
                    for (int n = WeatherMath.SpawnCount(s, 2); n > 0; n--) SpawnFlake(half, halfH, 0f);
                for (int t = WeatherMath.Ticks(prevBeat, beat, beatsPerBar, 1); t > 0; t--)   // rafale sur le 1
                    for (int n = WeatherMath.SpawnCount(s, 12); n > 0; n--) SpawnFlake(half, halfH, -2.6f);
            }
            if (e > 0f)
                for (int t = WeatherMath.Ticks(prevBeat, beat, 2, 1); t > 0; t--)             // temps 1 et 3
                    for (int n = WeatherMath.SpawnCount(e, 5); n > 0; n--)
                    {
                        var c = (Vector2)cam.transform.position;
                        Spawn(ember, c + new Vector2(Random.Range(-half, half), -halfH + Random.Range(0f, 0.6f)),
                              new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1.2f, 2.2f)), Random.Range(1.6f, 2.4f),
                              emberColor, 0f, -1f, 0.25f);
                    }
            prevBeat = beat;

            float camX = cam.transform.position.x;
            float dx = camX - lastCamX;
            lastCamX = camX;
            Move(dx, Time.deltaTime);
        }

        void SpawnTop(Sprite sprite, float half, float halfH, Vector2 velocity, float life, Color color, float sway)
        {
            var c = (Vector2)cam.transform.position;
            Spawn(sprite, c + new Vector2(Random.Range(-half, half + 2f), halfH + Random.Range(0f, 0.5f)), velocity,
                  life + 2f * halfH / Mathf.Max(0.5f, -velocity.y), color, 0f, -1f, sway);
        }

        void SpawnFlake(float half, float halfH, float wind)
        {
            SpawnTop(Random.value < 0.5f ? snowSmall : snowBig, half, halfH,
                     new Vector2(wind + Random.Range(-0.4f, 0.2f), Random.Range(-1.3f, -0.8f)), 0.4f, snowColor, 0.35f);
        }

        void Move(float camDx, float dt)
        {
            Vector2 c = cam.transform.position;
            float time = Time.time;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (p.life <= 0f) continue;
                p.life -= dt;
                if (p.life <= 0f) { p.transform.gameObject.SetActive(false); continue; }
                p.velocity.y -= p.gravity * dt;
                // monde fixe : il glisse de -camDx à l'écran ; un calque en parallaxe p glisse de -camDx × p
                p.local.x += p.velocity.x * dt - camDx * p.parallax;
                p.local.y += p.velocity.y * dt;
                float x = c.x + p.local.x + (p.sway > 0f ? Mathf.Sin(time * 2.2f + p.phase) * p.sway : 0f);
                p.transform.position = new Vector3(PixelScreen.Snap(x), PixelScreen.Snap(c.y + p.local.y), 0f);
                float t = p.life / p.maxLife;
                var col = p.color;
                col.a *= Mathf.Round(Mathf.Clamp01(t / 0.3f) * 4f) / 4f;     // s'efface par paliers sur la fin
                p.renderer.color = col;
            }
        }
    }
}
