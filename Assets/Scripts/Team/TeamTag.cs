using UnityEngine;

[DisallowMultipleComponent]
public class TeamTag : MonoBehaviour
{
    public Team team;

    // Optionnel : un “tint” simple sur le rendu
    [Header("Visual Tint (optionnel)")]
    public Renderer[] renderersToTint;
    [Range(0f, 1f)] public float tintStrength = 0.6f;

    private MaterialPropertyBlock _mpb;

    public void SetTeam(Team t)
    {
        team = t;
        TryApplyTint();
    }

    private void TryApplyTint()
    {
        if (renderersToTint == null || renderersToTint.Length == 0 || team == null) return;
        _mpb ??= new MaterialPropertyBlock();
        foreach (var r in renderersToTint)
        {
            if (!r) continue;
            r.GetPropertyBlock(_mpb);
            Color c = Color.Lerp(Color.white, team.teamColor, tintStrength);
            _mpb.SetColor("_Color", c); // fonctionne avec de nombreux shaders Standard/URP
            r.SetPropertyBlock(_mpb);
        }
    }
}
