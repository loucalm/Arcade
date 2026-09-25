using RythmeRunner.Level;
using UnityEngine;

namespace RythmeRunner.Player
{
    /// <summary>
    /// Héros « sans membres » façon Rayman, construit une fois avec les formes du thème : corps, tête à grand nez,
    /// mèche, gants et baskets qui flottent. Toute l'animation est une fonction du beat (aucune minuterie) :
    /// un pas par demi-beat, rebond du corps, mèche qui bat, poing télescopique pendant la frappe.
    /// Hiérarchie plate (échelle 1 partout sauf sur les feuilles) pour que les tailles restent lisibles.
    /// </summary>
    public class PlayerRig : MonoBehaviour
    {
        [Header("Couleurs du héros")]
        [SerializeField] Color bodyColor = new(0.58f, 0.28f, 1f);
        [SerializeField] Color collarColor = Color.white;
        [SerializeField] Color scarfColor = new(1f, 0.25f, 0.35f);
        [SerializeField] Color skinColor = new(1f, 0.8f, 0.62f);
        [SerializeField] Color hairColor = new(1f, 0.78f, 0.2f);
        [SerializeField] Color gloveColor = Color.white;
        [SerializeField] Color shoeColor = new(1f, 0.82f, 0.15f);

        [Header("Proportions (unités, pieds à y = 0)")]
        [SerializeField] float bodyY = 0.5f;
        [SerializeField] float headY = 0.98f;
        [SerializeField] float hitReach = 1.9f;

        Transform root, body, head, leftHand, rightHand, leftFoot, rightFoot, leftHair, rightHair, rope, ropeGlow;
        Transform leftPupil, rightPupil, handHalo;
        Transform[] trail;
        SpriteRenderer auraRenderer;
        Transform aura;
        Color auraColor;
        Transform visualParent;
        bool built;

        public void UseParent(Transform parent) => visualParent = parent;

        public void Build(LevelTheme theme)
        {
            if (built || theme == null) return;
            built = true;
            Sprite circle = theme.circle, rounded = theme.rounded != null ? theme.rounded : theme.circle;
            Sprite triangle = theme.triangle != null ? theme.triangle : theme.square;
            Material additive = theme.additive;

            root = Node("NeonHero", visualParent != null ? visualParent : transform, Vector2.zero);
            auraColor = new Color(theme.player.r, theme.player.g, theme.player.b, 0.35f);
            aura = Leaf("Aura", root, theme.glow, auraColor, new Vector2(0f, 0.65f), new Vector2(2.1f, 2.1f), 15, additive);
            auraRenderer = aura.GetComponent<SpriteRenderer>();

            // Corps : violet, col blanc, foulard rouge
            body = Node("Body", root, new Vector2(0f, bodyY));
            Leaf("Torso", body, circle, bodyColor, Vector2.zero, new Vector2(0.52f, 0.56f), 20);
            Leaf("Collar", body, circle, collarColor, new Vector2(0.02f, 0.2f), new Vector2(0.46f, 0.2f), 21);
            Leaf("Scarf", body, rounded, scarfColor, new Vector2(-0.18f, 0.16f), new Vector2(0.26f, 0.1f), 22).localRotation = Quaternion.Euler(0f, 0f, 20f);

            // Tête : peau, grand nez vers l'avant, grands yeux, mèche en hélice
            head = Node("Head", root, new Vector2(0.03f, headY));
            leftHair = Leaf("HairL", head, triangle, hairColor, new Vector2(-0.12f, 0.3f), new Vector2(0.2f, 0.42f), 19);
            rightHair = Leaf("HairR", head, triangle, hairColor, new Vector2(0.06f, 0.32f), new Vector2(0.2f, 0.46f), 19);
            Leaf("Skull", head, circle, skinColor, Vector2.zero, new Vector2(0.5f, 0.46f), 23);
            Leaf("EyeL", head, circle, Color.white, new Vector2(0.02f, 0.06f), new Vector2(0.15f, 0.22f), 24);
            Leaf("EyeR", head, circle, Color.white, new Vector2(0.16f, 0.06f), new Vector2(0.15f, 0.22f), 24);
            leftPupil = Leaf("PupilL", head, circle, Color.black, new Vector2(0.05f, 0.05f), new Vector2(0.07f, 0.1f), 25);
            rightPupil = Leaf("PupilR", head, circle, Color.black, new Vector2(0.19f, 0.05f), new Vector2(0.07f, 0.1f), 25);
            Leaf("Nose", head, circle, new Color(skinColor.r, skinColor.g * 0.88f, skinColor.b * 0.8f), new Vector2(0.26f, -0.07f), new Vector2(0.2f, 0.16f), 26);

            // Gants et baskets qui flottent, chacun avec un halo
            leftHand = Limb("HandL", circle, gloveColor, new Vector2(0.22f, 0.22f), 18, theme.glow, additive);
            rightHand = Limb("HandR", circle, gloveColor, new Vector2(0.24f, 0.24f), 27, theme.glow, additive);
            leftFoot = Limb("FootL", rounded, shoeColor, new Vector2(0.34f, 0.2f), 18, theme.glow, additive);
            rightFoot = Limb("FootR", rounded, shoeColor, new Vector2(0.36f, 0.21f), 27, theme.glow, additive);

            // Poing télescopique : halo + traînée
            handHalo = Leaf("PunchHalo", root, theme.glow, new Color(1f, 1f, 1f, 0.6f), Vector2.zero, new Vector2(0.9f, 0.9f), 17, additive);
            trail = new Transform[3];
            for (int i = 0; i < trail.Length; i++)
                trail[i] = Leaf("Trail" + i, root, circle, new Color(1f, 1f, 1f, 0.5f - i * 0.12f), Vector2.zero, Vector2.one * (0.18f - i * 0.04f), 26 - i, additive);

            // Corde de liane
            ropeGlow = Leaf("RopeGlow", root, theme.glow, auraColor, Vector2.zero, Vector2.one, 16, additive);
            rope = Leaf("Rope", root, theme.square, theme.player, Vector2.zero, Vector2.one, 17);
            ShowPunch(false);
            rope.gameObject.SetActive(false);
            ropeGlow.gameObject.SetActive(false);
        }

