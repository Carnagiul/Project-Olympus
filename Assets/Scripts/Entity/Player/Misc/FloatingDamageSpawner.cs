// FloatingDamageSpawner.cs
using UnityEngine;

[DisallowMultipleComponent]
public class FloatingDamageSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Camera lookAtCamera;                  // pour billboard (si null, on tentera FpsController.playerCamera)

    [Header("Prefab")]
    public FloatingDamageText damageTextPrefab;  // un prefab portant FloatingDamageText + TMP_Text

    [Header("Offsets")]
    public Vector3 worldOffset = new Vector3(0f, 0.2f, 0f);

    void Awake()
    {
        if (!lookAtCamera)
        {
            var fps = GetComponentInParent<FpsController>();
            if (fps) lookAtCamera = fps.playerCamera;
            if (!lookAtCamera) lookAtCamera = Camera.main;
        }
    }

    /// <summary>Instancie un texte de dégâts au monde.</summary>
    public void Spawn(Vector3 worldPos, float damage)
    {
        if (!damageTextPrefab) return;

        var go = Instantiate(damageTextPrefab, worldPos + worldOffset, Quaternion.identity);
        go.Init(damage);

        // Billboard vers la caméra
        if (lookAtCamera)
        {
            var toCam = (lookAtCamera.transform.position - go.transform.position).normalized;
            if (toCam.sqrMagnitude > 1e-4f)
                go.transform.rotation = Quaternion.LookRotation(toCam);
        }
    }
}
