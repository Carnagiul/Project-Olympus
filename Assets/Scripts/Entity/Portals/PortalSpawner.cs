using UnityEngine;
using System.Collections.Generic;

public class PortalSpawner : MonoBehaviour
{
    public enum SpawnMode { Single, Sequential, Random, Weighted }

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxSpawn = -1; // -1 = illimité
    [SerializeField] private Transform spawnPoint;

    [Header("Hierarchy")]
    [SerializeField] private Transform monsterFolder; // dossier pour ranger les monstres
    [SerializeField] private string autoFolderName = "Monsters"; // sera créé/trouvé si monsterFolder == null

    [Header("Selection Mode")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.Single;

    [Tooltip("Mode Single : utilise ceci")]
    [SerializeField] private GameObject monsterPrefab;

    [Tooltip("Mode Sequential/Random : utilise cette liste")]
    [SerializeField] private List<GameObject> prefabs = new List<GameObject>();

    [Tooltip("Mode Weighted : utilise cette liste (poids > 0)")]
    [SerializeField] private List<WeightedEntry> weightedPrefabs = new List<WeightedEntry>();

    private float timer = 0f;
    private int spawnedCount = 0;
    private int sequentialIndex = 0;

    [System.Serializable]
    public class WeightedEntry
    {
        public GameObject prefab;
        public float weight = 1f; // > 0
    }

    private void Awake()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        if (monsterFolder == null)
        {
            var existing = GameObject.Find(autoFolderName);
            monsterFolder = existing ? existing.transform : new GameObject(autoFolderName).transform;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnMonster();
        }
    }

    private void SpawnMonster()
    {
        if (maxSpawn != -1 && spawnedCount >= maxSpawn) return;

        var prefabToSpawn = ResolvePrefab();
        if (prefabToSpawn == null) return;

        Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation, monsterFolder);
        spawnedCount++;
    }

    private GameObject ResolvePrefab()
    {
        switch (spawnMode)
        {
            case SpawnMode.Single:
                return monsterPrefab;

            case SpawnMode.Sequential:
                if (prefabs == null || prefabs.Count == 0) return null;
                var p = prefabs[sequentialIndex % prefabs.Count];
                sequentialIndex = (sequentialIndex + 1) % Mathf.Max(1, prefabs.Count);
                return p;

            case SpawnMode.Random:
                if (prefabs == null || prefabs.Count == 0) return null;
                return prefabs[Random.Range(0, prefabs.Count)];

            case SpawnMode.Weighted:
                return PickWeighted(weightedPrefabs);

            default:
                return null;
        }
    }

    private GameObject PickWeighted(List<WeightedEntry> entries)
    {
        if (entries == null || entries.Count == 0) return null;

        float total = 0f;
        foreach (var e in entries)
            if (e != null && e.prefab != null && e.weight > 0f)
                total += e.weight;

        if (total <= 0f) return null;

        float r = Random.value * total;
        foreach (var e in entries)
        {
            if (e == null || e.prefab == null || e.weight <= 0f) continue;
            if (r < e.weight) return e.prefab;
            r -= e.weight;
        }
        return null;
    }

    // --------- API RUNTIME (pour changer dynamiquement) ---------

    /// <summary>Change le prefab en mode Single (immédiat, runtime OK).</summary>
    public void SetMonsterPrefab(GameObject newPrefab)
    {
        monsterPrefab = newPrefab;
        spawnMode = SpawnMode.Single;
    }

    /// <summary>Remplace la liste de prefabs (Sequential/Random) et réinitialise l’index.</summary>
    public void SetPrefabs(List<GameObject> newPrefabs, SpawnMode mode = SpawnMode.Sequential)
    {
        prefabs = newPrefabs ?? new List<GameObject>();
        spawnMode = mode;
        sequentialIndex = 0;
    }

    /// <summary>Remplace la table pondérée (Weighted).</summary>
    public void SetWeighted(List<WeightedEntry> newWeighted)
    {
        weightedPrefabs = newWeighted ?? new List<WeightedEntry>();
        spawnMode = SpawnMode.Weighted;
    }

    /// <summary>Force un spawn immédiat (ignore le timer).</summary>
    public void ForceSpawnOnce()
    {
        SpawnMonster();
    }

    /// <summary>Réinitialise le compteur et le timer (utile pour relancer un cycle).</summary>
    public void ResetSpawner(float? newInterval = null, int? newMaxSpawn = null)
    {
        timer = 0f;
        spawnedCount = 0;
        if (newInterval.HasValue) spawnInterval = newInterval.Value;
        if (newMaxSpawn.HasValue) maxSpawn = newMaxSpawn.Value;
        sequentialIndex = 0;
    }
}
