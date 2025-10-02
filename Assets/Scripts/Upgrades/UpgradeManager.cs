using UnityEngine;

[DisallowMultipleComponent]
public class UpgradeManager : MonoBehaviour
{
    [Header("Refs (assign in Inspector)")]
    public FpsController player;            // joueur (doit avoir EntityStats + Health + GoldWallet)
    public NexusController nexus;           // Nexus (EntityStats + Health)
    public PortalSpawner[] nexusSpawners;   // spawners liés au Nexus (impact niveau Nexus)

    GoldWallet wallet => player ? player.GetComponent<GoldWallet>() : null;


    [Header("Cost: Fixed")]
    public int healPlayerCost = 50;
    public int healNexusCost = 80;

    [Header("Cost: Curves (price grows with level)")]
    public PriceCurve playerMaxHpCost = new() { baseCost = 80, stepCost = 40, stepMult = 1.00f };
    public PriceCurve playerArmorCost = new() { baseCost = 70, stepCost = 35, stepMult = 1.00f };
    public PriceCurve playerWeaponCost = new() { baseCost = 90, stepCost = 45, stepMult = 1.00f };

    public PriceCurve nexusLevelCost = new() { baseCost = 120, stepCost = 75, stepMult = 1.05f };
    public PriceCurve nexusMaxHpCost = new() { baseCost = 100, stepCost = 50, stepMult = 1.00f };
    public PriceCurve nexusArmorCost = new() { baseCost = 90, stepCost = 45, stepMult = 1.00f };

    [Header("Caps")]
    public int playerMaxHpCapLv = 10;
    public int playerArmorCapLv = 10;
    public int playerWeaponCapLv = 10;
    public int nexusCapLevel = 10;
    public int nexusMaxHpCapLv = 15;
    public int nexusArmorCapLv = 15;

    [Header("Effects per level")]
    [Tooltip("+% PV max joueur par niveau (multiplie la valeur MaxHealth, via EntityStats si dispo ; sinon calcule une nouvelle Max)")]
    public float playerMaxHpPercentPerLv = 0.06f; // +6%

    [Tooltip("+ armure joueur par niveau")]
    public float playerArmorFlatPerLv = 5f;

    [Tooltip("+% dégâts d’arme / -% cooldown / etc. (à relier à tes stats d’arme)")]
    public float playerWeaponDamagePercentPerLv = 0.08f; // +8%

    [Tooltip("+% PV max Nexus par niveau")]
    public float nexusMaxHpPercentPerLv = 0.08f; // +8%

    [Tooltip("+ armure Nexus par niveau")]
    public float nexusArmorFlatPerLv = 6f;

    [Header("Nexus level → Spawner effects")]
    public bool enableGiantSpawnsOnLevelUp = true;
    public float baseGiantChancePerLevel = 0.015f;
    public float maxGiantChancePerLevel = 0.02f;
    public float rampPerMinutePerLevel = 0.01f;

    [Header("State")]
    public UpgradeLevels levels = new();

    // Helpers d’accès
    EntityStats PlayerStats => player ? player.GetComponent<EntityStats>() : null;
    EntityStats NexusStats => nexus ? nexus.GetComponent<EntityStats>() : null;
    Health PlayerHealth => player ? player.GetComponent<Health>() : null;
    Health NexusHealth => nexus ? nexus.GetComponent<Health>() : null;

    // === Fixed price actions ===
    public bool HealPlayer()
    {
        if (!wallet || !PlayerHealth) return false;
        if (wallet.Amount < healPlayerCost) return false;

        if (!wallet.TrySpend(healPlayerCost)) return false;
        float missing = Mathf.Max(0f, PlayerHealth.Max - PlayerHealth.Current);
        if (missing > 0f) PlayerHealth.Heal(missing); // full heal en un appel
        return true;
    }

    public bool HealNexus()
    {
        if (!wallet || !NexusHealth) return false;
        if (wallet.Amount < healNexusCost) return false;

        if (!wallet.TrySpend(healNexusCost)) return false;
        float missing = Mathf.Max(0f, NexusHealth.Max - NexusHealth.Current);
        if (missing > 0f) NexusHealth.Heal(missing);
        return true;
    }

    // === Player progressive upgrades ===
    public int GetCost_PlayerMaxHp() => playerMaxHpCost.GetCost(levels.playerMaxHpLvl);
    public int GetCost_PlayerArmor() => playerArmorCost.GetCost(levels.playerArmorLvl);
    public int GetCost_PlayerWeapon() => playerWeaponCost.GetCost(levels.playerWeaponLvl);

