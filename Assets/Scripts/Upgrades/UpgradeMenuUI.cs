using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UpgradeMenuUI : MonoBehaviour
{
    [Header("Input")]
    public Key toggleKey = Key.I;

    [Header("Refs")]
    public UpgradeManager upgrades;
    
    GoldWallet wallet => upgrades ? upgrades.player.GetComponent<GoldWallet>() : null;


    [Header("UI Root")]
    public GameObject panel;
    public CanvasGroup group;

    [Header("Fixed price")]
    public Button btnHealPlayer;
    public TMP_Text txtHealPlayerCost;
    public Button btnHealNexus;
    public TMP_Text txtHealNexusCost;

    [Header("Player – progressive")]
    public Button btnPlayerMaxHp;
    public TMP_Text txtPlayerMaxHpCostLvl;
    public Button btnPlayerArmorTypeCycler;
    public TMP_Text txtPlayerArmorType;
    public Button btnPlayerArmorLvl;
    public TMP_Text txtPlayerArmorCostLvl;
    public Button btnPlayerDamageTypeCycler;
    public TMP_Text txtPlayerDamageType;
    public Button btnPlayerWeaponLvl;
    public TMP_Text txtPlayerWeaponCostLvl;

    [Header("Nexus – progressive")]
    public Button btnNexusLevel;
    public TMP_Text txtNexusLevelCostLvl;
    public TMP_Text txtNexusLevelValue;
    public Button btnNexusMaxHp;
    public TMP_Text txtNexusMaxHpCostLvl;
    public Button btnNexusArmorTypeCycler;
    public TMP_Text txtNexusArmorType;
    public Button btnNexusArmorLvl;
    public TMP_Text txtNexusArmorCostLvl;

    bool isOpen;

    // Cyclers (exemples)
    EntityStatsAsset.ArmorType[] armorTypes = (EntityStatsAsset.ArmorType[])System.Enum.GetValues(typeof(EntityStatsAsset.ArmorType));
    int playerArmorIdx;
    int nexusArmorIdx;

    EntityStatsAsset.DamageType[] dmgTypes = (EntityStatsAsset.DamageType[])System.Enum.GetValues(typeof(EntityStatsAsset.DamageType));
    int playerDmgIdx;

    void Start()
    {
        if (panel) panel.SetActive(false);
        if (group) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }

        // Bind fixed
        if (btnHealPlayer) btnHealPlayer.onClick.AddListener(() => { if (upgrades.HealPlayer()) Refresh(); });
        if (btnHealNexus) btnHealNexus.onClick.AddListener(() => { if (upgrades.HealNexus()) Refresh(); });

        // Player prog
        if (btnPlayerMaxHp) btnPlayerMaxHp.onClick.AddListener(() => { if (upgrades.Upgrade_PlayerMaxHp()) Refresh(); });
        if (btnPlayerArmorTypeCycler) btnPlayerArmorTypeCycler.onClick.AddListener(CyclePlayerArmorType);
        if (btnPlayerArmorLvl) btnPlayerArmorLvl.onClick.AddListener(() => { if (upgrades.Upgrade_PlayerArmorLevel()) Refresh(); });
        if (btnPlayerDamageTypeCycler) btnPlayerDamageTypeCycler.onClick.AddListener(CyclePlayerDamageType);
        if (btnPlayerWeaponLvl) btnPlayerWeaponLvl.onClick.AddListener(() => { if (upgrades.Upgrade_PlayerWeaponLevel()) Refresh(); });

        // Nexus prog
        if (btnNexusLevel) btnNexusLevel.onClick.AddListener(() => { if (upgrades.Upgrade_NexusLevel()) Refresh(); });
        if (btnNexusMaxHp) btnNexusMaxHp.onClick.AddListener(() => { if (upgrades.Upgrade_NexusMaxHp()) Refresh(); });
        if (btnNexusArmorTypeCycler) btnNexusArmorTypeCycler.onClick.AddListener(CycleNexusArmorType);
        if (btnNexusArmorLvl) btnNexusArmorLvl.onClick.AddListener(() => { if (upgrades.Upgrade_NexusArmorStats()) Refresh(); });

        Refresh();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            Toggle();
    }

    void Toggle()
    {
        isOpen = !isOpen;
        if (panel) panel.SetActive(isOpen);
        if (group)
        {
            group.alpha = isOpen ? 1f : 0f;
            group.interactable = isOpen;
            group.blocksRaycasts = isOpen;
        }
        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = isOpen ? 0f : 1f;
        if (isOpen) Refresh();
    }

    void Refresh()
    {
        if (!upgrades) return;

        // Fixed
        if (txtHealPlayerCost) txtHealPlayerCost.text = $"Cost: {upgrades.healPlayerCost}";
        if (txtHealNexusCost) txtHealNexusCost.text = $"Cost: {upgrades.healNexusCost}";
        if (btnHealPlayer) btnHealPlayer.interactable = wallet && wallet.Amount >= upgrades.healPlayerCost;
        if (btnHealNexus) btnHealNexus.interactable = wallet && wallet.Amount >= upgrades.healNexusCost;

        // Player
        if (txtPlayerMaxHpCostLvl) txtPlayerMaxHpCostLvl.text = $"Lv {upgrades.levels.playerMaxHpLvl} | Cost: {upgrades.GetCost_PlayerMaxHp()}";
        if (btnPlayerMaxHp) btnPlayerMaxHp.interactable = wallet && wallet.Amount >= upgrades.GetCost_PlayerMaxHp();

        if (txtPlayerArmorCostLvl) txtPlayerArmorCostLvl.text = $"Lv {upgrades.levels.playerArmorLvl} | Cost: {upgrades.GetCost_PlayerArmor()}";
        if (btnPlayerArmorLvl) btnPlayerArmorLvl.interactable = wallet && wallet.Amount >= upgrades.GetCost_PlayerArmor();

        if (txtPlayerWeaponCostLvl) txtPlayerWeaponCostLvl.text = $"Lv {upgrades.levels.playerWeaponLvl} | Cost: {upgrades.GetCost_PlayerWeapon()}";
        if (btnPlayerWeaponLvl) btnPlayerWeaponLvl.interactable = wallet && wallet.Amount >= upgrades.GetCost_PlayerWeapon();

        // Nexus
        if (txtNexusLevelValue) txtNexusLevelValue.text = $"Level: {upgrades.levels.nexusLevel}";
        if (txtNexusLevelCostLvl) txtNexusLevelCostLvl.text = $"Cost: {upgrades.GetCost_NexusLevel()}";
        if (btnNexusLevel) btnNexusLevel.interactable = wallet && wallet.Amount >= upgrades.GetCost_NexusLevel();

        if (txtNexusMaxHpCostLvl) txtNexusMaxHpCostLvl.text = $"Lv {upgrades.levels.nexusMaxHpLvl} | Cost: {upgrades.GetCost_NexusMaxHp()}";
        if (btnNexusMaxHp) btnNexusMaxHp.interactable = wallet && wallet.Amount >= upgrades.GetCost_NexusMaxHp();

        if (txtNexusArmorCostLvl) txtNexusArmorCostLvl.text = $"Lv {upgrades.levels.nexusArmorLvl} | Cost: {upgrades.GetCost_NexusArmor()}";
        if (btnNexusArmorLvl) btnNexusArmorLvl.interactable = wallet && wallet.Amount >= upgrades.GetCost_NexusArmor();

        // Affichage types (labels)
        if (txtPlayerArmorType) txtPlayerArmorType.text = armorTypes[playerArmorIdx].ToString();
        if (txtPlayerDamageType) txtPlayerDamageType.text = dmgTypes[playerDmgIdx].ToString();
        if (txtNexusArmorType) txtNexusArmorType.text = armorTypes[nexusArmorIdx].ToString();
    }

    void CyclePlayerArmorType()
    {
        playerArmorIdx = (playerArmorIdx + 1) % armorTypes.Length;
        var ok = upgrades.Change_PlayerArmorType(armorTypes[playerArmorIdx]);
        if (!ok) playerArmorIdx = (playerArmorIdx - 1 + armorTypes.Length) % armorTypes.Length;
        Refresh();
    }

    void CyclePlayerDamageType()
    {
        playerDmgIdx = (playerDmgIdx + 1) % dmgTypes.Length;
        var ok = upgrades.Change_PlayerDamageType(dmgTypes[playerDmgIdx]);
        if (!ok) playerDmgIdx = (playerDmgIdx - 1 + dmgTypes.Length) % dmgTypes.Length;
        Refresh();
    }

    void CycleNexusArmorType()
    {
        nexusArmorIdx = (nexusArmorIdx + 1) % armorTypes.Length;
        var ok = upgrades.Change_NexusArmorType(armorTypes[nexusArmorIdx]);
        if (!ok) nexusArmorIdx = (nexusArmorIdx - 1 + armorTypes.Length) % armorTypes.Length;
        Refresh();
    }
}
