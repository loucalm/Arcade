#if !UNITY_EDITOR
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Kenta.Core
{
    /// <summary>
    /// Build seulement : fait le travail du MenuManager du toolkit (bouton blanc tenu 1,5 s ou 60 s sans input →
    /// retour au menu de la borne, texte « Retour au menu… »), avec les mêmes règles, mais en lisant les commandes par
    /// ArcadeInput. Le MenuManager lit l'Input Manager d'Unity, qui ne voit pas une manette hors de l'index 0 du
    /// navigateur : il aurait cru le joueur absent et l'aurait renvoyé au menu au bout de 60 s, en pleine partie.
    /// Le MenuManager reste dans la scène (règle de la borne) ; il est seulement désactivé au lancement et remplacé ici.
    /// En éditeur, c'est EditorArcadeShim qui s'en charge.
    /// </summary>
    public class ArcadeMenu : MonoBehaviour
    {
        const float AfkTime = 60f;
        const float HeldQuitTime = 1.5f;
        const string MenuMessage = "Retour au menu";

        TMP_Text quitText;
        float afk, held;
        bool leaving;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var managers = FindObjectsByType<MenuManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (managers.Length == 0) return;
            var field = typeof(MenuManager).GetField("quitText", BindingFlags.Instance | BindingFlags.NonPublic);
            TMP_Text text = null;
            foreach (var m in managers)
            {
                text ??= field?.GetValue(m) as TMP_Text;
                m.enabled = false;
            }
            var go = new GameObject(nameof(ArcadeMenu));
            DontDestroyOnLoad(go);
            go.AddComponent<ArcadeMenu>().quitText = text;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            held = ArcadeInput.CoinHeld() ? held + dt : 0f;
            afk = ArcadeInput.AnyActivity() ? 0f : afk + dt;
            if (!leaving && (held >= HeldQuitTime || afk >= AfkTime))
            {
                leaving = true;
                MenuManager.BackToMenu();
            }
            if (quitText == null) return;
            bool show = held > 0f || afk - AfkTime + 6f > 0f;
            quitText.gameObject.SetActive(show);
            if (show) quitText.text = MenuMessage + new string('.', (int)Mathf.Min(Mathf.Max(held * 3f, afk - AfkTime + 4f), 3));
        }
    }
}
#endif
