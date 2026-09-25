using System;
using RythmeRunner.Core;
using RythmeRunner.Level;
using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>Orchestre les transitions visuelles liées aux changements de section.</summary>
    public class DropDirector : MonoBehaviour
    {
        [SerializeField] Backdrop backdrop;
        [SerializeField] CameraRig cameraRig;
        [SerializeField] Transform debrisPoint;
        [SerializeField] string[] dropSections = { "refrain", "drop", "final" };
        [SerializeField] int dropBars = 2;
        [SerializeField] float dropZoom = 0.85f;
        [Tooltip("Décalage vertical de la caméra pendant un drop, relatif à son offset de base.")]
        [SerializeField] float dropYOffset = -0.6f;
        [SerializeField] Color dropColor = new(0.3f, 0.8f, 1f, 1f);

        RunManager runManager;
        Conductor conductor;
        double framingEndBeat;
        bool framingActive;

        void Start() => TrySubscribe();

        void OnDestroy()
        {
            if (runManager != null)
            {
                runManager.OnSectionChanged -= OnSectionChanged;
                runManager.OnRespawned -= ResetEffects;
                runManager.OnRunStarted -= ResetEffects;
            }
        }

        void TrySubscribe()
        {
            if (runManager == null && RunManager.Instance != null)
            {
                runManager = RunManager.Instance;
                runManager.OnSectionChanged += OnSectionChanged;
                runManager.OnRespawned += ResetEffects;
                runManager.OnRunStarted += ResetEffects;
            }
            if (conductor == null) conductor = Conductor.Instance;
            if (cameraRig == null) cameraRig = CameraRig.Instance;
        }

        void Update()
        {
            TrySubscribe();
            if (!framingActive || conductor == null) return;
            if (conductor.SongBeat < framingEndBeat) return;
            framingActive = false;
            cameraRig?.ResetFraming(0.25f);
        }

        void ResetEffects()
        {
            framingActive = false;
            cameraRig?.ResetFraming(0f);
            backdrop?.ResetCollapse();
        }

        void OnSectionChanged(ChartSection section, bool perfect, int bonus)
        {
            if (conductor == null) conductor = Conductor.Instance;
            if (conductor == null) return;
            if (IsDrop(section.name))
            {
                int bars = Mathf.Max(1, dropBars);
                float measureSeconds = (float)(conductor.SecondsPerBeat * conductor.BeatsPerBar);
                backdrop?.Collapse(measureSeconds * bars);
                cameraRig?.Shake(0.35f, 0.35f);
                cameraRig?.Punch(0.25f);
                cameraRig?.Flash(dropColor, 1f);
                cameraRig?.SetFraming(dropZoom, cameraRig.BaseYOffset + dropYOffset, 0.25f);
                framingEndBeat = conductor.SongBeat + bars * conductor.BeatsPerBar;
                framingActive = true;
                Vector3 point = debrisPoint != null ? debrisPoint.position : transform.position;
                FxPool.Instance?.Burst(point, dropColor, 36, 9f, 0.22f, 0.7f, 13f, 0.5f);
            }
            else
            {
                cameraRig?.Punch(0.1f);
                cameraRig?.Flash(dropColor, 0.45f);
            }
        }

        bool IsDrop(string sectionName)
        {
            if (dropSections == null) return false;
            for (int i = 0; i < dropSections.Length; i++)
                if (string.Equals(sectionName, dropSections[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
