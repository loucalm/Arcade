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
                if (s == PlayModeStateChange.EnteredEditMode) SessionState.EraseFloat(DevStart.Key);
            };
        }

        [MenuItem("Kenta/Tester le refrain (prototype)")]
        static void PlayRefrain() => PlayFrom(RefrainBeat);

        /// <summary>Le pont : de liane en liane au-dessus de l'eau (64-80), après la descente du couplet.</summary>
        [MenuItem("Kenta/Tester le pont (lianes)")]
        static void PlayVines() => PlayFrom(58.2f);

        /// <summary>Le final : gravité inversée puis arrière-plan (128-152).</summary>
        [MenuItem("Kenta/Tester le final (gravité, arrière-plan)")]
        static void PlayFinal() => PlayFrom(127.9f);

        /// <summary>Le refrain 2 : écran dédoublé (2, puis 5) puis grésillement (216-256).</summary>
        [MenuItem("Kenta/Tester le refrain 2 (effets d'écran)")]
        static void PlayScreenFx() => PlayFrom(214.2f);

        static void PlayFrom(float beat)
        {
            if (EditorApplication.isPlaying) return;
            SessionState.SetFloat(DevStart.Key, beat);
            EditorApplication.EnterPlaymode();
        }
    }
}
