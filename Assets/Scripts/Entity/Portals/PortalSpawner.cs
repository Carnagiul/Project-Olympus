using UnityEngine;

public class PortalSpawner : MonoBehaviour
{
	[Header("Spawn Settings")]
	[SerializeField] private GameObject monsterPrefab; // le prefab du monstre à spawn
	[SerializeField] private float spawnInterval = 5f; // temps en secondes entre chaque spawn
	[SerializeField] private int maxSpawn = -1; // -1 = illimité, sinon limite de monstres à spawn
	[SerializeField] private Transform spawnPoint; // optionnel : où spawn les monstres

    [Header("Spawn Settings")]
    [SerializeField] private Transform monsterFolder; // Le dossier ou irons les mobs spawner

    private float timer = 0f;
	private int spawnedCount = 0;

	private void Start()
	{
		if (spawnPoint == null)
			spawnPoint = transform; // par défaut le Portal lui-même
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
		if (monsterPrefab == null) return;

		if (maxSpawn != -1 && spawnedCount >= maxSpawn) return;

		Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation, monsterFolder);
		spawnedCount++;
	}
}
