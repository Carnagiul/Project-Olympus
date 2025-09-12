using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;

public class PortalWaveSpawner : MonoBehaviour
{
    [Header("Global")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform monstersRoot;
    [SerializeField] private string autoRootName = "Monsters";

    [Header("Waves")]
    [SerializeField] private List<Wave> waves = new List<Wave>();

    [Header("Events")]
    public UnityEvent<int> onWaveStarted;
    public UnityEvent<int> onWaveCompleted;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;

        if (monstersRoot == null)
        {
            var existing = GameObject.Find(autoRootName);
            monstersRoot = existing != null ? existing.transform : new GameObject(autoRootName).transform;
        }

        TriggerWave(0); // Démarre la wave 0

    }

    /// <summary> Lance une wave précise (par index dans la liste). </summary>
    public void TriggerWave(int index)
    {
        if (index < 0 || index >= waves.Count) return;
        StartCoroutine(RunWave(index));
    }

    private IEnumerator RunWave(int index)
    {
        var wave = waves[index];
        onWaveStarted?.Invoke(index);

        foreach (var group in wave.groups)
        {
            yield return SpawnGroupRoutine(group);
        }

        onWaveCompleted?.Invoke(index);
    }

    private IEnumerator SpawnGroupRoutine(WaveGroup group)
    {
        Transform groupRoot = ResolveContainer(group.container, group.autoContainerName);

        if (group.parallelEntries)
        {
            var running = new List<Coroutine>();
            foreach (var e in group.entries)
                running.Add(StartCoroutine(SpawnEntryRoutine(e, groupRoot)));
            foreach (var c in running)
                yield return c;
        }
        else
        {
            foreach (var e in group.entries)
                yield return SpawnEntryRoutine(e, groupRoot);
        }
    }

    private IEnumerator SpawnEntryRoutine(UnitEntry entry, Transform groupRoot)
    {
        if (entry == null || entry.prefab == null || entry.count <= 0)
            yield break;

        Transform container = ResolveContainer(entry.container, entry.autoContainerName, groupRoot);

        int toSpawn = entry.count;
        while (toSpawn > 0)
        {
            SpawnOne(entry, container);
            toSpawn--;

            if (toSpawn > 0 && entry.interval > 0)
                yield return new WaitForSeconds(entry.interval);
        }
    }

    private void SpawnOne(UnitEntry entry, Transform parent)
    {
        Vector3 pos = (entry.overrideSpawnPoint != null ? entry.overrideSpawnPoint.position : spawnPoint.position)
                      + entry.positionOffset;

        Quaternion rot = (entry.overrideSpawnPoint != null ? entry.overrideSpawnPoint.rotation : spawnPoint.rotation);

        var go = Instantiate(entry.prefab, pos, rot, parent);

        if (entry.randomizeRotationY)
        {
            var euler = go.transform.eulerAngles;
            euler.y = Random.Range(0f, 360f);
            go.transform.eulerAngles = euler;
        }

        if (entry.applyScaleMultiplier != 1f)
            go.transform.localScale *= entry.applyScaleMultiplier;
    }

    private Transform ResolveContainer(Transform explicitContainer, string autoName, Transform fallback = null)
    {
        if (explicitContainer != null) return explicitContainer;
        if (fallback != null) return fallback;
        if (!string.IsNullOrEmpty(autoName))
        {
            var found = GameObject.Find(autoName);
            return found != null ? found.transform : new GameObject(autoName).transform;
        }
        return monstersRoot != null ? monstersRoot : transform;
    }
}

#region Data Models
[System.Serializable]
public class Wave
{
    public string name = "Wave";
    public List<WaveGroup> groups = new List<WaveGroup>();
}

[System.Serializable]
public class WaveGroup
{
    public string name = "Group";
    public Transform container;
    public string autoContainerName = "";
    public bool parallelEntries = false;
    public List<UnitEntry> entries = new List<UnitEntry>();
}

[System.Serializable]
public class UnitEntry
{
    public GameObject prefab;
    [Min(1)] public int count = 5;
    public float interval = 0.5f;

    public Transform overrideSpawnPoint;
    public Vector3 positionOffset;
    public Transform container;
    public string autoContainerName = "";

    public bool randomizeRotationY = false;
    public float applyScaleMultiplier = 1f;
}
#endregion
