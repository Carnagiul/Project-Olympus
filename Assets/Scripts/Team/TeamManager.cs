using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance { get; private set; }

    [Tooltip("D�clare ici toutes les �quipes pr�sentes en jeu.")]
    public List<Team> teams = new();

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public Team GetTeamByName(string name)
    {
        return teams.Find(t => t && t.teamName == name);
    }
}