        /// <summary>Appelé chaque frame par PlayerController. hitProgress : 0 → 1 pendant la frappe.</summary>
        public void Pose(double beat, PlayerPose pose, float hitProgress, Vector2 ropeAnchorLocal)
        {
            if (!built) return;
            bool wallRun = pose == PlayerPose.WallRun;
            // Un pied touche le sol à chaque demi-beat (deux fois plus vite sur un wall run).
            float phase = (float)(beat * (wallRun ? 2.0 : 1.0) * 2.0 * System.Math.PI);
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float beatFrac = (float)(beat - System.Math.Floor(beat));
            float downbeat = Mathf.Exp(-beatFrac * 6f) * ((int)System.Math.Floor(beat) % 4 == 0 ? 1f : 0.35f);

            Vector2 bodyPos = new(0f, bodyY), headPos = new(0.03f, headY);
            Vector2 lh, rh, lf, rf;
            float bodyTilt = 0f, headTilt;
            Vector3 bodyScale = Vector3.one;

            switch (pose)
            {
                case PlayerPose.Jump:
                    lf = new Vector2(-0.14f, 0.12f); rf = new Vector2(0.16f, 0.16f);
                    lh = new Vector2(-0.34f, 0.95f); rh = new Vector2(0.38f, 1.02f);
                    headTilt = 6f;
                    break;
                case PlayerPose.FastFall:
                    lf = new Vector2(-0.12f, -0.05f); rf = new Vector2(0.12f, -0.05f);
                    lh = new Vector2(-0.3f, 1.2f); rh = new Vector2(0.3f, 1.2f);
                    bodyScale = new Vector3(0.88f, 1.15f, 1f);
                    headTilt = 0f;
                    break;
                case PlayerPose.Slide:
                    bodyPos = new Vector2(0f, 0.3f); headPos = new Vector2(0.28f, 0.42f);
                    lf = new Vector2(0.5f, 0.08f); rf = new Vector2(0.72f, 0.1f);
                    lh = new Vector2(-0.45f, 0.35f); rh = new Vector2(-0.3f, 0.2f);
                    bodyTilt = -70f; headTilt = -25f;
                    break;
                case PlayerPose.Hook:
                    lf = new Vector2(-0.1f + s * 0.18f, 0.08f + c * 0.06f); rf = new Vector2(0.1f - s * 0.18f, 0.06f - c * 0.06f);
                    lh = new Vector2(-0.4f, 0.55f); rh = ropeAnchorLocal;
                    headTilt = 10f;
                    break;
                default: // Run, WallRun, Hit
                {
                    float lift = Mathf.Max(0f, s);
                    lf = new Vector2(-0.02f + c * 0.26f, 0.1f + lift * 0.2f);
                    rf = new Vector2(-0.02f - c * 0.26f, 0.1f + Mathf.Max(0f, -s) * 0.2f);
                    lh = new Vector2(-0.34f - c * 0.16f, 0.58f + s * 0.05f);
                    rh = new Vector2(0.36f + c * 0.16f, 0.58f - s * 0.05f);
                    float bounce = Mathf.Abs(Mathf.Sin(phase)) * 0.07f;
                    bodyPos.y += bounce;
                    headPos.y += Mathf.Abs(Mathf.Sin(phase - 0.5f)) * 0.07f; // léger retard de la tête
                    bodyTilt = wallRun ? -16f : -6f;
                    headTilt = -Mathf.Sin(phase - 0.5f) * 4f;
                    break;
                }
            }

            // Poing télescopique : le gant avant part jusqu'à hitReach puis revient.
            bool punching = pose == PlayerPose.Hit || hitProgress > 0f;
            if (punching)
            {
                float ext = Mathf.Sin(Mathf.Clamp01(hitProgress) * Mathf.PI) * hitReach;
                Vector2 from = new(0.36f, 0.6f);
                rh = from + new Vector2(ext, 0f);
                handHalo.localPosition = rh;
                for (int i = 0; i < trail.Length; i++)
                    trail[i].localPosition = Vector2.Lerp(from, rh, 0.25f + i * 0.22f);
            }
            ShowPunch(punching);

            body.localPosition = bodyPos;
            body.localRotation = Quaternion.Euler(0f, 0f, bodyTilt);
            body.localScale = bodyScale;
            head.localPosition = headPos;
            head.localRotation = Quaternion.Euler(0f, 0f, headTilt);
            leftHand.localPosition = lh;
            rightHand.localPosition = rh;
            leftFoot.localPosition = lf;
            rightFoot.localPosition = rf;
            leftFoot.localRotation = Quaternion.Euler(0f, 0f, pose == PlayerPose.Slide ? 0f : c * 12f);
            rightFoot.localRotation = Quaternion.Euler(0f, 0f, pose == PlayerPose.Slide ? 0f : -c * 12f);

            // Mèche en hélice : bat deux fois par beat, plus vite en l'air.
            float flap = Mathf.Sin(phase * (pose == PlayerPose.Jump ? 3f : 2f)) * 22f;
            leftHair.localRotation = Quaternion.Euler(0f, 0f, 25f + flap);
            rightHair.localRotation = Quaternion.Euler(0f, 0f, -10f - flap);

            // Regard vers l'avant, un peu vers le haut en l'air.
            float look = pose == PlayerPose.Jump || pose == PlayerPose.Hook ? 0.02f : 0f;
            leftPupil.localPosition = new Vector2(0.055f, 0.05f + look);
            rightPupil.localPosition = new Vector2(0.195f, 0.05f + look);

            // Aura qui flashe sur les temps (plus fort sur le premier temps de la mesure).
            float k = 1f + downbeat * 0.35f;
            aura.localScale = new Vector3(2.1f * k, 2.1f * k, 1f);
            auraRenderer.color = new Color(auraColor.r, auraColor.g, auraColor.b, auraColor.a * (0.7f + downbeat * 0.6f));

            if (pose == PlayerPose.Hook) SetRope(ropeAnchorLocal, rh);
            else if (rope.gameObject.activeSelf) { rope.gameObject.SetActive(false); ropeGlow.gameObject.SetActive(false); }
        }

