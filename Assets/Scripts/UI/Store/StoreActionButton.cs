using UnityEngine;
using UnityEngine.UIElements;

// Unity 6.x : utilisez UxmlElement + UxmlAttribute (UxmlFactory/UxmlTraits obsolètes)
// Placez ce script dans un assembly accessible à l'UI Builder (ex: Assets/Scripts/GUI/WIP)
namespace Assets.Scripts.UI.Store
{
    [UxmlElement]
    public partial class StoreActionButton : VisualElement
    {
        // === Attributs UXML exposés ===
        [UxmlAttribute] public string label { get; set; } = "Action";
        [UxmlAttribute] public string sub { get; set; } = "Description";
        [UxmlAttribute] public string price { get; set; } = "0 or";
        [UxmlAttribute] public string icon { get; set; } = "❤";

        // État d'achat
        [UxmlAttribute] public bool canBuy { get; set; } = true;
        [UxmlAttribute] public int missingGold { get; set; } = 0;

        // Références aux assets (optionnelles) : assignables dans UI Builder
        [UxmlAttribute] public VisualTreeAsset uxml { get; set; }
        [UxmlAttribute] public StyleSheet uss { get; set; }

        // Sous-éléments (bind dynamiques)
        private Label _label;
        private Label _sub;
        private Label _price;
        private Label _icon;
        private Label _lock;

        private bool _built;

        public StoreActionButton()
        {
            // Construction paresseuse une fois attaché au panel (les UxmlAttribute sont alors appliqués)
            RegisterCallback<AttachToPanelEvent>(_ => BuildIfNeeded());
        }

        private void BuildIfNeeded()
        {
            if (_built) return;
            _built = true;

            // 1) StyleSheet
            if (uss != null)
                styleSheets.Add(uss);
            else
            {
                // Fallback (si vous mettez l'USS sous Resources/UI/Store/)
                var fallbackUSS = Resources.Load<StyleSheet>("UI/Store/StoreActionButton");
                if (fallbackUSS != null) styleSheets.Add(fallbackUSS);
            }

            // 2) Arbre visuel
            if (uxml != null)
                uxml.CloneTree(this);
            else
            {
                // Fallback (si vous mettez l'UXML sous Resources/UI/Store/)
                var fallbackVTA = Resources.Load<VisualTreeAsset>("UI/Store/StoreActionButton");
                if (fallbackVTA != null) fallbackVTA.CloneTree(this);
            }

            // 3) Récupérer les références
            _label = this.Q<Label>("Label");
            _sub = this.Q<Label>("Sub");
            _price = this.Q<Label>("Price");
            _icon = this.Q<Label>("Icon");
            _lock = this.Q<Label>("Lock");

            // 4) Appliquer l'état initial
            ApplyContent();
            ApplyState();
        }

        // ----- API publique (peut aussi être manipulée via UXML / UI Builder) -----
        public void SetLabel(string text) { label = text; if (_label != null) _label.text = text; }
        public void SetSub(string text) { sub = text; if (_sub != null) _sub.text = text; }
        public void SetPrice(string text) { price = text; if (_price != null) _price.text = text; }
        public void SetIcon(string text) { icon = text; if (_icon != null) _icon.text = text; }

        public void SetCanBuy(bool value, int missing = 0)
        {
            canBuy = value; missingGold = missing; ApplyState();
        }

        // ----- Impl -----
        private void ApplyContent()
        {
            if (_label != null) _label.text = label;
            if (_sub != null) _sub.text = sub;
            if (_price != null) _price.text = price;
            if (_icon != null) _icon.text = icon;
        }

        private void ApplyState()
        {
            RemoveFromClassList("can-buy");
            RemoveFromClassList("no-funds");

            if (canBuy)
            {
                AddToClassList("can-buy");
                tooltip = string.Empty;
                if (_lock != null) _lock.style.display = DisplayStyle.None;
            }
            else
            {
                AddToClassList("no-funds");
                tooltip = missingGold > 0 ? $"Manque {missingGold} or" : "Fonds insuffisants";
                if (_lock != null) _lock.style.display = DisplayStyle.Flex;
            }
        }
    }
}