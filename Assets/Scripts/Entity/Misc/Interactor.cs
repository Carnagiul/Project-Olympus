using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Interactor : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;

    [Header("Settings")]
    public float interactRange = 3f;
    public LayerMask interactLayers = ~0; // mets un LayerMask dédié si besoin

    private FpsController controller;

    void Awake()
    {
        controller = GetComponent<FpsController>();
        if (playerCamera == null && controller != null)
            playerCamera = controller.playerCamera;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || playerCamera == null) return;

        if (kb.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, interactRange, interactLayers, QueryTriggerInteraction.Ignore))
        {
            // Cherche un IInteractable sur l'objet touché
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(controller);
            }
        }
    }
}
