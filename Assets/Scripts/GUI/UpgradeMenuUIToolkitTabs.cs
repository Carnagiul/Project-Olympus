using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
// IMPORTANT : ajoute le namespace où est StoreActionButton
using Assets.Scripts.UI.Store;

[RequireComponent(typeof(UIDocument))]
public class UpgradeMenuUIToolkitTabs : MonoBehaviour
{
    [Header("Refs")]
    public UpgradeManager upgrades;   // Assign in Inspector
    public FpsController player;      // Optional (fallback: upgrades.player)
    public bool pauseWhenOpen = true;

    UIDocument doc;
    VisualElement root;

    // Tabs (restent en <ui:Button>)
    Button tabPlayer, tabNexus, tabWaves;
    VisualElement tcPlayer, tcNexus, tcWaves;

    // Player (custom controls)
    StoreActionButton bP_MaxHp, bP_ArmorType, bP_ArmorLvl, bP_DmgType, bP_WeaponLvl, bHealPlayer;

    // Nexus (custom controls)
    StoreActionButton bN_Level, bN_MaxHp, bN_ArmorType, bN_ArmorLvl, bHealNexus;

    // Waves (inchangé)
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

        // Player actions
        bP_MaxHp = root.Q<StoreActionButton>("Btn_PlayerMaxHp");
        bP_ArmorType = root.Q<StoreActionButton>("Btn_PlayerArmorTypeCycler");
        bP_ArmorLvl = root.Q<StoreActionButton>("Btn_PlayerArmorLvl");
        bP_DmgType = root.Q<StoreActionButton>("Btn_PlayerDamageTypeCycler");
        bP_WeaponLvl = root.Q<StoreActionButton>("Btn_PlayerWeaponLvl");
        bHealPlayer = root.Q<StoreActionButton>("Btn_HealPlayer");

        // Nexus actions
        bN_Level = root.Q<StoreActionButton>("Btn_NexusLevel");
        bN_MaxHp = root.Q<StoreActionButton>("Btn_NexusMaxHp");
        bN_ArmorType = root.Q<StoreActionButton>("Btn_NexusArmorTypeCycler");
        bN_ArmorLvl = root.Q<StoreActionButton>("Btn_NexusArmorLvl");
        bHealNexus = root.Q<StoreActionButton>("Btn_HealNexus");

        // Clicks (vérifie canBuy avant d'appeler l'upgrade)
        if (bP_MaxHp != null) bP_MaxHp.RegisterCallback<ClickEvent>(_ => { if (bP_MaxHp.canBuy && upgrades?.Upgrade_PlayerMaxHp() == true) Refresh(); });
        if (bP_ArmorType != null) bP_ArmorType.RegisterCallback<ClickEvent>(_ => { CyclePlayerArmorType(); Refresh(); });
        if (bP_ArmorLvl != null) bP_ArmorLvl.RegisterCallback<ClickEvent>(_ => { if (bP_ArmorLvl.canBuy && upgrades?.Upgrade_PlayerArmorLevel() == true) Refresh(); });
        if (bP_DmgType != null) bP_DmgType.RegisterCallback<ClickEvent>(_ => { CyclePlayerDamageType(); Refresh(); });
        if (bP_WeaponLvl != null) bP_WeaponLvl.RegisterCallback<ClickEvent>(_ => { if (bP_WeaponLvl.canBuy && upgrades?.Upgrade_PlayerWeaponLevel() == true) Refresh(); });
        if (bHealPlayer != null) bHealPlayer.RegisterCallback<ClickEvent>(_ => { if (bHealPlayer.canBuy && upgrades?.HealPlayer() == true) Refresh(); });

        if (bN_Level != null) bN_Level.RegisterCallback<ClickEvent>(_ => { if (bN_Level.canBuy && upgrades?.Upgrade_NexusLevel() == true) Refresh(); });
        if (bN_MaxHp != null) bN_MaxHp.RegisterCallback<ClickEvent>(_ => { if (bN_MaxHp.canBuy && upgrades?.Upgrade_NexusMaxHp() == true) Refresh(); });
        if (bN_ArmorType != null) bN_ArmorType.RegisterCallback<ClickEvent>(_ => { CycleNexusArmorType(); Refresh(); });
        if (bN_ArmorLvl != null) bN_ArmorLvl.RegisterCallback<ClickEvent>(_ => { if (bN_ArmorLvl.canBuy && upgrades?.Upgrade_NexusArmorStats() == true) Refresh(); });
        if (bHealNexus != null) bHealNexus.RegisterCallback<ClickEvent>(_ => { if (bHealNexus.canBuy && upgrades?.HealNexus() == true) Refresh(); });

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
        open = !open;
        if (open) Show(); else Hide();

        UnityEngine.Cursor.visible = open;
        UnityEngine.Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;

