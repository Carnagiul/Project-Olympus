using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class GoldWallet : MonoBehaviour
{
    [Min(0)][SerializeField] private int gold = 0;
    public int Amount => gold;

    [Header("Events")]
    public UnityEvent<int> OnGoldChanged; // (new amount)

    void Awake() => OnGoldChanged ??= new UnityEvent<int>();

    public void Add(int value)
    {
        if (value <= 0) return;
        gold = Mathf.Max(0, gold + value);
        OnGoldChanged.Invoke(gold);
    }

    public bool TrySpend(int cost)
    {
        if (cost < 0) return false;
        if (gold < cost) return false;
        gold -= cost;
        OnGoldChanged.Invoke(gold);
        return true;
    }

    public void Set(int value)
    {
        gold = Mathf.Max(0, value);
        OnGoldChanged.Invoke(gold);
    }
}
