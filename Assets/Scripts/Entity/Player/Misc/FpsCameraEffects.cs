using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FpsCameraEffects : MonoBehaviour
{
    [Header("References")]
    public FpsController controller;

    [Header("Headbob")]
    public bool enableHeadbob = true;
    public float bobFrequencyWalk = 1.8f;   // Hz à vitesse de marche
    public float bobFrequencySprint = 2.6f; // Hz à sprint
    public float bobAmpWalk = 0.03f;        // amplitude verticale
    public float bobAmpSprint = 0.06f;
    public float bobHorizontalScale = 0.5f; // 0..1 proportion latérale
    public float bobReturnLerp = 8f;        // retour au repos à l’arrêt

    [Header("FOV Kick")]
    public bool enableFovKick = true;
    public float baseFov = 60f;
    public float fovKickAtSprint = 8f;      // +FOV au sprint
    public float fovLerp = 8f;

    private Camera cam;
    private Vector3 defaultLocalPos;
    private float bobTimer;
    private float currentFov;

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
        if (enableHeadbob && grounded && speed01 > 0.05f)
        {
            // Lerp des paramètres en fonction de la “vitesse normalisée”
            float freq = Mathf.Lerp(bobFrequencyWalk, bobFrequencySprint, speed01);
            float ampV = Mathf.Lerp(bobAmpWalk, bobAmpSprint, speed01);
            float ampH = ampV * bobHorizontalScale;

            bobTimer += Time.deltaTime * (Mathf.PI * 2f) * freq;

            float bobY = Mathf.Sin(bobTimer) * ampV;                 // up/down
            float bobX = Mathf.Sin(bobTimer * 0.5f) * ampH;          // léger side sway (demi fréquence)

            targetOffset = new Vector3(bobX, bobY, 0f);
        }

        // Retour doux vers 0 quand on s’arrête ou en l’air
        Vector3 targetLocalPos = defaultLocalPos + targetOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, bobReturnLerp * Time.deltaTime);

        // --- FOV Kick ---
        if (enableFovKick)
        {
            float targetFov = baseFov + (fovKickAtSprint * Mathf.Clamp01(speed01 * 1.1f)); // accentue au sprint
            currentFov = Mathf.Lerp(currentFov, targetFov, fovLerp * Time.deltaTime);
            cam.fieldOfView = currentFov;
        }
    }
}
