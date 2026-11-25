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

#if UNITY_EDITOR
        public static double Beat => UnityEditor.SessionState.GetFloat(Key, 0f);
#else
        public static double Beat => 0;
#endif
        public static bool Active => Beat > 0;
    }
}
