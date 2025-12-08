using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class MouseLookController : MonoBehaviour
{
    [Range(50f, 300f)]
    public float mouseSensitivity = 150f;

    private float rotationX = 0f;
    private Transform playerBody;

    void Start()
    {
        playerBody = transform.root;

        // Eğer VR aktifse mouse look çalışmasın
        if (XRSettings.isDeviceActive)
        {
            this.enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (XRSettings.isDeviceActive)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        playerBody.Rotate(Vector3.up * mouseX);
    }
}
