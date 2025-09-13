using UnityEngine;

[CreateAssetMenu(fileName = "DamageMatrix", menuName = "Balance/Damage Matrix")]
public class DamageMatrix : ScriptableObject
{
    // 7 types de dégâts x 6 types d’armure → 42 cellules
    [Range(0f, 2f)] public float[] table = new float[7 * 6];

    public float GetMultiplier(int damageType, int armorType)
    {
        int idx = damageType * 6 + armorType;
        if (idx < 0 || idx >= table.Length) return 1f;
        return table[idx] <= 0f ? 1f : table[idx];
    }
}
