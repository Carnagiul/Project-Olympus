using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FpsController))]
public class FpsAudio : MonoBehaviour
{
    [Header("References")]
    public FpsCameraEffects cameraEffects;      // pour lire la phase du headbob

    [Header("Cadence")]
    [Tooltip("Intervalle minimal entre deux pas (sec)")]
    public float minStepIntervalSeconds = 0.5f;
    [Tooltip("Multiplicateur d'intervalle en sprint (<1 = plus rapide)")]
    public float sprintIntervalMultiplier = 0.60f;
    [Tooltip("Multiplicateur d'intervalle en crouch (>1 = plus lent)")]
    public float crouchIntervalMultiplier = 1.40f;

    private float nextStepTime = 0f; // horloge Time.time quand un pas est autorisé

    [Header("Audio Sources (stéréo)")]
    [Tooltip("Source pied gauche (auto-créée si vide)")]
    public AudioSource leftSource;
    [Tooltip("Source pied droit (auto-créée si vide)")]
    public AudioSource rightSource;

    [Header("Footsteps (défaut)")]
    public AudioClip[] footstepDefault;

    [Header("Footsteps par surface")]
    [Tooltip("Associe Tag -> set de pas + multiplicateur de volume")]
    public List<SurfaceSet> surfaceSets = new();

    [Serializable]
    public struct SurfaceSet
    {
        public string tag;
        public AudioClip[] footsteps;
        [Range(0.2f, 2f)] public float volumeScale; // 0.2..2.0 (ex: Metal 1.2, Grass 0.8)
    }

    [Header("Autres SFX")]
    public AudioClip jumpClip;
    public AudioClip landLightClip;
    public AudioClip landHardClip;
    public AudioClip slideLoopClip; // optionnel (pente raide)

    [Header("Réglages généraux")]
    [Range(0f, 1f)] public float baseFootstepVolume = 0.8f;
    public float footstepPitchJitter = 0.06f;
    public float hardLandVelY = -10f;      // seuil pour gros impact
    public float minMoveSpeed = 0.2f;      // seuil pour déclencher pas

    [Header("Raycast sol")]
    public LayerMask groundLayers = ~0;
    public float raycastDown = 0.8f;

    [Header("Reverb Zones")]
    [Tooltip("Mix des Reverb Zones (0=off, 1=plein). Laisse >=1 pour entendre la reverb de la scène.")]
    [Range(0f, 1.1f)] public float reverbZoneMix = 1.0f;

    [Header("Stéréo / placement pieds")]
    public float footOffsetX = 0.18f; // écart latéral des sources (m)
    public float footOffsetY = 0.0f;  // hauteur relative

    private FpsController ctrl;
    private CharacterController cc;

    // Slide loop
    private AudioSource slideLoopSource;
    private bool slidingLoopActive;

    // Surface map
    private Dictionary<string, (AudioClip[] clips, float vol)> map;

    // Landing
    private bool prevGrounded;
    private float prevVelY;

    // Synchro headbob
    private float prevPhase;  // 0..1
    private bool leftNext = true; // alterne G/D à chaque impact (0.0 et 0.5)

    void Awake()
    {
        ctrl = GetComponent<FpsController>();
        cc = GetComponent<CharacterController>();

        // Map surfaces
        map = new Dictionary<String, (AudioClip[], float)>(StringComparer.Ordinal);
        foreach (var s in surfaceSets)
        {
            if (!string.IsNullOrEmpty(s.tag) && s.footsteps != null)
                map[s.tag] = (s.footsteps, s.volumeScale <= 0f ? 1f : s.volumeScale);
        }

        // Auto-création sources G/D si besoin
        if (leftSource == null) leftSource = CreateChildSource("Foot_L", new Vector3(-footOffsetX, footOffsetY, 0f));
        if (rightSource == null) rightSource = CreateChildSource("Foot_R", new Vector3(footOffsetX, footOffsetY, 0f));

        // Slide loop source
        if (slideLoopClip != null)
        {
            slideLoopSource = CreateChildSource("SlideLoop", Vector3.zero, loop: true);
            slideLoopSource.clip = slideLoopClip;
        }

        // Init reverb mix
        ApplyReverbMix(leftSource);
        ApplyReverbMix(rightSource);
        if (slideLoopSource) ApplyReverbMix(slideLoopSource);
    }

    AudioSource CreateChildSource(string name, Vector3 localPos, bool loop = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 1f;                // 3D pour bénéficier des Reverb Zones
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1.0f;
        src.maxDistance = 20f;
        src.spread = 0f;
        return src;
    }

    void ApplyReverbMix(AudioSource src)
    {
        if (src == null) return;
        src.reverbZoneMix = reverbZoneMix; // 1.0 = mix par défaut des zones
    }

    void Update()
    {
        HandleHeadbobSynchronizedSteps();
        HandleSlideLoop();
        HandleLanding();
    }

