using System.Diagnostics;
using UnityEngine.UIElements;

public static class UIEHelpers
{
    public static void SetTextSafe(this Label lbl, string value)
    {
        if (lbl != null)
        {
            UnityEngine.Debug.Log($"Old Text : {lbl.text} > {value}");

            lbl.text = value;
        }
    }
}
