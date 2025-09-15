using UnityEngine;

public class PortalSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField, Min(0f)] private float spawnInterval = 5f;
    [SerializeField] private int maxSpawn = -1;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform monsterFolder;

    [Header("State")]
    public bool isActive = true;

    [Header("Teams")]
    public Team ownerTeam;   // équipe source
    public Team targetTeam;  // équipe ciblée (Nexus à attaquer)

    [Header("Giant (V2)")]
    public bool enableGiantSpawns = true;
    public GiantMultipliers giantProfile = new GiantMultipliers { healthMult = 2.5f, damageMult = 1.8f, speedMult = 0.9f, sizeMult = 1.75f };
    [Range(0f, 1f)] public float baseGiantChance = 0.01f;
    [Range(0f, 1f)] public float maxGiantChance = 0.25f;
    [Range(0f, 1f)] public float resetGiantChance = 0.01f;

    [Header("Giant Ramp")]
    public bool rampByTime = true;
    [Min(0f)] public float giantChanceRampPerMinute = 0.03f;
    [Range(0f, 1f)] public float chanceIncreasePerSpawn = 0.02f;
    [Min(0f)] public float giantCooldownSeconds = 0f;

    // runtime
    float _timer, _giantCooldownTimer, _currentGiantChance;
    int _spawnedCount;

    void Start()
    {
        if (!spawnPoint) spawnPoint = transform;
        if (!ownerTeam) ownerTeam = GetComponentInParent<Team>();
        _currentGiantChance = Mathf.Clamp01(baseGiantChance);
    }

    void Update()
    {
        if (!isActive) return;

        if (_giantCooldownTimer > 0f)
            _giantCooldownTimer = Mathf.Max(0f, _giantCooldownTimer - Time.deltaTime);

        if (enableGiantSpawns && rampByTime && maxGiantChance > 0f && giantChanceRampPerMinute > 0f)
        {
            float inc = (giantChanceRampPerMinute / 60f) * Time.deltaTime;
            _currentGiantChance = Mathf.Clamp01(_currentGiantChance + inc);
            if (_currentGiantChance > maxGiantChance) _currentGiantChance = maxGiantChance;
        }

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnMonster();
        }
    }

    void SpawnMonster()
    {
        if (!monsterPrefab) return;
        if (maxSpawn >= 0 && _spawnedCount >= maxSpawn) return;

        bool canSpawnGiant = enableGiantSpawns && _giantCooldownTimer <= 0f;
        bool spawnAsGiant = canSpawnGiant && Random.value < _currentGiantChance;

        var go = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);
        if (monsterFolder) go.transform.SetParent(monsterFolder, true);

        // équipe source + teinte éventuelle
        var tag = go.GetComponent<TeamTag>() ?? go.AddComponent<TeamTag>();
        tag.SetTeam(ownerTeam);

        // cible Nexus correcte (méthode 2)
        var mc = go.GetComponentInChildren<MonsterController>();
        if (mc && targetTeam) mc.SetTargetTeam(targetTeam);

        // géant
        if (spawnAsGiant)
        {
            var comps = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in comps)
                if (mb is IGiantifiable v2)
                    v2.ApplyGiantMultipliers(giantProfile);

            _currentGiantChance = Mathf.Clamp01(resetGiantChance);
            if (giantCooldownSeconds > 0f) _giantCooldownTimer = giantCooldownSeconds;
        }
        else if (!rampByTime && enableGiantSpawns && chanceIncreasePerSpawn > 0f)
        {
            _currentGiantChance = Mathf.Clamp01(_currentGiantChance + chanceIncreasePerSpawn);
            if (_currentGiantChance > maxGiantChance) _currentGiantChance = maxGiantChance;
        }

        _spawnedCount++;
    }

    // API
    public void SetActive(bool value) => isActive = value;
    public void ToggleActive() => isActive = !isActive;
    public void ResetGiantProbability() => _currentGiantChance = Mathf.Clamp01(baseGiantChance);

#if UNITY_EDITOR
    void OnValidate()
    {
        if (maxGiantChance < baseGiantChance) maxGiantChance = baseGiantChance;
        if (giantChanceRampPerMinute < 0f) giantChanceRampPerMinute = 0f;
        if (chanceIncreasePerSpawn < 0f) chanceIncreasePerSpawn = 0f;
        if (giantProfile.healthMult <= 0f) giantProfile.healthMult = 1f;
        if (giantProfile.damageMult <= 0f) giantProfile.damageMult = 1f;
        if (giantProfile.speedMult <= 0f) giantProfile.speedMult = 1f;
        if (giantProfile.sizeMult <= 0f) giantProfile.sizeMult = 1f;
    }
#endif
}
