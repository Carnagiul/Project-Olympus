using UnityEngine;
using UnityEngine.InputSystem; // NEW INPUT SYSTEM

public class FpsLook : MonoBehaviour
{
    [Header("Sensitivity")]
    public float mouseSensitivity = 0.15f; // facteur sur delta souris (pixels/frame)
    public float pitchMin = -85f;
    public float pitchMax = 85f;

    [Header("References")]
    public Transform playerBody;

    private float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null) return;

        // Mouse delta (pixels/frame) -> on ne multiplie PAS par Time.deltaTime
        Vector2 md = mouse.delta.ReadValue();

        // Yaw sur le corps
        playerBody.Rotate(Vector3.up * md.x * mouseSensitivity);

        // Pitch sur la caméra
        pitch -= md.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        transform.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // Gestion du curseur
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
