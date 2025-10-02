using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UpgradeMenuUIToolkit : MonoBehaviour
{
    [Header("Refs")]
    public UpgradeManager upgrades;   // Assigne dans l’inspecteur (ou cherche au Start)
    public FpsController player;      // Pour retrouver GoldWallet si besoin
    public bool pauseWhenOpen = true;

    UIDocument _doc;
    VisualElement _root;

    bool _open;

    // UI elements
    Button bHealPlayer, bHealNexus;
    Label tHealPlayerCost, tHealNexusCost;

    Button bP_MaxHp, bP_ArmorType, bP_ArmorLvl, bP_DmgType, bP_WeaponLvl;
    Label tP_MaxHpCostLvl, tP_ArmorType, tP_ArmorCostLvl, tP_DmgType, tP_WeaponCostLvl;

    Button bN_Level, bN_MaxHp, bN_ArmorType, bN_ArmorLvl;
    Label tN_LevelCost, tN_LevelVal, tN_MaxHpCostLvl, tN_ArmorType, tN_ArmorCostLvl;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    void Start()
    {
        if (!upgrades) upgrades = FindFirstObjectByType<UpgradeManager>();
        if (!player) player = upgrades ? upgrades.player : FindFirstObjectByType<FpsController>();

        _root = _doc.rootVisualElement;
        if (_root == null) { Debug.LogError("UpgradeMenuUIToolkit: UIDocument has no root."); return; }

        // Query elements by name
        bHealPlayer = _root.Q<Button>("Btn_HealPlayer");
        bHealNexus = _root.Q<Button>("Btn_HealNexus");
        tHealPlayerCost = _root.Q<Label>("Txt_HealPlayerCost");
        tHealNexusCost = _root.Q<Label>("Txt_HealNexusCost");

        bP_MaxHp = _root.Q<Button>("Btn_PlayerMaxHp");
        bP_ArmorType = _root.Q<Button>("Btn_PlayerArmorTypeCycler");
        bP_ArmorLvl = _root.Q<Button>("Btn_PlayerArmorLvl");
        bP_DmgType = _root.Q<Button>("Btn_PlayerDamageTypeCycler");
        bP_WeaponLvl = _root.Q<Button>("Btn_PlayerWeaponLvl");
        tP_MaxHpCostLvl = _root.Q<Label>("Txt_PlayerMaxHpCostLvl");
        tP_ArmorType = _root.Q<Label>("Txt_PlayerArmorType");
        tP_ArmorCostLvl = _root.Q<Label>("Txt_PlayerArmorCostLvl");
        tP_DmgType = _root.Q<Label>("Txt_PlayerDamageType");
        tP_WeaponCostLvl = _root.Q<Label>("Txt_PlayerWeaponCostLvl");

        bN_Level = _root.Q<Button>("Btn_NexusLevel");
        bN_MaxHp = _root.Q<Button>("Btn_NexusMaxHp");
        bN_ArmorType = _root.Q<Button>("Btn_NexusArmorTypeCycler");
        bN_ArmorLvl = _root.Q<Button>("Btn_NexusArmorLvl");
        tN_LevelCost = _root.Q<Label>("Txt_NexusLevelCostLvl");
        tN_LevelVal = _root.Q<Label>("Txt_NexusLevelValue");
        tN_MaxHpCostLvl = _root.Q<Label>("Txt_NexusMaxHpCostLvl");
        tN_ArmorType = _root.Q<Label>("Txt_NexusArmorType");
        tN_ArmorCostLvl = _root.Q<Label>("Txt_NexusArmorCostLvl");

        // Bind actions
        if (bHealPlayer != null) bHealPlayer.clicked += () => { if (upgrades.HealPlayer()) Refresh(); };
        if (bHealNexus != null) bHealNexus.clicked += () => { if (upgrades.HealNexus()) Refresh(); };

        if (bP_MaxHp != null) bP_MaxHp.clicked += () => { if (upgrades.Upgrade_PlayerMaxHp()) Refresh(); };
        if (bP_ArmorType != null) bP_ArmorType.clicked += () => { CyclePlayerArmorType(); Refresh(); };
        if (bP_ArmorLvl != null) bP_ArmorLvl.clicked += () => { if (upgrades.Upgrade_PlayerArmorLevel()) Refresh(); };
        if (bP_DmgType != null) bP_DmgType.clicked += () => { CyclePlayerDamageType(); Refresh(); };
        if (bP_WeaponLvl != null) bP_WeaponLvl.clicked += () => { if (upgrades.Upgrade_PlayerWeaponLevel()) Refresh(); };

        if (bN_Level != null) bN_Level.clicked += () => { if (upgrades.Upgrade_NexusLevel()) Refresh(); };
        if (bN_MaxHp != null) bN_MaxHp.clicked += () => { if (upgrades.Upgrade_NexusMaxHp()) Refresh(); };
        if (bN_ArmorType != null) bN_ArmorType.clicked += () => { CycleNexusArmorType(); Refresh(); };
        if (bN_ArmorLvl != null) bN_ArmorLvl.clicked += () => { if (upgrades.Upgrade_NexusArmorStats()) Refresh(); };

        Hide();
        Refresh();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.iKey.wasPressedThisFrame)
            Toggle();
    }

    void Toggle()
    {
        _open = !_open;
        if (_open) Show(); else Hide();
        if (pauseWhenOpen) Time.timeScale = _open ? 0f : 1f;
        Refresh();
    }

    void Show() => _root.style.display = DisplayStyle.Flex;
    void Hide() => _root.style.display = DisplayStyle.None;

    void Refresh()
    {
        if (upgrades == null) return;

        // Fixed costs
        tHealPlayerCost.text = $"Cost: {upgrades.healPlayerCost}";
        tHealNexusCost.text = $"Cost: {upgrades.healNexusCost}";

        // Enable/disable by wallet
        var wallet = player ? player.GetComponent<GoldWallet>() : null;
        bool Can(int cost) => wallet && wallet.Amount >= cost;

        bHealPlayer.SetEnabled(Can(upgrades.healPlayerCost));
        bHealNexus.SetEnabled(Can(upgrades.healNexusCost));

        // Player
        tP_MaxHpCostLvl.text = $"Lv {upgrades.levels.playerMaxHpLvl} | Cost: {upgrades.GetCost_PlayerMaxHp()}";
         bP_MaxHp.SetEnabled(Can(upgrades.GetCost_PlayerMaxHp()));

        tP_ArmorCostLvl.text = $"Lv {upgrades.levels.playerArmorLvl} | Cost: {upgrades.GetCost_PlayerArmor()}";
        bP_ArmorLvl.SetEnabled(Can(upgrades.GetCost_PlayerArmor()));

        tP_WeaponCostLvl.text = $"Lv {upgrades.levels.playerWeaponLvl} | Cost: {upgrades.GetCost_PlayerWeapon()}";
        bP_WeaponLvl.SetEnabled(Can(upgrades.GetCost_PlayerWeapon()));

        tP_ArmorType.text = GetPlayerArmorType();
        tP_DmgType.text = GetPlayerDamageType();

        // Nexus
        tN_LevelVal.text = $"Level: {upgrades.levels.nexusLevel}";
        tN_LevelCost.text = $"Cost: {upgrades.GetCost_NexusLevel()}";
        bN_Level.SetEnabled(Can(upgrades.GetCost_NexusLevel()));

        tN_MaxHpCostLvl.text = $"Lv {upgrades.levels.nexusMaxHpLvl} | Cost: {upgrades.GetCost_NexusMaxHp()}";
        bN_MaxHp.SetEnabled(Can(upgrades.GetCost_NexusMaxHp()));

        tN_ArmorCostLvl.text = $"Lv {upgrades.levels.nexusArmorLvl} | Cost: {upgrades.GetCost_NexusArmor()}";
        bN_ArmorLvl.SetEnabled(Can(upgrades.GetCost_NexusArmor()));

        tN_ArmorType.text = GetNexusArmorType();
    }

    // —— Helpers types (lis l’état réel de tes objets) ——
    string GetPlayerArmorType()
    {
        var s = upgrades ? upgrades.player?.GetComponent<EntityStats>() : null;
        return s ? s.preset.armorType.ToString() : "-";
    }
    string GetNexusArmorType()
    {
        var s = upgrades ? upgrades.nexus?.GetComponent<EntityStats>() : null;
        return s ? s.preset.armorType.ToString() : "-";
    }
    string GetPlayerDamageType()
    {
        var ec = upgrades ? upgrades.player?.GetComponent<EntityController>() : null;
        return ec ? ec.DamageType.ToString() : "-";
    }

    void CyclePlayerArmorType()
    {
        var s = upgrades ? upgrades.player?.GetComponent<EntityStats>() : null;
        if (s == null) return;
        var values = System.Enum.GetValues(typeof(EntityStatsAsset.ArmorType));
        int idx = (System.Array.IndexOf(values, s.preset.armorType) + 1) % values.Length;
        upgrades.Change_PlayerArmorType((EntityStatsAsset.ArmorType)values.GetValue(idx));
    }

    void CycleNexusArmorType()
    {
        var s = upgrades ? upgrades.nexus?.GetComponent<EntityStats>() : null;
        if (s == null) return;
        var values = System.Enum.GetValues(typeof(EntityStatsAsset.ArmorType));
        int idx = (System.Array.IndexOf(values, s.preset.armorType) + 1) % values.Length;
        upgrades.Change_NexusArmorType((EntityStatsAsset.ArmorType)values.GetValue(idx));
    }

    void CyclePlayerDamageType()
    {
        var ec = upgrades ? upgrades.player?.GetComponent<EntityController>() : null;
        if (ec == null) return;
        var values = System.Enum.GetValues(typeof(EntityStatsAsset.DamageType));
        int idx = (System.Array.IndexOf(values, ec.DamageType) + 1) % values.Length;
        upgrades.Change_PlayerDamageType((EntityStatsAsset.DamageType)values.GetValue(idx));
    }
}
