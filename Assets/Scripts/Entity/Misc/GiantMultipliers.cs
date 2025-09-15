using UnityEngine;

[System.Serializable]
public struct GiantMultipliers
{
    [Min(0f)] public float healthMult;  // ex: 2.5
    [Min(0f)] public float damageMult;  // ex: 1.8
    [Min(0f)] public float speedMult;   // ex: 0.9 (plus lent)
    [Min(0f)] public float sizeMult;    // ex: 1.75

    public static GiantMultipliers Identity => new GiantMultipliers
    {
        healthMult = 1f,
        damageMult = 1f,
        speedMult = 1f,
        sizeMult = 1f
    };
}
