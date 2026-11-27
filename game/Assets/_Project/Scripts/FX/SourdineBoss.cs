using Kenta.Level;
using Kenta.Player;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// La Sourdine, boss du niveau 9 (tools/gemini-niveau9/PROMPT.md, phase 4) : une masse noire collée au bord droit de
    /// l'écran, deux yeux à pupille rouge qui suivent le héros et une bouche en touches de piano. Elle entre et sort
    /// (événement `boss`), tire des lasers sur toute la largeur de l'écran (`laser`) et crache des notes noires (ennemis
    /// en vol de variante « note »). Visuel seulement : la collision du laser est dans PlayerController.CheckLasers.
    /// Chronologie, en temps de jeu (beats) :
    ///   laser à b : annonce de b − 2 à b (yeux grands ouverts, pupilles en tête d'épingle, bouche qui hurle, tirets
    ///   rouges qui clignotent à la hauteur du tir) ; tir de b à b + 0,3 (le faisceau traverse l'écran) ; brûle jusqu'à
    ///   b + 0,3 + durée ; s'éteint en 0,2 temps.
    ///   note frappée à h : crachée à h − 3, elle vole de la bouche à sa place et y arrive à h − 1,2.
    /// Rendu dans le calque monde, recalé chaque image sur la caméra (pixels entiers).
    /// </summary>
    [DefaultExecutionOrder(10)]   // après CameraRig (−50) : la caméra est déjà placée
    public class SourdineBoss : MonoBehaviour
    {
        const int Order = 40;                       // devant le niveau, derrière les acteurs (autre calque)
        const float EnterBeats = 4f;                // glisse depuis la droite
        const float WarnBeats = 2f, FireBeats = LaserFire;
        const float LaserFire = PlayerController.LaserStart;
        const float NoteSpit = 3f, NoteArrive = 1.2f;
        const float LowLaserY = 0.2f, HighLaserY = 1.5f;   // centre du faisceau au-dessus du sol (bas : aux pieds ; haut : à la tête)

        LevelBuilder level;
        SourdineSprites art;
        Transform root, beam, warn;
        SpriteRenderer body, mouth, muzzle, beamSr, warnSr;
        readonly SpriteRenderer[] eyes = new SpriteRenderer[2];
        readonly SpriteRenderer[] pupils = new SpriteRenderer[2];
        Vector2[] noteTargets;

        static readonly Vector2 EyeL = new(-3.55f, 1.9f), EyeR = new(-1.75f, 1.9f), Mouth = new(-2.65f, 0.35f);
        public static SourdineBoss Instance { get; private set; }

        /// <summary>Le boss d'un niveau qui en a un (lasers, notes ou entrée) ; sinon rien n'est créé.</summary>
        public static void Setup(LevelBuilder builder, Transform parent)
        {
            bool needed = builder.BossEvents.Count > 0 || builder.Lasers.Count > 0 || builder.Notes.Count > 0;
            var art = builder.Theme != null ? builder.Theme.sourdine : null;
            if (Instance == null)
            {
                if (!needed || art == null || art.body == null) return;
                var go = new GameObject("Sourdine");
                go.transform.SetParent(parent, false);
                Instance = go.AddComponent<SourdineBoss>();
                Instance.Build(art);
            }
            Instance.level = builder;
            Instance.gameObject.SetActive(needed);
            if (!needed) return;
            var notes = builder.Notes;
            Instance.noteTargets = new Vector2[notes.Count];
            for (int i = 0; i < notes.Count; i++)
                Instance.noteTargets[i] = notes[i].view != null ? (Vector2)notes[i].view.transform.position : notes[i].bounds.center;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        SpriteRenderer Part(string name, Transform parent, Sprite sprite, Vector2 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Order + order;
            return sr;
        }

        void Build(SourdineSprites s)
        {
            art = s;
            root = new GameObject("Body").transform;
            root.SetParent(transform, false);
            body = Part("Mass", root, s.body, new Vector2(-s.body.rect.width / 80f, 0f), 0);   // bord droit sur l'origine
            var e0 = s.eyes != null && s.eyes.Length > 0 ? s.eyes[0] : null;
            var p1 = s.pupils != null && s.pupils.Length > 1 ? s.pupils[1] : null;
            eyes[0] = Part("EyeL", root, e0, EyeL, 1);
            eyes[1] = Part("EyeR", root, e0, EyeR, 1);
            eyes[1].flipX = true;
            pupils[0] = Part("PupilL", eyes[0].transform, p1, Vector2.zero, 2);
            pupils[1] = Part("PupilR", eyes[1].transform, p1, Vector2.zero, 2);
            mouth = Part("Mouth", root, s.mouths != null && s.mouths.Length > 0 ? s.mouths[0] : null, Mouth, 1);

            warn = new GameObject("BeamWarning").transform;
            warn.SetParent(transform, false);
            warnSr = Part("Line", warn, s.beamWarning, Vector2.zero, 3);
            warnSr.drawMode = SpriteDrawMode.Tiled;
            beam = new GameObject("Beam").transform;
            beam.SetParent(transform, false);
            beamSr = Part("Ray", beam, s.beam, Vector2.zero, 4);
            beamSr.drawMode = SpriteDrawMode.Tiled;
            muzzle = Part("Muzzle", beam, s.muzzle, Vector2.zero, 5);
        }

        static float Snap(float v) => Mathf.Round(v * PixelScreen.Ppu) / PixelScreen.Ppu;

        /// <summary>Présence de la Sourdine à ce beat : 0 (hors écran, à droite) … 1 (en place).</summary>
        float Presence(double beat)
        {
            float k = 0f;
            foreach (var e in level.BossEvents)
            {
                if (e.beat > beat) break;
                float t = Mathf.Clamp01((float)(beat - e.beat) / EnterBeats);
                t = 1f - (1f - t) * (1f - t);
                k = e.lane == 1 ? t : 1f - t;
            }
            return k;
        }

        void LateUpdate()
        {
            var rig = CameraRig.Instance;
            var screen = PixelScreen.Instance;
            if (level == null || rig == null || screen == null || Conductor.Instance == null) return;
            double beat = Conductor.Instance.SongBeat;
            Vector2 view = screen.ViewUnits;
            Vector3 cam = rig.transform.position;
            float right = cam.x + view.x * 0.5f, left = cam.x - view.x * 0.5f;

            float presence = Presence(beat);
            root.gameObject.SetActive(presence > 0f);
            float bx = Snap(right + (1f - presence) * 6.5f);
            root.position = new Vector3(bx, Snap(cam.y), 0f);

            // le laser le plus proche : annonce, tir, brûlure
            ChartEvent laser = null;
            float laserT = float.NegativeInfinity;
            foreach (var l in level.Lasers)
            {
                float len = l.@params != null && l.@params.length > 0 ? l.@params.length : PlayerController.LaserLength;
                float t = (float)(beat - l.beat);
                if (t < -WarnBeats) break;
                if (t <= FireBeats + len + 0.2f) { laser = l; laserT = t; break; }
            }
            bool warning = laser != null && laserT < 0f;
            bool firing = laser != null && laserT >= 0f;

            // visage : yeux, pupilles qui suivent le héros, bouche
            var hero = Kenta.Core.RunManager.Instance != null ? Kenta.Core.RunManager.Instance.PlayerTransform : null;
            bool spitting = false;
            if (noteTargets != null)
                for (int i = 0; i < level.Notes.Count; i++)
                {
                    float t = (float)(beat - (level.Notes[i].beat - NoteSpit));
                    if (t >= 0f && t < 0.6f) spitting = true;
                }
            int eye = warning || firing ? 3 : BlinkFrame(beat);
            int pupil = warning || firing ? 2 : 1;
            int mouthFrame = warning || firing ? 2 : spitting ? 1 : 0;
            for (int i = 0; i < 2; i++)
            {
                eyes[i].sprite = Pick(art.eyes, eye);
                pupils[i].sprite = Pick(art.pupils, pupil);
                pupils[i].enabled = eye == 0 || eye == 3;
                Vector2 look = Vector2.zero;
                if (hero != null)
                {
                    Vector2 d = (Vector2)hero.position - (Vector2)eyes[i].transform.position;
                    look = d.normalized * 0.15f;
                }
                pupils[i].transform.localPosition = new Vector3(Snap(i == 1 ? -look.x : look.x), Snap(look.y), 0f);
            }
            mouth.sprite = Pick(art.mouths, mouthFrame);

            // laser : la hauteur du tir est celle du sol sous le héros
            float ground = hero != null ? level.HeightOnPlane(hero.position.x, 0) : cam.y;
            float ly = laser != null ? Snap(ground + (laser.lane == 1 ? HighLaserY : LowLaserY)) : 0f;
            float muzzleX = bx - 4.6f;
            warn.gameObject.SetActive(warning && presence > 0.5f && Mathf.Repeat((float)beat * 4f, 1f) < 0.6f);
            if (warn.gameObject.activeSelf)
            {
                float w = Mathf.Max(0.1f, muzzleX - left);
                warn.position = new Vector3(Snap(left + w * 0.5f), ly, 0f);
                warnSr.size = new Vector2(Snap(w), art.beamWarning.rect.height / PixelScreen.Ppu);
            }
            bool showBeam = firing && presence > 0.5f;
            beam.gameObject.SetActive(showBeam);
            if (showBeam)
            {
                float len = laser.@params != null && laser.@params.length > 0 ? laser.@params.length : PlayerController.LaserLength;
                float reach = Mathf.Clamp01(laserT / FireBeats);                    // le faisceau traverse l'écran
                float fade = laserT > FireBeats + len ? 1f - (laserT - FireBeats - len) / 0.2f : 1f;
                float w = Snap(Mathf.Max(0.1f, (muzzleX - left) * reach));
                beam.position = new Vector3(Snap(muzzleX - w * 0.5f), ly, 0f);
                beamSr.size = new Vector2(w, art.beam.rect.height / PixelScreen.Ppu);
                beamSr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade));
                muzzle.transform.position = new Vector3(Snap(muzzleX), ly, 0f);
                if (laserT < 0.05f) rig.Shake(0.12f, 0.15f);
            }

            // notes noires : crachées par la bouche, elles volent jusqu'à leur place
            if (noteTargets != null)
            {
                Vector2 from = (Vector2)mouth.transform.position;
                for (int i = 0; i < level.Notes.Count; i++)
                {
                    var o = level.Notes[i];
                    if (o.view == null || !o.view.activeInHierarchy || !o.alive) continue;
                    float t = (float)(beat - (o.beat - NoteSpit)) / (NoteSpit - NoteArrive);
                    Vector2 to = noteTargets[i];
                    Vector2 p = t <= 0f || presence <= 0f ? to : Vector2.Lerp(from, to, 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t)));
                    if (t > 0f && t < 1f) p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.8f;   // en cloche
                    o.view.transform.position = new Vector3(Snap(p.x), Snap(p.y), 0f);
                }
            }
        }

        static Sprite Pick(Sprite[] list, int i) => list != null && list.Length > 0 ? list[Mathf.Clamp(i, 0, list.Length - 1)] : null;

        /// <summary>Clignement : toutes les 8 temps, mi-clos, fermé, mi-clos (un quart de temps chacun).</summary>
        static int BlinkFrame(double beat)
        {
            float t = (float)(beat % 8.0);
            return t < 7.25f ? 0 : t < 7.5f ? 1 : t < 7.75f ? 2 : 1;
        }
    }
}
