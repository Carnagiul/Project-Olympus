using UnityEngine;

public class PortalSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField, Min(0f)] private float spawnInterval = 5f;
    [SerializeField] private int maxSpawn = -1; // -1 = illimité
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform monsterFolder;

    [Header("Activation")]
    [Tooltip("Permet d'activer/désactiver le spawn à la volée.")]
    public bool isActive = true;

    [Header("Giant Unit Settings (V2)")]
    [Tooltip("Active la possibilité de faire apparaître des unités géantes.")]
    public bool enableGiantSpawns = true;

    [Tooltip("Profil de multiplicateurs appliqué aux géants.")]
    public GiantMultipliers giantProfile = new GiantMultipliers
    {
        healthMult = 2.5f,
        damageMult = 1.8f,
        speedMult = 0.9f,
        sizeMult = 1.75f
    };

    [Tooltip("Probabilité de base au démarrage (0..1).")]
    [Range(0f, 1f)] public float baseGiantChance = 0.01f;

    [Tooltip("Probabilité maxi (0..1).")]
    [Range(0f, 1f)] public float maxGiantChance = 0.25f;

    [Tooltip("Réinitialisation de la proba après l'apparition d'un géant.")]
    [Range(0f, 1f)] public float resetGiantChance = 0.01f;

    [Space]
    [Tooltip("Fait croître la probabilité avec le TEMPS (sinon, par SPAWN).")]
    public bool rampByTime = true;

    [Tooltip("Vitesse d'augmentation par minute si rampByTime = true (ex: 0.03 = +3%/min).")]
    [Min(0f)] public float giantChanceRampPerMinute = 0.03f;

    [Tooltip("Augmentation par spawn manqué si rampByTime = false (ex: 0.02 = +2% par spawn).")]
    [Range(0f, 1f)] public float chanceIncreasePerSpawn = 0.02f;

    [Space]
    [Tooltip("Cooldown (secondes) minimal entre deux apparitions de géants.")]
    [Min(0f)] public float giantCooldownSeconds = 0f;

    // --- Runtime ---
    private float _timer = 0f;
    private int _spawnedCount = 0;
    private float _currentGiantChance;
    private float _giantCooldownTimer = 0f;

    private void Start()
    {
        if (!spawnPoint) spawnPoint = transform;
        _currentGiantChance = Mathf.Clamp01(baseGiantChance);
    }

    private void Update()
    {
        if (!isActive) return;

        // Cooldown géant
        if (_giantCooldownTimer > 0f)
            _giantCooldownTimer = Mathf.Max(0f, _giantCooldownTimer - Time.deltaTime);

        // Montée de proba avec le temps (optionnelle)
        if (enableGiantSpawns && rampByTime && maxGiantChance > 0f && giantChanceRampPerMinute > 0f)
        {
            float inc = (giantChanceRampPerMinute / 60f) * Time.deltaTime;
            _currentGiantChance = Mathf.Clamp01(_currentGiantChance + inc);
            if (_currentGiantChance > maxGiantChance) _currentGiantChance = maxGiantChance;
        }

        // Timer de spawn
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnMonster();
        }
    }

    private void SpawnMonster()
    {
        if (!monsterPrefab) return;
        if (maxSpawn >= 0 && _spawnedCount >= maxSpawn) return;

        // Tirage géant ?
        bool canSpawnGiant = enableGiantSpawns && _giantCooldownTimer <= 0f;
        bool spawnAsGiant = false;

        if (canSpawnGiant)
        {
            float roll = Random.value;
            spawnAsGiant = roll < _currentGiantChance;
        }

        // Instantiation
        GameObject go = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);
        if (monsterFolder) go.transform.SetParent(monsterFolder, true);

        // Application des multiplicateurs géants (V2 only)
        if (spawnAsGiant)
        {
            var comps = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in comps)
                if (mb is IGiantifiable v2)
                    v2.ApplyGiantMultipliers(giantProfile);

            // Reset de proba + cooldown
            _currentGiantChance = Mathf.Clamp01(resetGiantChance);
            if (giantCooldownSeconds > 0f)
                _giantCooldownTimer = giantCooldownSeconds;
        }
        else
        {
            // Montée par spawn si on n'utilise pas la montée par temps
            if (!rampByTime && enableGiantSpawns && chanceIncreasePerSpawn > 0f)
            {
                _currentGiantChance = Mathf.Clamp01(_currentGiantChance + chanceIncreasePerSpawn);
                if (_currentGiantChance > maxGiantChance) _currentGiantChance = maxGiantChance;
            }
        }

        _spawnedCount++;
    }

    // --- API publique ---

    public void SetActive(bool value) => isActive = value;
    public void ToggleActive() => isActive = !isActive;

    public void ResetGiantProbability() => _currentGiantChance = Mathf.Clamp01(baseGiantChance);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (resetGiantChance < 0f) resetGiantChance = 0f;
        if (maxGiantChance < baseGiantChance) maxGiantChance = baseGiantChance;
        if (chanceIncreasePerSpawn < 0f) chanceIncreasePerSpawn = 0f;
        if (giantChanceRampPerMinute < 0f) giantChanceRampPerMinute = 0f;

        // garde un profil valide
        if (giantProfile.healthMult <= 0f) giantProfile.healthMult = 1f;
        if (giantProfile.damageMult <= 0f) giantProfile.damageMult = 1f;
        if (giantProfile.speedMult <= 0f) giantProfile.speedMult = 1f;
        if (giantProfile.sizeMult <= 0f) giantProfile.sizeMult = 1f;
    }
#endif
}