    public bool Upgrade_PlayerMaxHp()
    {
        if (levels.playerMaxHpLvl >= playerMaxHpCapLv) return false;
        int cost = GetCost_PlayerMaxHp();
        if (!wallet || wallet.Amount < cost) return false;
        if (!wallet.TrySpend(cost)) return false;

        levels.playerMaxHpLvl++;

        // Applique le bonus de PV (deux stratégies) :
        // (A) Si EntityStats a un multiplicateur PV → multiplie puis SetMax(MaxHealth, refill)
        var s = PlayerStats;
        var h = PlayerHealth;
        if (s && h)
        {
            // On essaie de trouver "healthMultiplier" si exposé :
            var fi = typeof(EntityStats).GetField("healthMultiplier");
            if (fi != null)
            {
                float current = (float)fi.GetValue(s);
                fi.SetValue(s, current * (1f + playerMaxHpPercentPerLv));
                h.SetMax(s.MaxHealth, refill: true);
                return true;
            }
        }

        // (B) Sinon, calcule une nouvelle Max sur la base actuelle :
        if (h)
        {
            float newMax = h.Max * (1f + playerMaxHpPercentPerLv);
            h.SetMax(newMax, refill: true); // ta classe Health notifie l’UI via OnHealthChanged. 
            return true;
        }
        return false;
    }

    public bool Change_PlayerArmorType(EntityStatsAsset.ArmorType newType)
    {
        var s = PlayerStats;
        if (s == null) return false;
        s.preset.armorType = newType; // si ton preset est partagé, préfère un décorateur runtime ; sinon duplique le preset
        return true;
    }

    public bool Upgrade_PlayerArmorLevel()
    {
        if (levels.playerArmorLvl >= playerArmorCapLv) return false;
        int cost = GetCost_PlayerArmor();
        if (!wallet || wallet.Amount < cost) return false;
        if (!wallet.TrySpend(cost)) return false;

        levels.playerArmorLvl++;

        var s = PlayerStats;
        var h = PlayerHealth;
        if (s != null)
        {
            // essayer champ runtime "armorBonus" sinon fallback via preset
            var fi = typeof(EntityStats).GetField("armorBonus");
            if (fi != null)
            {
                float current = (float)fi.GetValue(s);
                fi.SetValue(s, current + playerArmorFlatPerLv);
                if (h) h.SetMax(s.MaxHealth, refill: false); // HP inchangée, mais notifie l’UI si besoin
                return true;
            }
            // fallback : si preset a "armor"
            s.preset.armor += playerArmorFlatPerLv;
            if (h) h.SetMax(s.MaxHealth, refill: false);
            return true;
        }
        return false;
    }

    public bool Change_PlayerDamageType(EntityStatsAsset.DamageType newType)
    {
        // Si tes tirs lisent owner.damageType (ex: HitscanWeapon.owner / EntityController), change sur l’EntityController du joueur
        var ec = player ? player.GetComponent<EntityController>() : null;
        var st = player ? player.GetComponent<EntityStats>() : null;

        if (!ec) return false;
        if (!st) return false;
        st.preset.damageType = newType;
        return true;
    }

    public bool Upgrade_PlayerWeaponLevel()
    {
        Debug.Log("Upgrade_PlayerWeaponLevel");
        if (levels.playerWeaponLvl >= playerWeaponCapLv) return false;
        Debug.Log("Upgrade_PlayerWeaponLevel - cost");
        int cost = GetCost_PlayerWeapon();

        if (!wallet || wallet.Amount < cost) return false;
        Debug.Log("Upgrade_PlayerWeaponLevel - tryspend");
        if (!wallet.TrySpend(cost)) return false;
        Debug.Log("Upgrade_PlayerWeaponLevel - success");
        levels.playerWeaponLvl++;

        // essaie d’augmenter dégâts via EntityStats.damageMultiplier (ou coolDownMultiplier si tu préfères)
        var s = PlayerStats;
        Debug.Log("Upgrade_PlayerWeaponLevel - stats");
        if (s != null)
        {
            Debug.Log("Upgrade_PlayerWeaponLevel - stats not null");

            var fi = typeof(EntityStats).GetField("damageMultiplier");
            if (fi != null)
            {
                Debug.Log("Upgrade_PlayerWeaponLevel - found field");
                float current = (float)fi.GetValue(s);
                fi.SetValue(s, current * (1f + playerWeaponDamagePercentPerLv));
                Debug.Log("Upgrade_PlayerWeaponLevel - upgraded");
                return true;
            }
            // fallback : si preset a "damage"
            s.preset.damage *= (1f + playerWeaponDamagePercentPerLv);
            Debug.Log("Upgrade_PlayerWeaponLevel - upgraded via preset");
            return true;
        }
        return false;
    }

