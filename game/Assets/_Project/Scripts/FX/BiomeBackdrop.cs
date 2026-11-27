using System.Collections.Generic;
using Kenta.Core;
using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Décor du biome courant : un ciel fixe (640×360), le gardien (sprite à part, endormi ou éveillé) et trois calques
    /// de 1280×360 en parallaxe qui bouclent sans couture. Dessinés à la définition de la vue large : leurs pixels ont la
    /// même taille que ceux du niveau. Tout est placé sur la grille de pixels. SetBiome change de monde.
    /// Ambiance par section (SetSection) : teinte et luminosité qui glissent, pour que le décor change au fil du morceau.
    /// En partie, les calques suivent aussi le sol du niveau à la verticale (parallaxe verticale) : le pied des arbres
    /// reste au niveau du sol le plus bas de la vue au lieu de rester collé à l'écran pendant que le niveau monte et
    /// descend (audit du 2026-11-25). Sous chaque calque, sa dernière rangée (le pied, tools/kenta_art.py) est étirée
    /// jusqu'au bas de la vue. Le sol est lu sur CameraRig.HeightProbe, sans rien changer à la caméra.
    /// Le ciel vit au fil du morceau (ADR-041) : le soleil (sprite à part) descend, rougit et passe derrière les
    /// montagnes au début de la section « fin » ; la lune se lève alors et les étoiles s'allument. Dans l'Orage,
    /// l'événement `lightning` de la chart zèbre le ciel d'un éclair (Strike).
    /// </summary>
    public class BiomeBackdrop : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [Tooltip("Vitesse relative des calques (0 = fixe à l'écran, 1 = défile comme le niveau) : loin, milieu, proche.")]
        [SerializeField] float[] parallax = { 0.12f, 0.35f, 0.7f };
        [SerializeField] int firstSortingOrder = -100;
        [Tooltip("Couleur de la brume entre le décor et le niveau (l'opacité vient de fogTop / fogBottom).")]
        [SerializeField] Color fog = new(0.93f, 0.96f, 1f, 1f);
        [Tooltip("Opacité de la brume en haut et en bas de la vue : plus dense vers l'horizon, le ciel reste net.")]
        [SerializeField, Range(0f, 1f)] float fogTop = 0.04f;
        [SerializeField, Range(0f, 1f)] float fogBottom = 0.2f;
        [Tooltip("Part du suivi vertical du sol par calque (0 = collé à l'écran, 1 = posé sur le sol) : loin, milieu, proche.")]
        [SerializeField] float[] groundFollow = { 0.35f, 0.7f, 1f };
        [Tooltip("Hauteur (px, image du calque) de sa ligne de sol au-dessus du bas de l'image : 360 − 2 × GROUND_Y de kenta_art.py.")]
        [SerializeField] float layerGroundPx = 96f;
        [Tooltip("Lissage (s) de la hauteur du sol suivie par le décor : pas de saut à chaque marche.")]
        [SerializeField] float groundSmoothTime = 0.6f;
        [Tooltip("Enfoncement du gardien (px) sous sa place d'origine. 0 : il est en entier ; c'est l'élément principal du décor.")]
        [SerializeField] float guardianSink = 0f;

        [Header("Altitude (on voit qu'on est haut)")]
        [Tooltip("Part de l'altitude dont chaque calque descend sous le sol (loin, milieu, proche) : le paysage passe sous le héros.")]
        [SerializeField] float[] altitudeSink = { 0.08f, 0.22f, 0.45f };
        [Tooltip("Descente maximale (u) de chaque calque : loin, milieu, proche.")]
        [SerializeField] float[] altitudeSinkMax = { 1.5f, 3.5f, 6f };
        [Tooltip("Altitude (u) à partir de laquelle la mer de nuages apparaît, et celle où elle est pleine.")]
        [SerializeField] Vector2 cloudSeaRange = new(6f, 26f);
        [SerializeField] Color cloudSea = new(0.96f, 0.97f, 1f, 1f);
        [Tooltip("Teinte du ciel tout en haut (plus froid, plus sombre) ; altitude pleine à 45 u.")]
        [SerializeField] Color highSky = new(0.72f, 0.78f, 0.98f, 1f);

        [Header("Course du soleil, nuit, éclairs (ADR-041)")]
        [Tooltip("Dérive horizontale (px du ciel 640×360) du soleil entre le départ et son coucher, vers le bord le plus proche.")]
        [SerializeField] float sunDrift = 36f;
        [Tooltip("Teinte du soleil couchant.")]
        [SerializeField] Color sunsetColor = new(1f, 0.6f, 0.38f, 1f);
        [Tooltip("Place de la lune levée (px du ciel 640×360, depuis le centre ; x du côté opposé au soleil).")]
        [SerializeField] Vector2 moonHigh = new(150f, 96f);
        [Tooltip("Durée (s) du lever de la lune et de l'allumage des étoiles.")]
        [SerializeField] float nightFade = 2.5f;
        [Tooltip("Durée (s) d'un éclair à l'écran (il s'éteint par paliers).")]
        [SerializeField] float boltTime = 0.22f;
        [SerializeField] Color boltSky = new(0.86f, 0.9f, 1f, 1f);

        SpriteRenderer sky;
        SpriteRenderer guardian;
        SpriteRenderer ground;
        SpriteRenderer groundFill;
        SpriteRenderer fogLayer;
        SpriteRenderer cloudLayer;
        SpriteRenderer sun, moon, stars, bolt;
        float dayProgress, nightAt = 1f, night, sunY, sunYVel;
        bool sunPlaced;
        float strike, strikePower;
        Vector2 boltPlace;     // x : part de la demi-largeur de la vue (−1..1) ; y : inutilisé
        float altitude, altitudeVel;
        static readonly Color GroundFillTint = new(0.58f, 0.54f, 0.7f);    // comme le flanc des piliers du niveau
        const float GroundFillDepth = 8f;
        readonly SpriteRenderer[][] layers = new SpriteRenderer[3][];
        readonly SpriteRenderer[] feet = new SpriteRenderer[3];
        readonly Dictionary<Sprite, Sprite> footSprites = new();
        float groundSmooth, groundVel;
        bool groundValid;
        BiomeTheme current;
        Color mood = Color.white, moodFrom = Color.white, moodTo = Color.white;
        float moodT = 1f, moodBlend = 1f;

        public BiomeTheme Current => current;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            sky = Make("Sky", firstSortingOrder);
            stars = Make("NightStars", firstSortingOrder + 1);     // devant le ciel, derrière tout le reste
            moon = Make("Moon", firstSortingOrder + 2);
            sun = Make("Sun", firstSortingOrder + 3);               // passe derrière les montagnes en se couchant
            guardian = Make("Guardian", firstSortingOrder + 5);    // derrière les montagnes lointaines (ADR-028)
            bolt = Make("Bolt", firstSortingOrder + 25);           // devant les calques lointain et du milieu, derrière le proche
            bolt.enabled = false;
            ground = Make("Ground", -5);
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.tileMode = SpriteTileMode.Continuous;
            ground.size = new Vector2(8f, 0.5f);
            ground.enabled = false;
            groundFill = Make("GroundFill", -6);
            groundFill.drawMode = SpriteDrawMode.Tiled;
            groundFill.tileMode = SpriteTileMode.Continuous;
            groundFill.color = GroundFillTint;
            groundFill.enabled = false;
            for (int i = 0; i < 3; i++)
            {
                layers[i] = new[] { Make($"Layer{i}A", firstSortingOrder + 10 * (i + 1)), Make($"Layer{i}B", firstSortingOrder + 10 * (i + 1)) };
                feet[i] = Make($"Layer{i}Foot", firstSortingOrder + 10 * (i + 1));
            }
            fogLayer = Make("Fog", firstSortingOrder + 40);       // au-dessus des 3 calques, sous le niveau
            fogLayer.sprite = FogSprite(fogTop, fogBottom);
            fogLayer.color = new Color(fog.r, fog.g, fog.b, 1f);
            cloudLayer = Make("CloudSea", firstSortingOrder + 35);   // devant les calques, sous la brume et le niveau
            cloudLayer.sprite = FogSprite(0f, 0.5f, 0.35f);      // 50 % au bas de l'image, rien au-dessus du tiers
        }

        /// <summary>Brume en dégradé vertical (8 bandes, pixel art) : sprite de 1 × 1 unité.</summary>
        static Sprite FogSprite(float top, float bottom, float clearAbove = 1f)
        {
            const int bands = 8;
            var tex = new Texture2D(1, bands, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < bands; y++)                   // y = 0 en bas ; au-dessus de clearAbove (fraction) : transparent
            {
                float f = y / (bands - 1f);
                float a = f > clearAbove ? 0f : Mathf.Lerp(bottom, top, Mathf.Clamp01(f / Mathf.Max(0.01f, clearAbove)));
                tex.SetPixel(0, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, bands), new Vector2(0.5f, 0.5f), bands);
        }

        /// <summary>Dernière rangée d'un calque (son pied, couleur unie), à étirer sous lui jusqu'au bas de la vue.</summary>
        Sprite FootOf(Sprite strip)
        {
            if (strip == null) return null;
            if (footSprites.TryGetValue(strip, out var foot)) return foot;
            var r = strip.textureRect;
            foot = Sprite.Create(strip.texture, new Rect(r.x, r.y, r.width, 1f), new Vector2(0.5f, 1f), strip.pixelsPerUnit);
            footSprites[strip] = foot;
            return foot;
        }

        SpriteRenderer Make(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        public void SetBiome(BiomeTheme biome)
        {
            if (biome == null) return;
            current = biome;
            sky.sprite = biome.sky;
            guardian.sprite = biome.guardian;
            if (sun != null)      // null : scripts rechargés en pleine partie (Play mode), astres perdus
            {
                sun.sprite = biome.sun;
                moon.sprite = biome.sun != null ? biome.moon : null;
                stars.sprite = biome.sun != null ? biome.nightStars : null;
            }
            sunPlaced = false;                                      // nouveau monde : le soleil se pose sans glisser
            ground.sprite = biome.ground;
            ground.size = new Vector2(8f, 0.5f);          // le sprite remet la taille à celle d'une tuile : on la rétablit
            groundFill.sprite = biome.fill;
            groundFill.size = new Vector2(8f, GroundFillDepth);
            var strips = new[] { biome.far, biome.mid, biome.near };
            for (int i = 0; i < 3; i++)
            {
                foreach (var sr in layers[i]) sr.sprite = strips[i];
                feet[i].sprite = FootOf(strips[i]);
            }
        }

        /// <summary>Retour à l'ambiance neutre (début de partie, accueil).</summary>
        public void ResetMood()
        {
            groundValid = false;     // nouvelle partie (même enchaînée sur la démo) : le décor se pose sans glisser
            mood = moodFrom = moodTo = Color.white;
            moodT = moodBlend = 1f;
            dayProgress = 0f;                                       // le jour se lève : soleil à sa place, pas de lune
            night = 0f;
            sunPlaced = false;
            strike = 0f;
        }

        /// <summary>Part du morceau (0..1) où le soleil est couché et où la nuit tombe (début de la section « fin »).</summary>
        public void SetNightAt(float progress) => nightAt = Mathf.Clamp(progress, 0.05f, 1f);

        /// <summary>
        /// Éclair (événement `lightning`, sur le temps) : un des éclairs du biome zèbre le ciel à une place tirée du
        /// beat (même place à chaque partie), le ciel et les nuages s'illuminent puis s'éteignent par paliers.
        /// </summary>
        public void Strike(float intensity, float beat)
        {
            if (bolt == null) return;     // scripts rechargés en pleine partie (Play mode)
            strike = 1f;
            strikePower = Mathf.Clamp01(intensity);
            var bolts = current != null ? current.bolts : null;
            int seed = Mathf.RoundToInt(beat * 2f) * 7919;
            bolt.sprite = bolts != null && bolts.Length > 0 ? bolts[(seed >> 3) % bolts.Length] : null;
            bolt.flipX = (seed >> 5) % 2 == 1;
            boltPlace = new Vector2(((seed % 1000) / 1000f) * 1.6f - 0.8f, 0f);
        }

        /// <summary>
        /// Hauteur 0..1 du chemin du soleil couchant au temps t (0 = départ, 1 = couché) : lent au début, il plonge vers
        /// la fin, comme un vrai coucher vu de près de l'horizon.
        /// </summary>
        public static float SunsetCurve(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (1.6f - 0.6f * t);
        }

        /// <summary>
        /// Ambiance d'une section : une teinte par type de section (couplet chaud, pont frais, refrain doré, final au
        /// couchant…) et une lumière qui baisse doucement au fil du morceau (progress 0 → 1), en `blendSeconds`.
        /// </summary>
        public void SetSection(string sectionName, float progress, float blendSeconds = 1.2f)
        {
            moodFrom = mood;
            // le monde sourd reste en noir et blanc pur : pas de refrain doré ni de couchant (ADR-040)
            moodTo = current != null && current.monochrome ? Color.white : MoodFor(sectionName, progress);
            moodT = 0f;
            moodBlend = Mathf.Max(0.01f, blendSeconds);
        }

        public static Color MoodFor(string sectionName, float progress)
        {
            string n = (sectionName ?? "").ToLowerInvariant();
            Color tint = n.StartsWith("couplet") ? new Color(1f, 0.96f, 0.88f)
                       : n.StartsWith("pont") ? new Color(0.9f, 0.95f, 1f)
                       : n.StartsWith("roulement") ? new Color(0.96f, 0.86f, 0.82f)
                       : n.StartsWith("refrain") ? new Color(1f, 0.9f, 0.72f)
                       : n.StartsWith("final") ? new Color(1f, 0.8f, 0.7f)
                       : n.StartsWith("fin") ? new Color(0.62f, 0.64f, 0.92f)       // la nuit (ADR-041)
                       : Color.white;
            float light = Mathf.Lerp(1f, 0.86f, Mathf.Clamp01(progress));      // le jour baisse au fil du morceau
            return new Color(tint.r * light, tint.g * light, tint.b * light, 1f);
        }

        /// <summary>Sol décoratif sous le ciel (accueil, menus) : en partie, le niveau pose son propre sol.</summary>
        public void SetGroundVisible(bool visible)
        {
            ground.enabled = visible;
            groundFill.enabled = visible && groundFill.sprite != null;
        }

        void LateUpdate()
        {
            if (cam == null || current == null) return;
            if (layers[0] == null) return;   // scripts rechargés en pleine partie (Play mode) : calques perdus, pas d'erreur à chaque image
            var c = cam.transform.position;
            // décor dessiné pour la vue large (640×360) : agrandi seulement si un jour la vue dépasse sa hauteur
            float skyPx = current.sky != null ? current.sky.rect.height : 360f;
            int k = PixelScreen.Instance != null ? Mathf.Max(1, Mathf.CeilToInt(PixelScreen.Instance.ViewHeight / skyPx)) : 1;
            var scale = new Vector3(k, k, 1f);
            sky.transform.localScale = scale;
            sky.transform.position = new Vector3(PixelScreen.Snap(c.x), PixelScreen.Snap(c.y), 0f);

            if (moodT < 1f)
            {
                moodT = Mathf.Min(1f, moodT + Time.deltaTime / moodBlend);
                mood = Color.Lerp(moodFrom, moodTo, moodT * moodT * (3f - 2f * moodT));
            }
            float half = cam.orthographicSize * cam.aspect;
            float halfH = cam.orthographicSize;
            // sol suivi par le décor : le plus bas de la vue, lissé (en partie seulement ; menus : décor collé à l'écran)
            float lift = GroundLift(c, half, halfH, k);
            // altitude : hauteur du sol suivi au-dessus du départ du niveau (0 hors partie)
            altitude = Mathf.SmoothDamp(altitude, groundValid ? Mathf.Max(0f, groundSmooth) : 0f, ref altitudeVel, 0.8f, Mathf.Infinity, Time.deltaTime);
            float high = Mathf.Clamp01(altitude / 45f);
            if (strike > 0f) strike = Mathf.Max(0f, strike - Time.deltaTime / Mathf.Max(0.01f, boltTime));
            float lit = Mathf.Ceil(strike * 3f) / 3f * strikePower;                // paliers : pixel art, pas de fondu
            sky.color = Color.Lerp(mood * Color.Lerp(Color.white, highSky, high), boltSky, lit * 0.55f);
            guardian.sprite = current.guardian;
            guardian.color = mood;
            guardian.transform.localScale = scale;
            Vector2 go = current.guardianOffset * (k / PixelScreen.Ppu);
            float sink = guardianSink * k / PixelScreen.Ppu;
            guardian.transform.position = new Vector3(PixelScreen.Snap(c.x + go.x), PixelScreen.Snap(c.y + go.y - sink + lift * Follow(0) - Sink(0)), 0f);
            UpdateSkyBodies(c, half, halfH, k, lift);
            float fogH = halfH * 2f + 1f;
            // le sprite fait 1 × 8 px à 8 px/u (0,125 × 1 u) : la largeur se divise par celle du sprite, sinon la brume
            // ne couvre qu'une colonne au centre de l'écran
            Vector2 fogSize = fogLayer.sprite != null ? (Vector2)fogLayer.sprite.bounds.size : Vector2.one;
            fogLayer.transform.localScale = new Vector3((fogH * cam.aspect + 1f) / fogSize.x, fogH / fogSize.y, 1f);
            fogLayer.transform.position = new Vector3(c.x, c.y, 0f);
            // mer de nuages : en hauteur, le bas de la vue se remplit de nuages (le monde est loin dessous)
            float clouds = Mathf.Clamp01((altitude - cloudSeaRange.x) / Mathf.Max(0.01f, cloudSeaRange.y - cloudSeaRange.x));
            cloudLayer.enabled = clouds > 0.01f;
            if (cloudLayer.enabled)
            {
                Vector2 cs = cloudLayer.sprite.bounds.size;
                cloudLayer.transform.localScale = new Vector3((fogH * cam.aspect + 1f) / cs.x, fogH / cs.y, 1f);
                cloudLayer.transform.position = new Vector3(c.x, c.y, 0f);
                cloudLayer.color = new Color(cloudSea.r * mood.r, cloudSea.g * mood.g, cloudSea.b * mood.b, Mathf.Round(clouds * 6f) / 6f);
            }
            if (ground.enabled && ground.sprite != null)
            {
                // sol des menus : toute la largeur de la vue, motif accroché au monde (il défile avec le décor), terre dessous
                float tile = ground.sprite.bounds.size.x;
                float w = Mathf.Ceil((half * 2f + 2f) / tile) * tile;
                float gx = Mathf.Floor(c.x / tile) * tile;
                ground.size = new Vector2(w, 0.5f);
                ground.transform.position = new Vector3(gx, -0.25f, 0f);
                if (groundFill.enabled)
                {
                    groundFill.size = new Vector2(w, GroundFillDepth);
                    groundFill.transform.position = new Vector3(gx, -0.5f - GroundFillDepth * 0.5f, 0f);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                var strip = layers[i][0].sprite;
                if (strip == null) continue;
                float width = strip.bounds.size.x * k;
                float height = strip.bounds.size.y * k;
                float offset = Mathf.Repeat(c.x * parallax[i], width);
                float left = c.x - half - offset;
                float y = PixelScreen.Snap(c.y + lift * Follow(i) - Sink(i));
                for (int j = 0; j < 2; j++)
                {
                    layers[i][j].transform.localScale = scale;
                    layers[i][j].color = i == 0 ? Color.Lerp(mood, boltSky, lit * 0.35f) : mood;
                    layers[i][j].transform.position = new Vector3(PixelScreen.Snap(left + width * (j + 0.5f)), y, 0f);
                }
                // pied : dernière rangée du calque, étirée de son bas jusque sous la vue
                var foot = feet[i];
                float bottom = y - height * 0.5f;
                float rows = Mathf.Max(0f, Mathf.Ceil((bottom - (c.y - halfH) + 1f) * PixelScreen.Ppu / k));
                foot.enabled = foot.sprite != null && rows > 0f;
                foot.color = mood;
                foot.transform.localScale = new Vector3(k * Mathf.Ceil((half * 2f + 2f) / (foot.sprite != null ? foot.sprite.bounds.size.x : 1f)), k * rows, 1f);
                foot.transform.position = new Vector3(PixelScreen.Snap(c.x), bottom, 0f);
            }
        }

        const float LayerHalfPx = 180f;                     // calques de 1280 × 360 px

        /// <summary>
        /// Soleil, lune, étoiles et éclair. Le soleil suit l'avancement du morceau jusqu'à `nightAt` : il descend
        /// (SunsetCurve), dérive vers le bord le plus proche, rougit, et se colle peu à peu au calque lointain (qui suit
        /// le sol) pour passer derrière ses montagnes. Passé `nightAt`, la lune se lève du côté opposé et les étoiles
        /// s'allument, en paliers. Tout est posé sur la grille de pixels.
        /// </summary>
        void UpdateSkyBodies(Vector3 c, float half, float halfH, int k, float lift)
        {
            if (sun == null || moon == null || stars == null || bolt == null) return;   // rechargés en Play mode
            var run = RunManager.Instance;
            if (run != null && run.IsRunning) dayProgress = run.Progress;
            float t = Mathf.Clamp01(dayProgress / nightAt);
            float px = k / (float)PixelScreen.Ppu;                       // un px du ciel 640×360, en unités (pas de division entière)
            float farY = lift * Follow(0) - Sink(0);                      // décalage du calque lointain (u)

            if (sun.sprite != null)
            {
                Vector2 o = current.sunOffset;
                float set = SunsetCurve(t);
                float targetY = Mathf.Lerp(o.y, current.sunSetY, set);
                // reprise à un checkpoint (le morceau recule) ou nouveau monde : il se pose sans glisser
                if (!sunPlaced || Mathf.Abs(targetY - sunY) > 40f) { sunY = targetY; sunYVel = 0f; sunPlaced = true; }
                else sunY = Mathf.SmoothDamp(sunY, targetY, ref sunYVel, 0.4f, Mathf.Infinity, Time.deltaTime);
                float x = o.x + Mathf.Sign(o.x == 0f ? 1f : o.x) * sunDrift * t;
                sun.enabled = set < 0.999f;
                sun.transform.localScale = new Vector3(k, k, 1f);
                sun.transform.position = new Vector3(PixelScreen.Snap(c.x + x * px), PixelScreen.Snap(c.y + sunY * px + farY * set), 0f);
                float warm = Mathf.Clamp01((t - 0.45f) / 0.55f);
                sun.color = Color.Lerp(Color.white, sunsetColor, Mathf.Round(warm * 4f) / 4f);
            }
            else sun.enabled = false;

            float nightTarget = sun.sprite != null && dayProgress >= nightAt - 0.001f ? 1f : 0f;
            night = Mathf.MoveTowards(night, nightTarget, Time.deltaTime / Mathf.Max(0.1f, nightFade));
            float shown = Mathf.Round(night * 4f) / 4f;                    // paliers d'un quart
            if (moon.sprite != null && shown > 0f)
            {
                float side = current.sunOffset.x >= 0f ? -1f : 1f;           // côté opposé au soleil
                float rise = night * night * (3f - 2f * night);
                float y = Mathf.Lerp(current.sunSetY, moonHigh.y, rise);
                moon.enabled = true;
                moon.transform.localScale = new Vector3(k, k, 1f);
                moon.transform.position = new Vector3(PixelScreen.Snap(c.x + side * moonHigh.x * px),
                                                      PixelScreen.Snap(c.y + y * px + farY * (1f - rise)), 0f);
            }
            else moon.enabled = false;
            stars.enabled = stars.sprite != null && shown > 0f;
            if (stars.enabled)
            {
                stars.transform.localScale = new Vector3(k, k, 1f);
                stars.transform.position = sky.transform.position;
                stars.color = new Color(1f, 1f, 1f, shown);
            }

            bolt.enabled = bolt.sprite != null && strike > 0f;
            if (bolt.enabled)
            {
                float h = bolt.sprite.bounds.size.y * k;
                bolt.transform.localScale = new Vector3(k, k, 1f);
                // le haut de l'éclair dépasse un peu du haut de la vue : il tombe des nuages
                bolt.transform.position = new Vector3(PixelScreen.Snap(c.x + boltPlace.x * half),
                                                      PixelScreen.Snap(c.y + halfH + 0.25f - h * 0.5f), 0f);
                bolt.color = new Color(1f, 1f, 1f, Mathf.Ceil(strike * 3f) / 3f);
            }
        }

        float Follow(int layer) => groundFollow != null && layer < groundFollow.Length ? groundFollow[layer] : 0f;

        /// <summary>Descente (u) d'un calque sous sa place en altitude : le proche passe sous le bord du palier.</summary>
        float Sink(int layer)
        {
            if (altitudeSink == null || layer >= altitudeSink.Length) return 0f;
            float max = altitudeSinkMax != null && layer < altitudeSinkMax.Length ? altitudeSinkMax[layer] : 0f;
            return Mathf.Min(altitude * altitudeSink[layer], max);
        }

        /// <summary>
        /// Décalage vertical (u) à appliquer au décor pour poser sa ligne de sol sur le sol le plus bas de la vue (lissé),
        /// par rapport à sa place collée à l'écran. 0 hors partie ou sans terrain.
        /// </summary>
        float GroundLift(Vector3 c, float half, float halfH, int k)
        {
            var run = RunManager.Instance;
            var probe = CameraRig.Instance != null ? CameraRig.Instance.HeightProbe : null;
            if (run == null || !run.IsRunning || probe == null)
            {
                groundValid = false;
                return 0f;
            }
            float low = float.MaxValue;
            for (int s = 0; s <= 8; s++)
                low = Mathf.Min(low, probe(c.x - half + half * 2f * s / 8f));
            if (!groundValid || Mathf.Abs(low - groundSmooth) > halfH * 3f)
            {
                groundSmooth = low;                         // début de partie ou grand saut : on se pose sans glisser
                groundVel = 0f;
                groundValid = true;
            }
            else groundSmooth = Mathf.SmoothDamp(groundSmooth, low, ref groundVel, groundSmoothTime, Mathf.Infinity, Time.deltaTime);
            // ligne de sol d'un calque collé à l'écran (centré sur la caméra, 360 px de haut × k)
            float lockedGround = c.y + k * (layerGroundPx - LayerHalfPx) / PixelScreen.Ppu;
            // jamais au-dessus des 6/10 de la vue : le décor ne doit pas remplir l'écran de terre
            float target = Mathf.Min(groundSmooth, c.y + halfH * 0.2f);
            return target - lockedGround;
        }
    }
}
