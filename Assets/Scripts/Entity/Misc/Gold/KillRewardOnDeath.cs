using UnityEngine;

[DisallowMultipleComponent]
public class KillRewardOnDeath : MonoBehaviour
{
    [Min(0)] public int goldReward = 0; // la prime offerte en mourant (ex: 25)

    private EntityController entity;

    void Awake()
    {
        entity = GetComponent<EntityController>();
        if (entity == null)
        {
            Debug.LogError("[KillRewardOnDeath] EntityController requis.");
            enabled = false;
            return;
        }

        // S’abonner à l’évènement de mort
        entity.OnKilledBy.AddListener(HandleKilledBy);
    }

    private void OnDestroy()
    {
        if (entity != null)
            entity.OnKilledBy.RemoveListener(HandleKilledBy);
    }

    private void HandleKilledBy(EntityController killer)
    {
        if (goldReward <= 0 || killer == null) return;

        var wallet = killer.GetComponent<GoldWallet>();
        if (wallet != null)
        {
            wallet.Add(goldReward);
            Debug.Log($"[GOLD] {killer.name} +{goldReward} (kill {entity.name}) → {wallet.Amount}");
        }
    }
}