        void ShowPunch(bool on)
        {
            if (handHalo.gameObject.activeSelf == on) return;
            handHalo.gameObject.SetActive(on);
            for (int i = 0; i < trail.Length; i++) trail[i].gameObject.SetActive(on);
        }

        Transform Limb(string name, Sprite sprite, Color color, Vector2 size, int order, Sprite glow, Material additive)
        {
            var t = Node(name, root, Vector2.zero);
            Leaf("Glow", t, glow, new Color(color.r, color.g, color.b, 0.45f), Vector2.zero, size * 2.2f, order - 1, additive);
            Leaf("Shape", t, sprite, color, Vector2.zero, size, order);
            return t;
        }

        static Transform Node(string name, Transform parent, Vector2 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            return t;
        }

        static Transform Leaf(string name, Transform parent, Sprite sprite, Color color, Vector2 position, Vector2 size, int order, Material material = null)
        {
            var t = Node(name, parent, position);
            t.localScale = new Vector3(size.x, size.y, 1f);
            var sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (material != null) sr.sharedMaterial = material;
            return t;
        }

        void SetRope(Vector2 anchor, Vector2 hand)
        {
            Vector2 delta = anchor - hand;
            float length = delta.magnitude;
            Vector2 center = hand + delta * 0.5f;
            var rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
            rope.localPosition = center;
            rope.localScale = new Vector3(0.07f, length, 1f);
            rope.localRotation = rot;
            ropeGlow.localPosition = center;
            ropeGlow.localScale = new Vector3(0.35f, length, 1f);
            ropeGlow.localRotation = rot;
            if (!rope.gameObject.activeSelf) { rope.gameObject.SetActive(true); ropeGlow.gameObject.SetActive(true); }
        }
    }
}
