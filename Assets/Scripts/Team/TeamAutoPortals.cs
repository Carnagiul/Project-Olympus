using System.Linq;
using UnityEngine;

public class TeamAutoPortals : MonoBehaviour
{
    [Header("Config")]
    public Team team;                       // l’équipe propriétaire
    public PortalSpawner portalPrefab;      // prefab de PortalSpawner (V2)
    public Transform[] portalSpawnPoints;   // positions où placer les portails

    private void Start()
    {
        if (!team || !portalPrefab || portalSpawnPoints == null) return;
        var enemies = team.GetEnemyTeams().ToList();
        int n = Mathf.Min(enemies.Count, portalSpawnPoints.Length);

        for (int i = 0; i < n; i++)
        {
            var ps = team.CreatePortalToEnemy(enemies[i], portalPrefab, portalSpawnPoints[i]);
            // Tuning par équipe si besoin :
            // ps.baseGiantChance = 0.02f; etc.
        }
    }
}
