using UnityEngine;

[System.Serializable]
public class UpgradeLevels
{
    [Header("Player")]
    public int playerMaxHpLvl;
    public int playerArmorLvl;
    public int playerWeaponLvl;

    [Header("Nexus")]
    public int nexusLevel = 1;   // commence à 1 par défaut
    public int nexusMaxHpLvl;
    public int nexusArmorLvl;
}
