using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    [UxmlElement]
    public partial class GoldPill : VisualElement
    {
        [UxmlAttribute] public int amount { get; set; } = 0;
        [UxmlAttribute] public string icon { get; set; } = "●";
        Label _amount, _icon;

        // Références aux assets (optionnelles) : assignables dans UI Builder
        [UxmlAttribute] public VisualTreeAsset uxml { get; set; }
        [UxmlAttribute] public StyleSheet uss { get; set; }
        public GoldPill()
        {
            AddToClassList("goldpill");
            _icon = new Label(icon) { name = "Icon" }; _icon.AddToClassList("goldpill__icon");
            _amount = new Label(amount.ToString()) { name = "Amount" }; _amount.AddToClassList("goldpill__amount");
            Add(_icon); Add(_amount);
        }
        public void SetAmount(int value) { amount = value; if (_amount != null) _amount.text = value.ToString(); }
    }
}