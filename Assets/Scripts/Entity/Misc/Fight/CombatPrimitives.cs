// CombatPrimitives.cs
using System;
using UnityEngine;

public enum ArmorType { None, Light, Medium, Heavy, Shield, Energy }
public enum DamageType { Kinetic, Piercing, Explosive, Fire, Electric, Toxic, Cold }

[Serializable]
public struct DamageInfo
{
    public EntityController source;
    public float amount;
    public DamageType damageType;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public bool isCritical;

    public DamageInfo(EntityController src, float amt, DamageType type, Vector3 hp, Vector3 hn, bool crit = false)
    {
        source = src; amount = amt; damageType = type; hitPoint = hp; hitNormal = hn; isCritical = crit;
    }
}
