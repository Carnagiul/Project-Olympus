using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FpsController : MonoBehaviour
{
    public enum PivotMode { Feet, Center }

    [Header("Status (optional)")]
    [SerializeField] private TMP_Text statusText; // TextMeshPro ou TextMeshProUGUI

    [Header("Capsule/Pivot")]
    public PivotMode pivotMode = PivotMode.Feet; // Choisis selon ton pivot objet

    [Header("Move")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.5f;
    public float crouchSpeed = 2.5f;
    public float acceleration = 12f;
    public float airControl = 0.5f;

    [Header("Jump/Gravity")]
    public float jumpHeight = 1.6f;
    public float gravity = -13.0f;
    public float groundedStick = -2f;

    [Header("Crouch")]
    public bool enableCrouch = true;
    public float crouchHeight = 1.0f;
    public float standHeight = 1.8f;
    public float crouchLerp = 12f;

    [Header("Ground Probe (advanced)")]
    public LayerMask groundLayers = ~0;
    public float groundCheckDistance = 0.30f; // distance sous les pieds
    public float maxSnapSpeed = 6f;           // vitesse max pour snap/stick
    public float maxSlopeAngle = 50f;         // pente marchable
    public float slopeSlideGravity = 20f;     // gravité parallèle sur pente trop raide
    public float stepOffsetSnap = 0.10f;      // marge pour petites marches/aspérités

    [Header("Ceiling Check")]
    public float ceilingProbeRadius = 0.26f;
    public float ceilingCheckDistance = 0.15f;

    private CharacterController cc;
    private Vector3 velocity;          // composante verticale + éventuel slide
    private float targetSpeed;
    private float currentSpeed;

    // Sol
    private bool isOnGround;           // état principal post-move
    private Vector3 groundNormal = Vector3.up;
    private float groundAngle;
    private bool onWalkableSlope;

    // Mouvement
    private Vector3 planarDirWorld;
    private Vector3 lastMoveXZ;

    // Audio (optionnel)
    private FpsAudio audioSfx;

    // Exposés pour la caméra (headbob/FOV)
    public float CurrentPlanarSpeed01 { get; private set; } // 0..1 par rapport au sprint
    public bool IsGroundedForCamera => isOnGround;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!enableCrouch) standHeight = cc.height;

        // HUD fallback
        if (statusText == null)
        {
            var go = GameObject.Find("Status");
            if (go != null) statusText = go.GetComponent<TMP_Text>();
        }

        audioSfx = GetComponent<FpsAudio>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // --- Inputs plan XZ ---
        float ix = 0f, iz = 0f;
        if (kb.aKey.isPressed || kb.qKey.isPressed) ix -= 1f;
        if (kb.dKey.isPressed) ix += 1f;
        if (kb.sKey.isPressed) iz -= 1f;
        if (kb.wKey.isPressed || kb.zKey.isPressed) iz += 1f;

        Vector3 wishDirLocal = new Vector3(ix, 0f, iz).normalized;

        bool wantSprint = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        bool wantCrouch = enableCrouch && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed || kb.cKey.isPressed);

        float baseSpeed = walkSpeed;
        if (wantSprint && !wantCrouch && wishDirLocal.z > 0.1f) baseSpeed = sprintSpeed;
        if (wantCrouch) baseSpeed = crouchSpeed;

        targetSpeed = baseSpeed * wishDirLocal.magnitude;

        // --- Probe avant Move : donne une normale de référence pour projeter l'input ---
        PreMoveGroundProbe();

        // --- Accélération lissée ---
        float accel = isOnGround ? acceleration : acceleration * airControl;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

        // Direction monde depuis input
        planarDirWorld = (transform.right * ix + transform.forward * iz);
        if (planarDirWorld.sqrMagnitude > 0f) planarDirWorld.Normalize();

        // Projeter sur le plan de la pente si marchable
        if (isOnGround && onWalkableSlope && planarDirWorld.sqrMagnitude > 0f)
            planarDirWorld = Vector3.ProjectOnPlane(planarDirWorld, groundNormal).normalized;

        Vector3 planarMove = planarDirWorld * currentSpeed;

        // --- Saut / Gravité / Slide ---
        if (kb.spaceKey.wasPressedThisFrame && isOnGround && !wantCrouch)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isOnGround = false;
            if (audioSfx != null) audioSfx.OnJump();
        }

        if (isOnGround)
        {
            if (onWalkableSlope)
            {
                if (velocity.y < 0f) velocity.y = groundedStick; // colle au sol
            }
            else
            {
                // Pente trop raide -> slide parallèle
                Vector3 downslope = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                velocity += downslope * slopeSlideGravity * Time.deltaTime;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // --- MOUVEMENT UNIQUE + flags ---
        Vector3 motion = (planarMove + velocity) * Time.deltaTime;
        CollisionFlags flags = cc.Move(motion);

        bool below = (flags & CollisionFlags.Below) != 0;
        bool above = (flags & CollisionFlags.Above) != 0;

        // État sol robuste post-move
        isOnGround = below;
        if (isOnGround && velocity.y < 0f) velocity.y = groundedStick;
        if (isOnGround && velocity.y > 0f) velocity.y = 0f;
        if (above && velocity.y > 0f) velocity.y = 0f;

        // --- Crouch / Uncrouch sécurisé + center selon pivot ---
        if (enableCrouch)
        {
            bool ceilingBlocked = CeilingBlocked();
            float targetH = (wantCrouch || ceilingBlocked) ? crouchHeight : standHeight;
            cc.height = Mathf.Lerp(cc.height, targetH, crouchLerp * Time.deltaTime);

            if (pivotMode == PivotMode.Feet)
                cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            else
                cc.center = Vector3.zero;
        }
        else
        {
            // même sans crouch, garantir un center cohérent
            if (pivotMode == PivotMode.Feet)
                cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            else
                cc.center = Vector3.zero;
        }

        // Sorties caméra & HUD
        lastMoveXZ = planarMove;
        float maxRef = Mathf.Max(walkSpeed, sprintSpeed);
        CurrentPlanarSpeed01 = Mathf.Clamp01(lastMoveXZ.magnitude / Mathf.Max(0.0001f, maxRef));

        if (statusText != null)
        {
            string moveState =
                !isOnGround ? "Air" :
                (onWalkableSlope ? (wantCrouch ? "Crouch" : (baseSpeed == sprintSpeed ? "Sprint" : "Walk")) : "Slide");

            statusText.text =
                $"State: {moveState}\n" +
                $"Grounded: {isOnGround}\n" +
                $"Slope: {groundAngle:0.0}° (≤ {maxSlopeAngle}°: {(onWalkableSlope ? "Yes" : "No")})\n" +
                $"Speed: {lastMoveXZ.magnitude:0.00} m/s\n" +
                $"VelY: {velocity.y:0.00}\n" +
                $"Pivot: {pivotMode}";
        }
    }

    // --- Probe avant Move : CapsuleCast aligné au CC ---
    void PreMoveGroundProbe()
    {
        float r = Mathf.Max(0.0f, cc.radius - 0.01f);
        GetCapsuleEnds(out Vector3 capTop, out Vector3 capBottom, r);

        float castDist = Mathf.Max(0.01f, groundCheckDistance + stepOffsetSnap);
        bool hit = Physics.CapsuleCast(
            capTop, capBottom, r,
            Vector3.down,
            out RaycastHit hitInfo,
            castDist,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        Vector3 newNormal = Vector3.up;
        float newAngle = 0f;
        bool walkable = false;

        if (hit)
        {
            newNormal = hitInfo.normal;
            newAngle = Vector3.Angle(newNormal, Vector3.up);
            walkable = newAngle <= maxSlopeAngle;

            // Stick doux si on tombe doucement et qu'on est presque au sol
            float planarSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            bool fallingGently = velocity.y <= 0f && planarSpeed <= maxSnapSpeed;
            bool nearGround = hitInfo.distance <= (groundCheckDistance + 0.02f);
            if (nearGround && walkable && fallingGently)
            {
                velocity.y = Mathf.Min(velocity.y, groundedStick);
            }
        }

        groundNormal = newNormal;
        groundAngle = newAngle;
        onWalkableSlope = walkable;
        // isOnGround sera fixé après Move via flags
    }

    bool CeilingBlocked()
    {
        float r = Mathf.Max(0.0f, cc.radius - 0.01f);
        GetCapsuleEnds(out Vector3 capTop, out _, r);
        // Cast au-dessus de la tête
        return Physics.SphereCast(capTop, ceilingProbeRadius, Vector3.up, out _, ceilingCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
    }

    // Construit les extrémités de la capsule du CC en espace monde (indépendant du pivot choisi)
    void GetCapsuleEnds(out Vector3 capTop, out Vector3 capBottom, float r)
    {
        // Centre monde de la capsule:
        Vector3 centerWS = transform.TransformPoint(cc.center);
        // Longueur de la partie cylindrique (demi-ligne)
        float halfLine = Mathf.Max(0f, (cc.height * 0.5f - r));
        Vector3 up = Vector3.up * halfLine; // CharacterController est Y-up
        capTop = centerWS + up;
        capBottom = centerWS - up;
    }

    Vector3 GetFeet()
    {
        float r = Mathf.Max(0.0f, cc.radius - 0.01f);
        GetCapsuleEnds(out _, out Vector3 bottom, r);
        return bottom + Vector3.up * 0.02f;
    }

    // Conserver une normale sol fiable pendant les collisions (utile en mouvement sur rampes)
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.moveDirection.y <= 0.1f)
        {
            float ang = Vector3.Angle(hit.normal, Vector3.up);
            if (ang <= maxSlopeAngle + 5f)
            {
                groundNormal = hit.normal;
                groundAngle = ang;
                onWalkableSlope = ang <= maxSlopeAngle;
            }
        }
    }

    // Debug visuel
    void OnDrawGizmosSelected()
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        float r = Mathf.Max(0.0f, cc.radius - 0.01f);
        GetCapsuleEnds(out Vector3 top, out Vector3 bottom, r);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(top, r);
        Gizmos.DrawWireSphere(bottom, r);
        Gizmos.DrawLine(top, bottom);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(GetFeet(), groundNormal.normalized * 0.8f);
    }
}
