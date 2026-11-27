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
                SessionState.EraseBool(DevStart.AutoplayKey);
            };
        }

        /// <summary>Le niveau 9, La Sourdine (ADR-040) : depuis la fissure, juste avant que le monde perde sa couleur.</summary>
        [MenuItem("Kenta/Tester le niveau 9 (La Sourdine)")]
        static void PlayLevel9() => PlayFrom(26f, "sourd");

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

        /// <summary>Le niveau 4 (La Forge, tambours, rampes, arrière-plan) du début, joué à la main.</summary>
        [MenuItem("Kenta/Tester le niveau 4 (La Forge)")]
        static void PlayLevel4() => PlayFrom(1f, "forge");

        /// <summary>Le niveau 4 en entier joué par l'IA (validation de la chart dans le vrai jeu).</summary>
        [MenuItem("Kenta/Tester le niveau 4 en autoplay")]
        static void PlayLevel4Auto() => PlayFrom(1f, "forge", true);

        static void PlayFrom(float beat, string level = "", bool autoplay = false)
        {
            if (EditorApplication.isPlaying) return;
            SessionState.SetFloat(DevStart.Key, beat);
            SessionState.SetString(DevStart.LevelKey, level);
            SessionState.SetBool(DevStart.AutoplayKey, autoplay);
            EditorApplication.EnterPlaymode();
        }
    }
}