    float CurrentStepInterval()
    {
        // Détecte crouch/sprint comme dans FpsController
        bool isCrouch = ctrl.enableCrouch && Mathf.Abs(cc.height - ctrl.crouchHeight) < 0.15f;
        bool isSprint = !isCrouch && ctrl.CurrentPlanarSpeed01 > 0.80f;

        // Multiplicateur
        float mult;
        if (isCrouch) mult = Mathf.Max(0.1f, crouchIntervalMultiplier);
        else if (isSprint) mult = Mathf.Max(0.1f, sprintIntervalMultiplier);
        else
        {
            // Marche: possibilité d'interpoler légèrement vers sprint en fonction de la vitesse
            // (optionnel, doux et invisible si tu préfères strictement 1.0s en marche, supprime ce bloc)
            float t = Mathf.InverseLerp(0.55f, 0.90f, ctrl.CurrentPlanarSpeed01); // 0 à 1 quand on se rapproche du sprint
            float sprintMult = Mathf.Max(0.1f, sprintIntervalMultiplier);
            mult = Mathf.Lerp(1f, sprintMult, Mathf.Clamp01(t));
        }

        return Mathf.Max(0.05f, minStepIntervalSeconds * mult);
    }


    // === 1) PAS synchronisés sur la phase du headbob ===
    void HandleHeadbobSynchronizedSteps()
    {
        if (cameraEffects == null || !cameraEffects.BobActive)
        {
            prevPhase = 0f;
            // Réarme légèrement pour éviter un pas instantané à la reprise
            nextStepTime = Mathf.Max(nextStepTime, Time.time + 0.05f);
            return;
        }

        float planarSpeed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
        bool moving = ctrl.IsGroundedForCamera && planarSpeed >= minMoveSpeed;

        if (!moving)
        {
            prevPhase = cameraEffects.BobPhase01;
            nextStepTime = Mathf.Max(nextStepTime, Time.time + 0.05f);
            return;
        }

        float phase = cameraEffects.BobPhase01; // 0..1
        float interval = CurrentStepInterval();
        bool canStep = Time.time >= nextStepTime;

        // Triggers synchronisés au headbob: ~0.00 et ~0.50 (gauche/droite)
        bool hit0 = canStep && Crossed(prevPhase, phase, 0.00f);
        bool hit05 = canStep && Crossed(prevPhase, phase, 0.50f);

        if (hit0 || hit05)
        {
            PlayFootstepStereo(leftNext);
            leftNext = !leftNext;
            nextStepTime = Time.time + interval; // impose l’intervalle dynamique
        }

        prevPhase = phase;
    }


    bool Crossed(float prev, float cur, float trigger)
    {
        // Normalize
        prev = Mathf.Repeat(prev, 1f);
        cur = Mathf.Repeat(cur, 1f);
        if (prev <= cur) return prev < trigger && trigger <= cur;
        else return prev < trigger || trigger <= cur; // wrap-around
    }

    void PlayFootstepStereo(bool left)
    {
        var (clips, volScale) = ResolveSurfaceSet();
        if (clips == null || clips.Length == 0) clips = footstepDefault;
        if (clips == null || clips.Length == 0) return;

        var clip = clips[UnityEngine.Random.Range(0, clips.Length)];

        // Base volume modifié par surface, crouch/sprint, et un petit random pitch
        float v = baseFootstepVolume * volScale;
        if (Mathf.Abs(cc.height - ctrl.crouchHeight) < 0.15f) v *= 0.65f;
        else if (ctrl.CurrentPlanarSpeed01 > 0.8f) v *= 1.0f;

        var src = left ? leftSource : rightSource;
        if (!src) return;

        src.pitch = 1f + UnityEngine.Random.Range(-footstepPitchJitter, footstepPitchJitter);
        src.PlayOneShot(clip, v);
    }

    (AudioClip[], float) ResolveSurfaceSet()
    {
        Vector3 origin = transform.position + Vector3.up * 0.15f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDown, groundLayers, QueryTriggerInteraction.Ignore))
        {
            if (map.TryGetValue(hit.collider.tag, out var tuple))
                return tuple;
        }
        // défaut : clips + volumeScale=1
        return (footstepDefault, 1f);
    }

    // === 2) Loop de glissade sur pente raide ===
    void HandleSlideLoop()
    {
        if (slideLoopSource == null) return;
        bool isSliding = ctrl.IsGroundedForCamera && cc.slopeLimit < 89f && CurrentGroundAngle() > cc.slopeLimit + 0.5f;

        if (isSliding && !slidingLoopActive) { slideLoopSource.Play(); slidingLoopActive = true; }
        else if (!isSliding && slidingLoopActive) { slideLoopSource.Stop(); slidingLoopActive = false; }

        if (slidingLoopActive)
        {
            float planarSpeed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
            slideLoopSource.volume = Mathf.Clamp01(planarSpeed / 8f);
        }
    }

    float CurrentGroundAngle()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.2f, groundLayers, QueryTriggerInteraction.Ignore))
            return Vector3.Angle(hit.normal, Vector3.up);
        return 0f;
    }

    // === 3) Atterrissage light/hard ===
    void LateUpdate()
    {
        prevVelY = cc.velocity.y;
    }

    void HandleLanding()
    {
        bool grounded = ctrl.IsGroundedForCamera;
        if (grounded && !prevGrounded)
        {
            var clip = (prevVelY <= hardLandVelY && landHardClip != null) ? landHardClip : landLightClip;
            var src = leftNext ? leftSource : rightSource; // joue sur la dernière jambe “attendue”
            if (clip != null && src != null) { src.pitch = 1f; src.PlayOneShot(clip, 1f); }
        }
        prevGrounded = grounded;
    }

    // === 4) Appelée par le controller au début du saut ===
    public void OnJump()
    {
        var src = leftNext ? leftSource : rightSource;
        if (jumpClip != null && src != null) { src.pitch = 1f; src.PlayOneShot(jumpClip, 1f); }
    }
}
