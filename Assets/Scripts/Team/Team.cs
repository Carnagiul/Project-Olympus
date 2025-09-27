using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Team : MonoBehaviour
{
    [Header("Identité")]
    public string teamName = "Team";
    public Color teamColor = Color.white;

    [Header("Structure")]
    public NexusController nexus;                                   // Le Nexus de l’équipe


    public List<GameObject> players = new();         // Tes composants joueurs (FPS)
    public List<PortalSpawner> portals = new();      // Portails “sortants” vers les ennemis
    
    public List<PortalSpawner> recievePortals = new();      // Portails “Monsters” des ennemis

    [SerializeField] private List<GameObject> monsters = new();
    private void Start()
    {
        if (nexus != null)
        {
            //nexus.Team = this;
            nexus.OnNexusLevelChanged.AddListener(OnNexusLevelChanged);
        }
    }

    private void OnDestroy()
    {
        if (nexus != null)
        {
            nexus.OnNexusLevelChanged.RemoveListener(OnNexusLevelChanged);
        }
    }

    public void updateNexusLevel(int level)
    {
        if (nexus != null)
        {
            nexus.NexusLevel = level;
        }

        if (level <= 1)
            recievePortals?.ForEach(p =>
            {
                p.monsterPrefab = monsters[0];
            });
        else if (level >= monsters.Count)
        {
            recievePortals?.ForEach(p =>
            {
                p.monsterPrefab = monsters[monsters.Count - 1];
            });
        }
        else
        {
            recievePortals?.ForEach(p =>
            {
                p.monsterPrefab = monsters[level - 1];
            });
        }
    }

    public void OnNexusLevelChanged(int level)
    {
        Debug.Log("Detect an change");
        updateNexusLevel(level);
    }
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
