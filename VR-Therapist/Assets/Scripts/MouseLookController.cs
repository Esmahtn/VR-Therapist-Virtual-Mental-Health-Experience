using UnityEngine;
using UnityEngine.InputSystem; // Yeni Input System'ı kullanmak için

public class MouseLookController : MonoBehaviour
{
    // Inspector'da ayarlanacak hassasiyet
    [Range(50f, 300f)]
    public float mouseSensitivity = 150f;

    private float rotationX = 0f;
    private Transform playerBody; // XR Origin (XR Rig) objesi

    void Start()
    {
        // XR Origin'i (oyuncunun ana gövdesini) al
        // Bu script'i Main Camera'ya ekleyeceğiz, XR Origin ise onun büyük ebeveynidir.
        // Hiyerarşi: XR Origin > Camera Offset > Main Camera
        playerBody = transform.root; 

        // Fare imlecini gizle ve kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Input System'dan fare girdisini oku
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        // 1. Yukarı/Aşağı Bakış (Kamera Rotasyonu)
        // Y rotasyonunu hesapla (Yukarı/aşağı bakış)
        rotationX -= mouseY;
        // Açıyı -90 ile 90 derece arasında sıkıştır (Başın arkaya eğilmesini engelle)
        rotationX = Mathf.Clamp(rotationX, -90f, 90f); 

        // Kameranın yukarı/aşağı rotasyonunu uygula
        transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        // 2. Sağa/Sola Dönüş (XR Origin Rotasyonu)
        // Ana gövdeyi (XR Origin) Y ekseninde (sağa/sola) döndür
        playerBody.Rotate(Vector3.up * mouseX);
    }
}