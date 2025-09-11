using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FpsController : MonoBehaviour
{
    [Header("Status (optional)")]
    [SerializeField] private TMP_Text statusText; // TextMeshPro ou TextMeshProUGUI

    [Header("FpsAudio")]
    private FpsAudio audioSfx;


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
    public float maxSnapSpeed = 6f;           // vitesse max pour snap au sol
    public float maxSlopeAngle = 50f;         // pente max marchable
    public float slopeSlideGravity = 20f;     // gravité parallèle sur pente trop raide
    public float stepOffsetSnap = 0.10f;      // léger snap pour adoucir petites marches

    [Header("Ceiling Check")]
    public float ceilingProbeRadius = 0.26f;  // check plafond pour uncrouch
    public float ceilingCheckDistance = 0.15f;

    private CharacterController cc;
    private Vector3 velocity;          // composante verticale + éventuel slide
    private float targetSpeed;
    private float currentSpeed;

    // État sol
    private bool isOnGround;           // état principal (robuste) mis à jour après Move
    private Vector3 groundNormal = Vector3.up;
    private float groundAngle;
    private bool onWalkableSlope;

    // Inputs/mouvement
    private Vector3 planarDirWorld;    // direction XZ souhaitée (monde)
    private Vector3 lastMoveXZ;        // pour headbob/FOV et HUD

    // Exposés pour la caméra (headbob/FOV)
    public float CurrentPlanarSpeed01 { get; private set; } // 0..1 par rapport au sprint
    public bool IsGroundedForCamera => isOnGround;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        audioSfx = GetComponent<FpsAudio>();

        if (!enableCrouch) standHeight = cc.height;

        // Fallback pour status si rien n’est assigné
        if (statusText == null)
        {
            var statusGo = GameObject.Find("Status");
            if (statusGo != null)
                statusText = statusGo.GetComponent<TMP_Text>();
        }
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

        // --- Ground probe (pré-move) ---
        // Donne une normale de référence pour projeter l'input sur la pente
        PreMoveGroundProbe();

        // --- Accélération lissée (airControl en l’air) ---
        float accel = isOnGround ? acceleration : acceleration * airControl;
        float speedThisFrame = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);
        currentSpeed = speedThisFrame;

        // Direction monde depuis input
        planarDirWorld = (transform.right * ix + transform.forward * iz);
        if (planarDirWorld.sqrMagnitude > 0f) planarDirWorld.Normalize();

        // Projeter sur le plan de la pente si marchable
        if (isOnGround && onWalkableSlope && planarDirWorld.sqrMagnitude > 0f)
            planarDirWorld = Vector3.ProjectOnPlane(planarDirWorld, groundNormal).normalized;

        // Vecteur horizontal final (avant Move)
        Vector3 planarMove = planarDirWorld * currentSpeed;

        // --- Saut / Gravité / Slide ---
        if (kb.spaceKey.wasPressedThisFrame && isOnGround && !wantCrouch)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isOnGround = false;
            if (audioSfx != null) audioSfx.OnJump(); // <-- NOTIF DU SAUT

        }

        if (isOnGround)
        {
            if (onWalkableSlope)
            {
                if (velocity.y < 0f) velocity.y = groundedStick; // coller au sol
            }
            else
            {
                // pente trop raide → slide parallèle
                Vector3 downslope = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                velocity += downslope * slopeSlideGravity * Time.deltaTime;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // --- MOUVEMENT UNIQUE + lecture des flags ---
        Vector3 motion = (planarMove + velocity) * Time.deltaTime;
        CollisionFlags flags = cc.Move(motion);

        bool below = (flags & CollisionFlags.Below) != 0;
        bool above = (flags & CollisionFlags.Above) != 0;

        // État sol robuste post-move
        isOnGround = below;
        if (isOnGround && velocity.y < 0f) velocity.y = groundedStick;
        if (isOnGround && velocity.y > 0f) velocity.y = 0f; // élimine de petites remontées parasites

        // Si on a touché un plafond, annule la composante +Y
        if (above && velocity.y > 0f) velocity.y = 0f;

        // --- Crouch / Uncrouch sécurisé ---
        if (enableCrouch)
        {
            bool ceilingBlocked = CeilingBlocked();
            float targetH = (wantCrouch || ceilingBlocked) ? crouchHeight : standHeight;
            cc.height = Mathf.Lerp(cc.height, targetH, crouchLerp * Time.deltaTime);
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
        }

        // --- Sorties caméra & HUD ---
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
                $"VelY: {velocity.y:0.00}";
        }
    }

    // --- Probe avant Move : CapsuleCast aligné à la capsule du CC ---
    void PreMoveGroundProbe()
    {
        // borne de capsule du CC
        float r = Mathf.Max(0.0f, cc.radius - 0.01f);
        Vector3 center = cc.bounds.center;
        Vector3 up = Vector3.up * (cc.height * 0.5f - r);

        Vector3 capTop = center + up;
        Vector3 capBottom = center - up;

        float castDist = Mathf.Max(0.01f, groundCheckDistance + stepOffsetSnap);

        bool hit = Physics.CapsuleCast(
            capTop, capBottom, r,
            Vector3.down,
            out RaycastHit hitInfo,
            castDist,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        // Par défaut
        Vector3 newNormal = Vector3.up;
        float newAngle = 0f;
        bool groundContact = false;
        bool walkable = false;

        if (hit)
        {
            newNormal = hitInfo.normal;
            newAngle = Vector3.Angle(newNormal, Vector3.up);
            groundContact = hitInfo.distance <= (groundCheckDistance + 0.02f);
            walkable = newAngle <= maxSlopeAngle;

            // Snap doux si on tombe lentement et qu'on est presque au sol
            float planarSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            bool fallingGently = velocity.y <= 0f && planarSpeed <= maxSnapSpeed;
            if (groundContact && walkable && fallingGently)
            {
                float snapAmt = Mathf.Max(0f, (groundCheckDistance - hitInfo.distance));
                if (snapAmt > 0f)
                {
                    // on applique le snap après Move en composant motion ; ici on peut juste forcer un stick
                    velocity.y = Mathf.Min(velocity.y, groundedStick);
                }
            }
        }

        // On met à jour ces infos pour projeter l'input.
        groundNormal = newNormal;
        groundAngle = newAngle;
        onWalkableSlope = walkable;

        // On ne fige pas isOnGround ici : l'état final sera basé sur CollisionFlags.Below après Move.
    }

    bool CeilingBlocked()
    {
        // Origin au sommet actuel de la capsule (depuis center)
        Vector3 head = cc.bounds.center + Vector3.up * (cc.height * 0.5f - cc.radius);
        Ray rayUp = new Ray(head, Vector3.up);
        return Physics.SphereCast(rayUp, ceilingProbeRadius, ceilingCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
    }

    // Récupère une normale de sol fiable lors des collisions pendant le déplacement (utile sur rampes)
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.moveDirection.y <= 0.1f) // contact plutôt sous/latéral
        {
            float ang = Vector3.Angle(hit.normal, Vector3.up);
            if (ang <= maxSlopeAngle + 5f) // tolérance
            {
                groundNormal = hit.normal;
                groundAngle = ang;
                onWalkableSlope = ang <= maxSlopeAngle;
            }
        }
    }

    // Debug visuel dans la scène
    void OnDrawGizmosSelected()
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        Gizmos.color = Color.yellow;

        float r = Mathf.Max(0.0f, cc.radius - 0.01f);
        Vector3 center = cc.bounds.center;
        Vector3 up = Vector3.up * (cc.height * 0.5f - r);

        Vector3 capTop = center + up;
        Vector3 capBottom = center - up;

        Gizmos.DrawWireSphere(capTop, r);
        Gizmos.DrawWireSphere(capBottom, r);
        Gizmos.DrawLine(capTop, capBottom);

        // normal sol courante
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(center, groundNormal.normalized * 0.8f);
    }
}