        if (pauseWhenOpen) Time.timeScale = open ? 0f : 1f;
        if (open) Refresh();
    }

    void Show() { if (root != null) root.style.display = DisplayStyle.Flex; }
    void Hide() { if (root != null) root.style.display = DisplayStyle.None; }

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

    // ---------- REFRESH (met à jour label/sub/price/état) ----------
    void Refresh()
    {
        if (upgrades == null) return;

        var wallet = (player ? player.GetComponent<GoldWallet>() : null) ?? upgrades.player?.GetComponent<GoldWallet>();
        int gold = wallet ? wallet.Amount : 0;
        bool Can(int cost) => wallet && gold >= cost;

        // HEAL
        UpdateActionButton(bHealPlayer, "Soin immédiat", "Améliorer la santé du joueur",
            upgrades.healPlayerCost, Can(upgrades.healPlayerCost), gold);

        UpdateActionButton(bHealNexus, "Réparer le Nexus", "Restaure les PV du Nexus",
            upgrades.healNexusCost, Can(upgrades.healNexusCost), gold);

        // PLAYER
        var costMaxHp = upgrades.GetCost_PlayerMaxHp();
        var costArmor = upgrades.GetCost_PlayerArmor();
        var costWeapon = upgrades.GetCost_PlayerWeapon();

        UpdateActionButton(bP_MaxHp, $"Santé max (Lv {upgrades.levels.playerMaxHpLvl + 1})", "Niveau +1", costMaxHp, Can(costMaxHp), gold);
        UpdateActionButton(bP_ArmorLvl, $"Armure (Lv {upgrades.levels.playerArmorLvl + 1})", "Niveau +1", costArmor, Can(costArmor), gold);
        UpdateActionButton(bP_WeaponLvl, $"Attaque (Lv {upgrades.levels.playerWeaponLvl + 1})", "Niveau +1", costWeapon, Can(costWeapon), gold);

        if (bP_ArmorType != null)
        {
            bP_ArmorType.SetLabel("Type d'armure");
            bP_ArmorType.SetSub(GetPlayerArmorType());
            bP_ArmorType.SetPrice("0 or");
            bP_ArmorType.SetCanBuy(true);
        }

        if (bP_DmgType != null)
        {
            bP_DmgType.SetLabel("Type d'attaque");
            bP_DmgType.SetSub(GetPlayerDamageType());
            bP_DmgType.SetPrice("0 or");
            bP_DmgType.SetCanBuy(true);
        }

        // NEXUS
        var costNexusLevel = upgrades.GetCost_NexusLevel();
        var costNexusMaxHp = upgrades.GetCost_NexusMaxHp();
        var costNexusArmor = upgrades.GetCost_NexusArmor();

        UpdateActionButton(bN_Level, $"Nexus Lv {upgrades.levels.nexusLevel + 1}", "Augmente le niveau", costNexusLevel, Can(costNexusLevel), gold);
        UpdateActionButton(bN_MaxHp, $"Nexus PV max (Lv {upgrades.levels.nexusMaxHpLvl + 1})", "Niveau +1", costNexusMaxHp, Can(costNexusMaxHp), gold);
        UpdateActionButton(bN_ArmorLvl, $"Nexus Armure (Lv {upgrades.levels.nexusArmorLvl + 1})", "Niveau +1", costNexusArmor, Can(costNexusArmor), gold);

        if (bN_ArmorType != null)
        {
            bN_ArmorType.SetLabel("Type d'armure Nexus");
            bN_ArmorType.SetSub(GetNexusArmorType());
            bN_ArmorType.SetPrice("0 or");
            bN_ArmorType.SetCanBuy(true);
        }

        // WAVES (inchangé)
        if (upgrades?.nexusSpawners != null && upgrades.nexusSpawners.Length > 0)
        {
            var sp = upgrades.nexusSpawners[0];
            if (sp != null)
            {
                if (tGiants != null) tGiants.SetValueWithoutNotify(sp.enableGiantSpawns);
                if (sBase != null) sBase.SetValueWithoutNotify(Mathf.RoundToInt(sp.baseGiantChance * 100f));
                if (sMax != null) sMax.SetValueWithoutNotify(Mathf.RoundToInt(sp.maxGiantChance * 100f));
                if (sRamp != null) sRamp.SetValueWithoutNotify(Mathf.RoundToInt(sp.giantChanceRampPerMinute * 100f));

                txtBase?.SetTextSafe($"Chance de base élites: {sBase?.value ?? 0}%");
                txtMax?.SetTextSafe($"Chance max élites: {sMax?.value ?? 0}%");
                txtRamp?.SetTextSafe($"Rampe élites (%/min): {sRamp?.value ?? 0}%");
            }
        }
    }

    void UpdateActionButton(StoreActionButton b, string label, string sub, int cost, bool canBuy, int gold)
    {
        if (b == null) return;
        b.SetLabel(label);
        b.SetSub(sub);
        b.SetPrice(cost >= 0 ? $"{cost} or" : "--");
        var missing = Mathf.Max(0, cost - gold);
        b.SetCanBuy(canBuy, canBuy ? 0 : missing);
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
