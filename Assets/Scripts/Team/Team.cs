using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Team : MonoBehaviour
{
    [Header("Identité")]
    public string teamName = "Team";
    public Color teamColor = Color.white;

    [Header("Structure")]
    public NexusController nexus;                          // Le Nexus de l’équipe
    public List<GameObject> players = new();         // Tes composants joueurs (FPS)
    public List<PortalSpawner> portals = new();      // Portails “sortants” vers les ennemis

    // Utilitaire rapide
    public IEnumerable<Team> GetEnemyTeams()
    {
        if (!TeamManager.Instance) yield break;
        foreach (var t in TeamManager.Instance.teams)
            if (t && t != this) yield return t;
    }

    // Pour créer dynamiquement un portail vers une team cible (et l’ajouter à la liste)
    public PortalSpawner CreatePortalToEnemy(Team target, PortalSpawner prefab, Transform where)
    {
        var ps = Instantiate(prefab, where.position, where.rotation, transform);
        ps.ownerTeam = this;
        ps.targetTeam = target;
        portals.Add(ps);
        return ps;
    }
}
