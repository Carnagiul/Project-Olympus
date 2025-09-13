// AimHoverDetector.cs
using UnityEngine;
using System;

[DisallowMultipleComponent]
public class AimHoverDetector : MonoBehaviour
{
    [Header("Refs (auto si vide)")]
    public Camera cam;            // si null, on tente FpsController.playerCamera
    public FpsController player;

    [Header("Settings")]
    public float maxCheckDistance = 100f;
    public LayerMask mask = ~0;   // filtre physique (exclure Player si besoin)
    public string enemyTag = "Monsters";

    public event Action<EntityController, RaycastHit> HoverEnemy;   // quand on vise un ennemi
    public event Action LostEnemy;                                  // quand on n'en vise plus

    private EntityController lastEnemy;

    void Awake()
    {
        if (!player) player = GetComponent<FpsController>();
        if (!cam && player) cam = player.playerCamera;
        if (mask == 0) mask = ~0;
    }

    void Update()
    {
        if (!cam) return;

        var origin = cam.transform.position;
        var dir = cam.transform.forward;

        if (Physics.Raycast(origin, dir, out var hit, maxCheckDistance, mask, QueryTriggerInteraction.Ignore))
        {
            var ec = hit.collider.GetComponentInParent<EntityController>();
            bool isEnemy = ec != null && ec.CompareTag(enemyTag);

            if (isEnemy)
            {
                if (ec != lastEnemy)
                {
                    lastEnemy = ec;
                    HoverEnemy?.Invoke(ec, hit);
                }
                return;
            }
        }

        // Si on arrive ici, pas d'ennemi visé
        if (lastEnemy != null)
        {
            lastEnemy = null;
            LostEnemy?.Invoke();
        }
    }
}
