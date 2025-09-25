using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    [UxmlElement]
public partial class LocationPill : VisualElement
{
    [UxmlAttribute] public string label { get; set; } = "Forêt des Ombres";
    [UxmlAttribute] public string sub { get; set; } = "Zone 3";
    [UxmlAttribute] public string icon { get; set; } = "🧭";

        // Références aux assets (optionnelles) : assignables dans UI Builder
        [UxmlAttribute] public VisualTreeAsset uxml { get; set; }
        [UxmlAttribute] public StyleSheet uss { get; set; }
        public LocationPill()
    {
        AddToClassList("locpill");
        var ic = new Label(icon) { name = "Icon" }; ic.AddToClassList("locpill__icon");
        var lb = new Label(label) { name = "Label" }; lb.AddToClassList("locpill__label");
        var sb = new Label(sub) { name = "Sub" }; sb.AddToClassList("locpill__sub");
        Add(ic); Add(lb); Add(sb);
    }
}}