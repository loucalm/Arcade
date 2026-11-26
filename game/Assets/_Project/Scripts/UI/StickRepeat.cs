namespace Kenta.UI
{
    /// <summary>
    /// Défilement au joystick dans un menu : un pas dès qu'on pousse, puis, si on maintient, un pas après
    /// <c>delay</c> et ensuite tous les <c>interval</c>. Revenir au centre réarme.
    /// </summary>
    public class StickRepeat
    {
        const float Threshold = 0.5f;
        readonly float delay, interval;
        int held;
        float timer;

        public StickRepeat(float delay = 0.4f, float interval = 0.15f)
        {
            this.delay = delay;
            this.interval = interval;
        }

        /// <summary>−1, 0 ou +1 pas pour cette image (axe : +1 = haut ou droite).</summary>
        public int Step(float axis, float dt)
        {
            int dir = axis > Threshold ? 1 : axis < -Threshold ? -1 : 0;
            if (dir == 0) { held = 0; return 0; }
            if (dir != held)
            {
                held = dir;
                timer = delay;
                return dir;
            }
            timer -= dt;
            if (timer > 0f) return 0;
            timer += interval;
            return dir;
        }

        /// <summary>Considère la direction actuelle comme déjà comptée (à l'ouverture d'un écran, joystick déjà poussé).</summary>
        public void Hold(float axis)
        {
            held = axis > Threshold ? 1 : axis < -Threshold ? -1 : 0;
            timer = delay;
        }
    }
}
