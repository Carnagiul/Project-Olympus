using System;
using UnityEngine;

[Serializable]
public class PriceCurve
{
    [Min(0)] public int baseCost = 50;      // coût niveau 0 → 1
    [Min(0)] public int stepCost = 25;      // + coût par niveau atteint
    [Min(1)] public float stepMult = 1f;    // multiplicateur par niveau (optionnel)

    public int GetCost(int currentLevel)
    {
        // Coût = base + step * level, puis multiplicateur exponentiel si besoin
        double cost = baseCost + stepCost * currentLevel;
        if (stepMult != 1f)
            cost *= Math.Pow(stepMult, currentLevel);
        return Mathf.RoundToInt((float)cost);
    }
}
