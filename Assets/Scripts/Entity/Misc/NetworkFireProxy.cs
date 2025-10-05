using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Pont réseau pour ton HitscanWeapon existant.
/// Version simple :
///  - Le propriétaire déclenche le tir local (FX immédiats).
///  - On notifie le serveur (RPC prêt pour passer full server-authoritative).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkFireProxy : NetworkBehaviour
{
    public HitscanWeapon weapon; // assigne ton composant HitscanWeapon (même GO ou enfant)

    void Awake()
    {
        if (!weapon) weapon = GetComponentInChildren<HitscanWeapon>(true);
    }

    void Update()
    {
        if (!IsOwner || weapon == null) return;

        // L'arme de ton projet expose WantsToFire() + TryFire(out result)
        if (weapon.WantsToFire() && weapon.CanFireNow)
        {
            // 1) Effet/local UX immédiat (Host = effet & dégâts ok car serveur local)
            if (weapon.TryFire(out var local))
            {
                // 2) Informer le serveur (pour les vrais clients)
                //    Pas besoin de tout passer — le serveur refera son raycast (plus sûr).
                RequestServerFireRpc();
            }
        }
    }

    private void RequestServerFireRpc(ServerRpcParams rpcParams = default)
    {
        if (weapon == null) return;

        // NOTE: version de départ "hybride"
        // On rejoue la logique serveur pour appliquer les dégâts côté autorité.
        // Ton HitscanWeapon s’appuie sur Physics.Raycast + owner.DealDamage(target,...)
        // → ça marche côté serveur si l’owner (EntityController) existe aussi serveur.
        weapon.TryFire(out var serverRes);

        // Optionnel : répliquer un tracer à tout le monde (si besoin)
        // On pourrait envoyer positions via ClientRpc, mais ton LineRenderer est déjà local.
    }
}
