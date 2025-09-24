using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UpgradeMenuUIToolkitTabs : MonoBehaviour
{
    [Header("Refs")]
    public UpgradeManager upgrades;   // Assign in Inspector
    public FpsController player;      // Optional (fallback: upgrades.player)
    public bool pauseWhenOpen = true;

    UIDocument doc;
    VisualElement root;

    // Tabs
    Button tabPlayer, tabNexus, tabWaves;
    VisualElement tcPlayer, tcNexus, tcWaves;

    // Player
    Button bP_MaxHp, bP_ArmorType, bP_ArmorLvl, bP_DmgType, bP_WeaponLvl, bHealPlayer;
    Label tP_MaxHpCostLvl, tP_ArmorType, tP_ArmorCostLvl, tP_DmgType, tP_WeaponCostLvl, tHealPlayerCost;

    // Nexus
    Button bN_Level, bN_MaxHp, bN_ArmorType, bN_ArmorLvl, bHealNexus;
    Label tN_LevelCost, tN_LevelVal, tN_MaxHpCostLvl, tN_ArmorType, tN_ArmorCostLvl, tHealNexusCost;

    // Waves
    Toggle tGiants;
    SliderInt sBase, sMax, sRamp;
    Label txtBase, txtMax, txtRamp;

    bool open;

    void Awake() => doc = GetComponent<UIDocument>();

    void Start()
    {
        if (!upgrades) upgrades = FindFirstObjectByType<UpgradeManager>();
        if (!player && upgrades) player = upgrades.player;

        root = doc.rootVisualElement;
        if (root == null) { Debug.LogError("UIDocument root not found"); return; }

        // Rendre les zones cliquables (picking)
        root.pickingMode = PickingMode.Position;
        var frame = root.Q<VisualElement>("Frame");
        if (frame != null) { frame.style.display = DisplayStyle.Flex; frame.pickingMode = PickingMode.Position; }
        var tabs = root.Q<VisualElement>("Tabs");
        if (tabs != null) { tabs.style.display = DisplayStyle.Flex; tabs.pickingMode = PickingMode.Position; }

        // Tabs
        tabPlayer = root.Q<Button>("Tab_Player");
        tabNexus = root.Q<Button>("Tab_Nexus");
        tabWaves = root.Q<Button>("Tab_Waves");
        tcPlayer = root.Q<VisualElement>("TabContent_Player");
        tcNexus = root.Q<VisualElement>("TabContent_Nexus");
        tcWaves = root.Q<VisualElement>("TabContent_Waves");

        if (tabPlayer != null) tabPlayer.clicked += () => SetTab(0);
        if (tabNexus != null) tabNexus.clicked += () => SetTab(1);
        if (tabWaves != null) tabWaves.clicked += () => SetTab(2);

        // Player
        bP_MaxHp = root.Q<Button>("Btn_PlayerMaxHp");
        bP_ArmorType = root.Q<Button>("Btn_PlayerArmorTypeCycler");
        bP_ArmorLvl = root.Q<Button>("Btn_PlayerArmorLvl");
        bP_DmgType = root.Q<Button>("Btn_PlayerDamageTypeCycler");
        bP_WeaponLvl = root.Q<Button>("Btn_PlayerWeaponLvl");
        bHealPlayer = root.Q<Button>("Btn_HealPlayer");

        tP_MaxHpCostLvl = root.Q<Label>("Txt_PlayerMaxHpCostLvl");
        tP_ArmorType = root.Q<Label>("Txt_PlayerArmorType");
        tP_ArmorCostLvl = root.Q<Label>("Txt_PlayerArmorCostLvl");
        tP_DmgType = root.Q<Label>("Txt_PlayerDamageType");
        tP_WeaponCostLvl = root.Q<Label>("Txt_PlayerWeaponCostLvl");
        tHealPlayerCost = root.Q<Label>("Txt_HealPlayerCost");

        if (bP_MaxHp != null) bP_MaxHp.clicked += () => { if (upgrades?.Upgrade_PlayerMaxHp() == true) Refresh(); };
        if (bP_ArmorType != null) bP_ArmorType.clicked += () => { CyclePlayerArmorType(); Refresh(); };
        if (bP_ArmorLvl != null) bP_ArmorLvl.clicked += () => { if (upgrades?.Upgrade_PlayerArmorLevel() == true) Refresh(); };
        if (bP_DmgType != null) bP_DmgType.clicked += () => { CyclePlayerDamageType(); Refresh(); };
        if (bP_WeaponLvl != null) bP_WeaponLvl.clicked += () => { if (upgrades?.Upgrade_PlayerWeaponLevel() == true) Refresh(); };
        if (bHealPlayer != null) bHealPlayer.clicked += () => { if (upgrades?.HealPlayer() == true) Refresh(); };

        // Nexus
        bN_Level = root.Q<Button>("Btn_NexusLevel");
        bN_MaxHp = root.Q<Button>("Btn_NexusMaxHp");
        bN_ArmorType = root.Q<Button>("Btn_NexusArmorTypeCycler");
        bN_ArmorLvl = root.Q<Button>("Btn_NexusArmorLvl");
        bHealNexus = root.Q<Button>("Btn_HealNexus");

        tN_LevelCost = root.Q<Label>("Txt_NexusLevelCostLvl");
        tN_LevelVal = root.Q<Label>("Txt_NexusLevelValue");
        tN_MaxHpCostLvl = root.Q<Label>("Txt_NexusMaxHpCostLvl");
        tN_ArmorType = root.Q<Label>("Txt_NexusArmorType");
        tN_ArmorCostLvl = root.Q<Label>("Txt_NexusArmorCostLvl");
        tHealNexusCost = root.Q<Label>("Txt_HealNexusCost");

        if (bN_Level != null) bN_Level.clicked += () => { if (upgrades?.Upgrade_NexusLevel() == true) Refresh(); };
        if (bN_MaxHp != null) bN_MaxHp.clicked += () => { if (upgrades?.Upgrade_NexusMaxHp() == true) Refresh(); };
        if (bN_ArmorType != null) bN_ArmorType.clicked += () => { CycleNexusArmorType(); Refresh(); };
        if (bN_ArmorLvl != null) bN_ArmorLvl.clicked += () => { if (upgrades?.Upgrade_NexusArmorStats() == true) Refresh(); };
        if (bHealNexus != null) bHealNexus.clicked += () => { if (upgrades?.HealNexus() == true) Refresh(); };

        // Waves
        tGiants = root.Q<Toggle>("Tgl_Giants");
        sBase = root.Q<SliderInt>("Sld_BaseGiantChance");
        sMax = root.Q<SliderInt>("Sld_MaxGiantChance");
        sRamp = root.Q<SliderInt>("Sld_GiantRamp");
        txtBase = root.Q<Label>("Txt_BaseGiantChance");
        txtMax = root.Q<Label>("Txt_MaxGiantChance");
        txtRamp = root.Q<Label>("Txt_GiantRamp");

        if (tGiants != null)
            tGiants.RegisterValueChangedCallback(evt =>
            {
                if (upgrades?.nexusSpawners == null) return;
                foreach (var sp in upgrades.nexusSpawners) if (sp) sp.enableGiantSpawns = evt.newValue;
            });

        if (sBase != null)
            sBase.RegisterValueChangedCallback(evt =>
            {
                if (upgrades?.nexusSpawners != null)
                    foreach (var sp in upgrades.nexusSpawners) if (sp) sp.baseGiantChance = Mathf.Clamp01(evt.newValue / 100f);
                txtBase.SetTextSafe($"Chance de base élites: {evt.newValue}%");
            });

        if (sMax != null)
            sMax.RegisterValueChangedCallback(evt =>
            {
                if (upgrades?.nexusSpawners != null)
                    foreach (var sp in upgrades.nexusSpawners) if (sp) sp.maxGiantChance = Mathf.Clamp01(evt.newValue / 100f);
                txtMax.SetTextSafe($"Chance max élites: {evt.newValue}%");
            });

        if (sRamp != null)
            sRamp.RegisterValueChangedCallback(evt =>
            {
                if (upgrades?.nexusSpawners != null)
                    foreach (var sp in upgrades.nexusSpawners) if (sp) sp.giantChanceRampPerMinute = evt.newValue / 100f;
                txtRamp.SetTextSafe($"Rampe élites (%/min): {evt.newValue}%");
            });

        Hide();
        //SetTab(0);
        Refresh();

        RegisterDebugClicks();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.iKey.wasPressedThisFrame)
            Toggle();
    }

    void Toggle()
    {
        open = !open;
        if (open) Show(); else Hide();

        // Curseur (namespace explicite)
        UnityEngine.Cursor.visible = open;
        UnityEngine.Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;

        if (pauseWhenOpen) Time.timeScale = open ? 0f : 1f;
        if (open) Refresh();
    }

    void Show() { if (root != null) root.style.display = DisplayStyle.Flex; }
    void Hide() { if (root != null) root.style.display = DisplayStyle.None; }

    void RegisterDebugClicks()
    {
        var allButtons = root.Query<Button>().ToList();
        foreach (var btn in allButtons)
        {
            if (btn == null) continue;
            btn.clicked += () =>
            {
                Debug.Log($"[UI] Click sur: {btn.name} (texte='{btn.text}')");
                Refresh();
            };
        }
    }

    void SetTab(int idx)
    {
        SetTabClass(tabPlayer, idx == 0);
        SetTabClass(tabNexus, idx == 1);
        SetTabClass(tabWaves, idx == 2);

        SetTabContent(tcPlayer, idx == 0);
        SetTabContent(tcNexus, idx == 1);
        SetTabContent(tcWaves, idx == 2);
    }

    void SetTabClass(Button b, bool active) { if (b != null) b.EnableInClassList("tab--active", active); }
    void SetTabContent(VisualElement ve, bool active)
    {
        if (ve == null) return;
        ve.EnableInClassList("tabcontent--active", active);
        ve.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        ve.pickingMode = PickingMode.Position;
    }

    void Refresh()
    {
        if (upgrades == null) return;

        var wallet = (player ? player.GetComponent<GoldWallet>() : null) ?? upgrades.player?.GetComponent<GoldWallet>();
        bool Can(int cost) => wallet && wallet.Amount >= cost;

        // Fixed
        tHealPlayerCost.SetTextSafe($"Cost: {upgrades.healPlayerCost}");
        tHealNexusCost.SetTextSafe($"Cost: {upgrades.healNexusCost}");
        if (bHealPlayer != null) bHealPlayer.SetEnabled(Can(upgrades.healPlayerCost));
        if (bHealNexus != null) bHealNexus.SetEnabled(Can(upgrades.healNexusCost));

        // Player
        tP_MaxHpCostLvl.SetTextSafe($"Lv {upgrades.levels.playerMaxHpLvl} | Cost: {upgrades.GetCost_PlayerMaxHp()}");
        if (bP_MaxHp != null) bP_MaxHp.SetEnabled(Can(upgrades.GetCost_PlayerMaxHp()));

        tP_ArmorCostLvl.SetTextSafe($"Lv {upgrades.levels.playerArmorLvl} | Cost: {upgrades.GetCost_PlayerArmor()}");
        if (bP_ArmorLvl != null) bP_ArmorLvl.SetEnabled(Can(upgrades.GetCost_PlayerArmor()));

        tP_WeaponCostLvl.SetTextSafe($"Lv {upgrades.levels.playerWeaponLvl} | Cost: {upgrades.GetCost_PlayerWeapon()}");
        if (bP_WeaponLvl != null) bP_WeaponLvl.SetEnabled(Can(upgrades.GetCost_PlayerWeapon()));

        tP_ArmorType.SetTextSafe(GetPlayerArmorType());
        tP_DmgType.SetTextSafe(GetPlayerDamageType());

        // Nexus
        tN_LevelVal.SetTextSafe($"Level: {upgrades.levels.nexusLevel}");
        tN_LevelCost.SetTextSafe($"Cost: {upgrades.GetCost_NexusLevel()}");
        if (bN_Level != null) bN_Level.SetEnabled(Can(upgrades.GetCost_NexusLevel()));

        tN_MaxHpCostLvl.SetTextSafe($"Lv {upgrades.levels.nexusMaxHpLvl} | Cost: {upgrades.GetCost_NexusMaxHp()}");
        if (bN_MaxHp != null) bN_MaxHp.SetEnabled(Can(upgrades.GetCost_NexusMaxHp()));

        tN_ArmorCostLvl.SetTextSafe($"Lv {upgrades.levels.nexusArmorLvl} | Cost: {upgrades.GetCost_NexusArmor()}");
        if (bN_ArmorLvl != null) bN_ArmorLvl.SetEnabled(Can(upgrades.GetCost_NexusArmor()));

        tN_ArmorType.SetTextSafe(GetNexusArmorType());

        // Waves (affiche les valeurs du 1er spawner)
        if (upgrades?.nexusSpawners != null && upgrades.nexusSpawners.Length > 0)
        {
            var sp = upgrades.nexusSpawners[0];
            if (sp != null)
            {
                if (tGiants != null) tGiants.SetValueWithoutNotify(sp.enableGiantSpawns);
                if (sBase != null) sBase.SetValueWithoutNotify(Mathf.RoundToInt(sp.baseGiantChance * 100f));
                if (sMax != null) sMax.SetValueWithoutNotify(Mathf.RoundToInt(sp.maxGiantChance * 100f));
                if (sRamp != null) sRamp.SetValueWithoutNotify(Mathf.RoundToInt(sp.giantChanceRampPerMinute * 100f));

                txtBase.SetTextSafe($"Chance de base élites: {sBase?.value ?? 0}%");
                txtMax.SetTextSafe($"Chance max élites: {sMax?.value ?? 0}%");
                txtRamp.SetTextSafe($"Rampe élites (%/min): {sRamp?.value ?? 0}%");
            }
        }
    }

    // Helpers
    string GetPlayerArmorType()
    {
        var s = upgrades?.player?.GetComponent<EntityStats>();
        return s ? s.preset.armorType.ToString() : "-";
    }

    string GetNexusArmorType()
    {
        var s = upgrades?.nexus?.GetComponent<EntityStats>();
        return s ? s.preset.armorType.ToString() : "-";
    }

    string GetPlayerDamageType()
    {
        var ec = upgrades?.player?.GetComponent<EntityController>();
        return ec ? ec.DamageType.ToString() : "-";
    }

    void CyclePlayerArmorType()
    {
        var s = upgrades?.player?.GetComponent<EntityStats>();
        if (!s) return;
        var vals = System.Enum.GetValues(typeof(EntityStatsAsset.ArmorType));
        int idx = (System.Array.IndexOf(vals, s.preset.armorType) + 1) % vals.Length;
        upgrades?.Change_PlayerArmorType((EntityStatsAsset.ArmorType)vals.GetValue(idx));
    }

    void CycleNexusArmorType()
    {
        var s = upgrades?.nexus?.GetComponent<EntityStats>();
        if (!s) return;
        var vals = System.Enum.GetValues(typeof(EntityStatsAsset.ArmorType));
        int idx = (System.Array.IndexOf(vals, s.preset.armorType) + 1) % vals.Length;
        upgrades?.Change_NexusArmorType((EntityStatsAsset.ArmorType)vals.GetValue(idx));
    }

    void CyclePlayerDamageType()
    {
        var ec = upgrades?.player?.GetComponent<EntityController>();
        if (!ec) return;
        var vals = System.Enum.GetValues(typeof(EntityStatsAsset.DamageType));
        int idx = (System.Array.IndexOf(vals, ec.DamageType) + 1) % vals.Length;
        upgrades?.Change_PlayerDamageType((EntityStatsAsset.DamageType)vals.GetValue(idx));
    }
}
