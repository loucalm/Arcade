namespace Kenta.Core
{
    /// <summary>
    /// Test d'un passage du niveau dans l'éditeur (menu Kenta > Tester le refrain) : la partie démarre directement à
    /// ce beat (musique, héros, caméra), sans passer par les écrans, et après une mort un appui relance au même endroit.
    /// Dans un build, toujours 0 : le jeu suit le flow normal de la borne.
    /// </summary>
    public static class DevStart
    {
        public const string Key = "Kenta.DevStartBeat";
        /// <summary>Niveau du test (`LevelDef.id`, ex. « sourd ») ; vide = le premier niveau du catalogue.</summary>
        public const string LevelKey = "Kenta.DevStartLevel";

#if UNITY_EDITOR
        public static double Beat => UnityEditor.SessionState.GetFloat(Key, 0f);
        public static string LevelId => UnityEditor.SessionState.GetString(LevelKey, "");
#else
        public static double Beat => 0;
        public static string LevelId => "";
#endif
        public static bool Active => Beat > 0;
    }
}
