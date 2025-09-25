using UnityEngine;
using UnityEngine.UIElements;


namespace Assets.Scripts.UI
{
    [UxmlElement]
public partial class HudBar : VisualElement
{
    [UxmlAttribute] public string label { get; set; } = "HP";
    [UxmlAttribute] public float current { get; set; } = 60f;
    [UxmlAttribute] public float max { get; set; } = 100f;
    [UxmlAttribute] public string theme { get; set; } = "hp"; // hp | mana | endu
    Label _label; VisualElement _fill;

        // Références aux assets (optionnelles) : assignables dans UI Builder
    [UxmlAttribute] public VisualTreeAsset uxml { get; set; }
     [UxmlAttribute] public StyleSheet uss { get; set; }
        public HudBar()
    {
        AddToClassList("hudbar");
        _label = new Label(label) { name = "Label" }; _label.AddToClassList("hudbar__label");
        var track = new VisualElement() { name = "Track" }; track.AddToClassList("hudbar__track");
        _fill = new VisualElement() { name = "Fill" }; _fill.AddToClassList("hudbar__fill"); track.Add(_fill);
        Add(_label); Add(track);
        Apply();
    }
    void Apply()
    {
        _label.text = label;
        var pct = Mathf.Approximately(max, 0f) ? 0f : Mathf.Clamp01(current / max);
        _fill.style.width = Length.Percent(pct * 100f);
        // thèmes
        RemoveFromClassList("hudbar--hp"); RemoveFromClassList("hudbar--mana"); RemoveFromClassList("hudbar--endu");
        AddToClassList(theme == "mana" ? "hudbar--mana" : theme == "endu" ? "hudbar--endu" : "hudbar--hp");
    }
    public void Set(float cur, float m) { current = cur; max = m; Apply(); }
    public void SetTheme(string t) { theme = t; Apply(); }
    public void SetLabel(string l) { label = l; Apply(); }
}}