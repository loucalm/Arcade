using Kenta.Core;
using UnityEditor;

namespace Kenta.EditorTools
{
    /// <summary>Raccourcis de test : lancer le jeu directement sur un passage du niveau 01.</summary>
    [InitializeOnLoad]
    static class KentaDevMenu
    {
        /// <summary>Le prototype du refrain (beats 96-128), précédé du roulement (80-96) : 1,2 s avant la première action.</summary>
        const float RefrainBeat = 78.5f;

        static KentaDevMenu()
        {
            // en revenant à l'éditeur, le bouton Play retrouve le flow normal (accueil, consignes…)
            EditorApplication.playModeStateChanged += s =>
            {
                if (s != PlayModeStateChange.EnteredEditMode) return;
                SessionState.EraseFloat(DevStart.Key);
                SessionState.EraseString(DevStart.LevelKey);
            };
        }

        /// <summary>Le niveau 9, La Sourdine (ADR-040) : depuis la fissure, juste avant que le monde perde sa couleur.</summary>
        [MenuItem("Kenta/Tester le niveau 9 (La Sourdine)")]
        static void PlayLevel9() => PlayFrom(26f, "sourd");

        /// <summary>Le niveau 8, l'Orage : juste avant le drop (96), où les éclairs tombent tous les 2 temps (ADR-041).</summary>
        [MenuItem("Kenta/Tester le niveau 8 (éclairs de l'Orage)")]
        static void PlayLevel8() => PlayFrom(92f, "orage");

        /// <summary>Le niveau 1, la Sylve : le soleil se couche, puis la nuit tombe à la section fin (128, ADR-041).</summary>
        [MenuItem("Kenta/Tester le niveau 1 (coucher du soleil, nuit)")]
        static void PlayLevel1Night() => PlayFrom(108f, "sylve");

        [MenuItem("Kenta/Tester le refrain (prototype)")]
        static void PlayRefrain() => PlayFrom(RefrainBeat);

        /// <summary>L'intro et le couplet : cuirassés (on saute dessus et on appuie au rebond) et notes de saut (0-64).</summary>
        [MenuItem("Kenta/Tester l'intro et le couplet (cuirassés, notes de saut)")]
        static void PlayIntro() => PlayFrom(1f);

        /// <summary>Le pont : de liane en liane au-dessus de l'eau (64-80), on saute pour lâcher chaque liane.</summary>
        [MenuItem("Kenta/Tester le pont (lianes)")]
        static void PlayVines() => PlayFrom(58.2f);

        /// <summary>Le final : gravité inversée puis arrière-plan (128-152).</summary>
        [MenuItem("Kenta/Tester le final (gravité, arrière-plan)")]
        static void PlayFinal() => PlayFrom(127.9f);

        /// <summary>Le refrain 2 : écran dédoublé (2, puis 5) puis grésillement (216-256).</summary>
        [MenuItem("Kenta/Tester le refrain 2 (effets d'écran)")]
        static void PlayScreenFx() => PlayFrom(214.2f);

        static void PlayFrom(float beat, string level = "")
        {
            if (EditorApplication.isPlaying) return;
            SessionState.SetFloat(DevStart.Key, beat);
            SessionState.SetString(DevStart.LevelKey, level);
            EditorApplication.EnterPlaymode();
        }
    }
}
