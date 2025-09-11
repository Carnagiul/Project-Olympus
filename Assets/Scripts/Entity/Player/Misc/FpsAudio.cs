using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FpsController))]
public class FpsAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Source OneShot pour pas/saut/land")]
    public AudioSource oneShotSource;
    [Tooltip("Source loop pour slide sur pente raide (facultatif)")]
    public AudioSource loopSource;

    [Header("Footsteps (par défaut)")]
    public AudioClip[] footstepDefault;

    [Header("Footsteps par surface (optionnel)")]
    [Tooltip("Associe un Tag de collider à un set de pas")]
    public List<SurfaceSet> surfaceSets = new();

    [Serializable]
    public struct SurfaceSet
    {
        public string tag;
        public AudioClip[] footsteps;
    }

    [Header("Autres SFX")]
    public AudioClip jumpClip;
    public AudioClip landLightClip;
    public AudioClip landHardClip;
    public AudioClip slideLoopClip; // son en boucle quand on glisse sur pente raide

    [Header("Réglages Pas")]
    [Tooltip("Distance parcourue entre deux pas en marche (m)")]
    public float stepDistWalk = 1.8f;
    [Tooltip("Distance parcourue entre deux pas en sprint (m)")]
    public float stepDistSprint = 2.2f;
    [Tooltip("Distance parcourue entre deux pas accroupi (m)")]
    public float stepDistCrouch = 1.2f;
    [Tooltip("Variation de pitch aléatoire ±")]
    public float footstepPitchJitter = 0.06f;
    [Tooltip("Volume des pas")]
    [Range(0f, 1f)] public float footstepVolume = 0.8f;

    [Header("Seuils détection")]
    [Tooltip("Vitesse minimale (m/s) pour déclencher les pas")]
    public float minMoveSpeed = 0.2f;
    [Tooltip("Seuil d'impact vertical pour LAND HARD")]
    public float hardLandVelY = -10f;

    [Header("Raycast sol")]
    public LayerMask groundLayers = ~0;
    public float raycastDown = 0.6f;

    private FpsController ctrl;
    private CharacterController cc;

    // Accumulateur distance pour cadence des pas
    private Vector3 lastPos;
    private float stepAccumulator;

    // États précédents
    private bool prevGrounded;
    private bool slidingLoopActive;

    // Cache map tag -> clips
    private Dictionary<string, AudioClip[]> map;

    void Awake()
    {
        ctrl = GetComponent<FpsController>();
        cc = GetComponent<CharacterController>();
        lastPos = transform.position;

        map = new Dictionary<string, AudioClip[]>(StringComparer.Ordinal);
        foreach (var s in surfaceSets)
        {
            if (!string.IsNullOrEmpty(s.tag) && s.footsteps != null)
                map[s.tag] = s.footsteps;
        }

        // Sécurité: crée une source OneShot si absente
        if (oneShotSource == null)
        {
            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.spatialBlend = 1f;
            oneShotSource.rolloffMode = AudioRolloffMode.Linear;
            oneShotSource.maxDistance = 20f;
        }

        if (loopSource == null && slideLoopClip != null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.loop = true;
            loopSource.spatialBlend = 1f;
            loopSource.rolloffMode = AudioRolloffMode.Linear;
            loopSource.maxDistance = 25f;
            loopSource.clip = slideLoopClip;
        }
    }

    void Update()
    {
        HandleFootsteps();
        HandleSlideLoop();
        HandleLanding();
    }

    // --- Pas synchronisés sur la distance parcourue au sol ---
    void HandleFootsteps()
    {
        // Distance horizontale depuis la dernière frame
        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos;
        delta.y = 0f;
        float planarSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = pos;

        // Conditions: doit être au sol, bouger assez vite, et toucher une pente marchable ou surface quelconque
        if (!ctrl.IsGroundedForCamera || planarSpeed < minMoveSpeed)
        {
            // reset doux pour éviter un pas immédiat après arrêt
            stepAccumulator = Mathf.Clamp(stepAccumulator - 2f * Time.deltaTime, 0f, 999f);
            return;
        }

        // Détermine la "foulée" cible selon la démarche (crouch, walk, sprint)
        float stepDist = GetCurrentStepDistance();

        // Ajoute la distance parcourue
        stepAccumulator += delta.magnitude;

        if (stepAccumulator >= stepDist)
        {
            stepAccumulator = 0f;
            PlayFootstepOneShot();
        }
    }

    float GetCurrentStepDistance()
    {
        // Heuristique: si capsule proche de la hauteur accroupie → crouch
        bool isCrouch = ctrl.enableCrouch && Mathf.Abs(cc.height - ctrl.crouchHeight) < 0.15f;

        // Sprint approximé: vitesse normalisée élevée
        bool isSprint = !isCrouch && ctrl.CurrentPlanarSpeed01 > 0.75f;

        if (isCrouch) return stepDistCrouch;
        if (isSprint) return stepDistSprint;

        // Interpolation douce selon la vitesse
        return Mathf.Lerp(stepDistCrouch, stepDistWalk, Mathf.Clamp01(ctrl.CurrentPlanarSpeed01 * 1.2f));
    }

    void PlayFootstepOneShot()
    {
        var clips = ResolveSurfaceFootsteps();
        if (clips == null || clips.Length == 0) clips = footstepDefault;
        if (clips == null || clips.Length == 0) return;

        var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        oneShotSource.pitch = 1f + UnityEngine.Random.Range(-footstepPitchJitter, footstepPitchJitter);

        // Volume un peu plus fort en sprint, plus faible en crouch
        float v = footstepVolume;
        if (Mathf.Abs(cc.height - ctrl.crouchHeight) < 0.15f) v *= 0.65f;
        else if (ctrl.CurrentPlanarSpeed01 > 0.8f) v *= 1.0f;
        oneShotSource.PlayOneShot(clip, v);
    }

    AudioClip[] ResolveSurfaceFootsteps()
    {
        // Raycast vers le bas pour lire le Tag de la surface
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDown, groundLayers, QueryTriggerInteraction.Ignore))
        {
            if (map.TryGetValue(hit.collider.tag, out var arr))
                return arr;
        }
        return footstepDefault;
    }

    // --- Slide loop quand on est sur une pente trop raide (si tu utilises ce comportement) ---
    void HandleSlideLoop()
    {
        if (slideLoopClip == null || loopSource == null) return;

        // On considère "slide" si Grounded ET vitesse horizontale > min ET pente non marchable.
        // Le contrôleur expose seulement IsGroundedForCamera; on estime "non marchable" via cc.slopeLimit
        bool isSliding = ctrl.IsGroundedForCamera && cc.slopeLimit < 89f && CurrentGroundAngle() > cc.slopeLimit + 0.5f;

        if (isSliding && !slidingLoopActive)
        {
            loopSource.Play();
            slidingLoopActive = true;
        }
        else if (!isSliding && slidingLoopActive)
        {
            loopSource.Stop();
            slidingLoopActive = false;
        }

        // Ajuste le volume du slide selon la vitesse au sol
        if (slidingLoopActive)
        {
            float planarSpeed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
            loopSource.volume = Mathf.Clamp01(planarSpeed / 8f);
        }
    }

    // Petite estimation d'angle sous les pieds pour la slide (optionnel)
    float CurrentGroundAngle()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1f, groundLayers, QueryTriggerInteraction.Ignore))
        {
            return Vector3.Angle(hit.normal, Vector3.up);
        }
        return 0f;
    }

    // --- Atterrissage (light/hard) ---
    private float prevVelY;

    void LateUpdate()
    {
        // stocker velY après Update pour next frame (CharacterController expose velocity)
        prevVelY = cc.velocity.y;
    }

    void HandleLanding()
    {
        bool grounded = ctrl.IsGroundedForCamera;

        if (grounded && !prevGrounded)
        {
            // On vient d'atterrir : choisir light/hard selon la vitesse de chute (prevVelY était négative)
            var clip = (prevVelY <= hardLandVelY && landHardClip != null) ? landHardClip : landLightClip;
            if (clip != null)
            {
                oneShotSource.pitch = 1f;
                oneShotSource.PlayOneShot(clip, 1f);
            }
        }

        prevGrounded = grounded;
    }

    // --- Appelée par le controller quand le saut démarre ---
    public void OnJump()
    {
        if (jumpClip != null)
        {
            oneShotSource.pitch = 1f;
            oneShotSource.PlayOneShot(jumpClip, 1f);
        }
    }
}
