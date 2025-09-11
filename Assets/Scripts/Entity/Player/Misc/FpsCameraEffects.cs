using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FpsCameraEffects : MonoBehaviour
{
    [Header("References")]
    public FpsController controller;

    [Header("Headbob")]
    public bool enableHeadbob = true;
    public float bobFrequencyWalk = 1.8f;
    public float bobFrequencySprint = 2.6f;
    public float bobAmpWalk = 0.03f;
    public float bobAmpSprint = 0.06f;
    public float bobHorizontalScale = 0.5f;
    public float bobReturnLerp = 8f;

    [Header("FOV Kick")]
    public bool enableFovKick = true;
    public float baseFov = 60f;
    public float fovKickAtSprint = 8f;
    public float fovLerp = 8f;

    private Camera cam;
    private Vector3 defaultLocalPos;
    private float bobTimer;
    private float currentFov;

    // === NOUVEAU : exposé pour audio ===
    public float BobPhase01 { get; private set; }  // 0..1 (0 et 0.5 = impacts G/D)
    public bool BobActive { get; private set; }  // moving & grounded & headbob on

    void Awake()
    {
        cam = GetComponent<Camera>();
        defaultLocalPos = transform.localPosition;
        if (baseFov <= 0f) baseFov = cam.fieldOfView;
        cam.fieldOfView = baseFov;
        currentFov = baseFov;
    }

    void LateUpdate()
    {
        if (controller == null) return;

        float speed01 = controller.CurrentPlanarSpeed01;
        bool grounded = controller.IsGroundedForCamera;

        // --- Headbob ---
        Vector3 targetOffset = Vector3.zero;
        float freq = Mathf.Lerp(bobFrequencyWalk, bobFrequencySprint, speed01);
        float ampV = Mathf.Lerp(bobAmpWalk, bobAmpSprint, speed01);
        float ampH = ampV * bobHorizontalScale;

        BobActive = enableHeadbob && grounded && speed01 > 0.05f;

        if (BobActive)
        {
            bobTimer += Time.deltaTime * (Mathf.PI * 2f) * freq;

            float bobY = Mathf.Sin(bobTimer) * ampV;
            float bobX = Mathf.Sin(bobTimer * 0.5f) * ampH;

            targetOffset = new Vector3(bobX, bobY, 0f);

            // Phase normalisée 0..1
            float twoPi = Mathf.PI * 2f;
            float t = bobTimer % twoPi; if (t < 0f) t += twoPi;
            BobPhase01 = t / twoPi; // 0.0 -> 1.0
        }
        else
        {
            // retour quand on ne bouge pas
            BobPhase01 = 0f;
        }

        Vector3 targetLocalPos = defaultLocalPos + targetOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, bobReturnLerp * Time.deltaTime);

        // --- FOV Kick ---
        if (enableFovKick)
        {
            float targetFov = baseFov + (fovKickAtSprint * Mathf.Clamp01(speed01 * 1.1f));
            currentFov = Mathf.Lerp(currentFov, targetFov, fovLerp * Time.deltaTime);
            cam.fieldOfView = currentFov;
        }
    }
}