    // === Nexus progressive upgrades ===
    public int GetCost_NexusLevel() => nexusLevelCost.GetCost(Mathf.Max(0, levels.nexusLevel - 1));
    public int GetCost_NexusMaxHp() => nexusMaxHpCost.GetCost(levels.nexusMaxHpLvl);
    public int GetCost_NexusArmor() => nexusArmorCost.GetCost(levels.nexusArmorLvl);

    public bool Upgrade_NexusLevel()
    {
        if (levels.nexusLevel >= nexusCapLevel) return false;
        int cost = GetCost_NexusLevel();
        if (!wallet || wallet.Amount < cost) return false;
        if (!wallet.TrySpend(cost)) return false;

        levels.nexusLevel++;


        if (player != null)
        {
            if (player.NexusController != null)
            {
                player.NexusController.NexusLevel += 1;
                Debug.Log("Nexus level increased to " + player.NexusController.NexusLevel);
                player.NexusController?.Team?.updateNexusLevel(player.NexusController.NexusLevel);
            }
        }

        // Impacte les spawners (vagues envoyées)
        foreach (var sp in nexusSpawners)
        {
            if (!sp) continue;
            if (enableGiantSpawnsOnLevelUp) sp.enableGiantSpawns = true;
            sp.baseGiantChance = Mathf.Clamp01(sp.baseGiantChance + baseGiantChancePerLevel);
            sp.maxGiantChance = Mathf.Clamp01(sp.maxGiantChance + maxGiantChancePerLevel);
            sp.giantChanceRampPerMinute += rampPerMinutePerLevel;
        }
        return true;
    }

    public bool Upgrade_NexusMaxHp()
    {
        if (levels.nexusMaxHpLvl >= nexusMaxHpCapLv) return false;
        int cost = GetCost_NexusMaxHp();
        if (!wallet || wallet.Amount < cost) return false;
        if (!wallet.TrySpend(cost)) return false;

        levels.nexusMaxHpLvl++;

        var s = NexusStats;
        var h = NexusHealth;
        if (s && h)
        {
            var fi = typeof(EntityStats).GetField("healthMultiplier");
            if (fi != null)
            {
                float current = (float)fi.GetValue(s);
                fi.SetValue(s, current * (1f + nexusMaxHpPercentPerLv));
                h.SetMax(s.MaxHealth, refill: true); // On remplit pour ressenti progression.
                return true;
            }
        }
        if (NexusHealth)
        {
            float newMax = NexusHealth.Max * (1f + nexusMaxHpPercentPerLv);
            NexusHealth.SetMax(newMax, refill: true); // notifie l’UI via OnHealthChanged. 
            return true;
        }
        return false;
    }

    public bool Change_NexusArmorType(EntityStatsAsset.ArmorType newType)
    {
        var s = NexusStats;
        if (s == null) return false;
        s.preset.armorType = newType; // idem remarque : si preset partagé, mieux vaut un champ runtime décorateur
        return true;
    }

    public bool Upgrade_NexusArmorStats()
    {
        if (levels.nexusArmorLvl >= nexusArmorCapLv) return false;
        int cost = GetCost_NexusArmor();
        if (!wallet || wallet.Amount < cost) return false;
        if (!wallet.TrySpend(cost)) return false;

        levels.nexusArmorLvl++;

        var s = NexusStats;
        var h = NexusHealth;
        if (s != null)
        {
            var fi = typeof(EntityStats).GetField("armorBonus");
            if (fi != null)
            {
                float current = (float)fi.GetValue(s);
                fi.SetValue(s, current + nexusArmorFlatPerLv);
                if (h) h.SetMax(s.MaxHealth, refill: false);
                return true;
            }
            s.preset.armor += nexusArmorFlatPerLv;
            if (h) h.SetMax(s.MaxHealth, refill: false);
            return true;
        }
        return false;
    }
}
