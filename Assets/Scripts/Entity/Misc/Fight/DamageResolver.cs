using UnityEngine;

public class DamageResolver : MonoBehaviour
{
    public DamageMatrix matrix;

    private static DamageResolver _inst;
    public static DamageResolver Instance => _inst;

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        DontDestroyOnLoad(gameObject);
    }

    public static float GetMultiplier(DamageType dmg, ArmorType arm)
    {
        if (Instance == null || Instance.matrix == null) return 1f;
        return Instance.matrix.GetMultiplier((int)dmg, (int)arm);
    }
}
