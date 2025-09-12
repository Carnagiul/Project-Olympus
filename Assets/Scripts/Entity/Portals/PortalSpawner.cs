using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PortalSpawner : MonoBehaviour
{
    public enum SpawnMode { Single, Sequential, Random, Weighted }

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxSpawn = -1; // -1 = illimité
    [SerializeField] private Transform spawnPoint;

    [Header("Hierarchy")]
    [SerializeField] private Transform monsterFolder;
    [SerializeField] private string autoFolderName = "Monsters";

    [Header("Selection Mode")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.Single;
    [SerializeField] private GameObject monsterPrefab;          // Single
    [SerializeField] private List<GameObject> prefabs;          // Sequential/Random
    [SerializeField] private List<WeightedEntry> weightedPrefabs; // Weighted

    [Header("Runtime Control")]
    [SerializeField] private bool startEnabled = true;          // état initial
    [SerializeField] private bool autoDisableOnMax = true;      // coupe après maxSpawn atteint
    [SerializeField] private bool spawnEnabled = true;
    [SerializeField] private GameObject[] visualsToToggle;      // optionnel: FX/mesh à cacher quand off

    private float timer = 0f;
    private int spawnedCount = 0;
    private int sequentialIndex = 0;

    [System.Serializable]
    public class WeightedEntry { public GameObject prefab; public float weight = 1f; }

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;

        if (monsterFolder == null)
        {
            var existing = GameObject.Find(autoFolderName);
            monsterFolder = existing ? existing.transform : new GameObject(autoFolderName).transform;
        }
        SetSpawning(startEnabled, affectVisuals: true);
    }

    private void Update()
    {
        if (!spawnEnabled) return;

        // option: si max atteint, auto-off
        if (maxSpawn != -1 && spawnedCount >= maxSpawn)
        {
            if (autoDisableOnMax) SetSpawning(false);
            return;
        }

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
        if (!prefabToSpawn) return;

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

            default: return null;
        }
    }

    private GameObject PickWeighted(List<WeightedEntry> entries)
    {
        if (entries == null || entries.Count == 0) return null;
        float total = 0f;
        foreach (var e in entries) if (e?.prefab && e.weight > 0) total += e.weight;
        if (total <= 0f) return null;

        float r = Random.value * total;
        foreach (var e in entries)
        {
            if (!e?.prefab || e.weight <= 0) continue;
            if (r < e.weight) return e.prefab;
            r -= e.weight;
        }
        return null;
    }

    // --------- API RUNTIME : ON/OFF, Pause, Reset ---------

    /// Active/désactive le spawn (et optionnellement les visuels).
    public void SetSpawning(bool enabled, bool affectVisuals = false)
    {
        spawnEnabled = enabled;
        if (affectVisuals && visualsToToggle != null)
        {
            foreach (var go in visualsToToggle) if (go) go.SetActive(enabled);
        }
    }

    public void EnableSpawning(bool affectVisuals = false) => SetSpawning(true, affectVisuals);
    public void DisableSpawning(bool affectVisuals = false) => SetSpawning(false, affectVisuals);
    public void ToggleSpawning(bool affectVisuals = false) => SetSpawning(!spawnEnabled, affectVisuals);

    /// Pause le spawn pendant X secondes puis reprend automatiquement.
    public void PauseFor(float seconds, bool affectVisuals = false)
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(PauseRoutine(seconds, affectVisuals));
    }
    private IEnumerator PauseRoutine(float seconds, bool affectVisuals)
    {
        bool prev = spawnEnabled;
        SetSpawning(false, affectVisuals);
        yield return new WaitForSeconds(seconds);
        SetSpawning(prev, affectVisuals);
    }

    /// Force un spawn immédiat (même si timer pas écoulé), seulement si activé.
    public void ForceSpawnOnce()
    {
        if (!spawnEnabled) return;
        SpawnMonster();
    }

    /// Reset (compteur/timer) et optionnellement les limites/intervalle.
    public void ResetSpawner(float? newInterval = null, int? newMaxSpawn = null, bool resetCounter = true)
    {
        if (resetCounter)
        {
            spawnedCount = 0;
            sequentialIndex = 0;
        }
        timer = 0f;
        if (newInterval.HasValue) spawnInterval = newInterval.Value;
        if (newMaxSpawn.HasValue) maxSpawn = newMaxSpawn.Value;
    }

    // --------- API RUNTIME : changer le type d’unité ---------

    public void SetMonsterPrefab(GameObject newPrefab)
    {
        monsterPrefab = newPrefab;
        spawnMode = SpawnMode.Single;
    }
    public void SetPrefabs(List<GameObject> newPrefabs, SpawnMode mode = SpawnMode.Sequential)
    {
        prefabs = newPrefabs ?? new List<GameObject>();
        spawnMode = mode;
        sequentialIndex = 0;
    }
    public void SetWeighted(List<WeightedEntry> newWeighted)
    {
        weightedPrefabs = newWeighted ?? new List<WeightedEntry>();
        spawnMode = SpawnMode.Weighted;
    }

    public bool IsSpawning => spawnEnabled;
}
