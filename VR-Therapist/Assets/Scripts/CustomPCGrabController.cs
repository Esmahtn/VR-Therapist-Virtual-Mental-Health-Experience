using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; 
using System.Collections; 
using UnityEngine.XR; // VR Input için gerekli

public class CustomPCGrabController : MonoBehaviour
{
    // Inspector'da ayarlanacaklar
    public Image crosshairImage;    
    public Transform handPosition;  
    public float grabRange = 3f;    
    public LayerMask grabbableLayer; 
    public float throwForce = 15f; 
    
    // Renkler
    public Color defaultColor = Color.red;
    public Color highlightColor = Color.green;

    private GameObject currentlyHeldObject = null;
    private Rigidbody heldRb;
    private Collider playerCollider; 
    
    // VR Kontrolcüsünü tutacak değişken (Tam adı kullanıldı: UnityEngine.XR.InputDevice)
    private UnityEngine.XR.InputDevice leftHandDevice; 

    void Start()
    {
        playerCollider = transform.root.GetComponentInChildren<CharacterController>();

        if (playerCollider == null)
        {
            Debug.LogError("XR Rig üzerinde CharacterController bileşeni bulunamadı! Çarpışma kapatılamaz.");
        }
        
        // --- GRAB GİRİŞİ BAĞLANTISI ---
        
        // 1. Sol VR Kontrolcüsünü Bulmaya Çalış
        var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>(); 
        
        // Controller ve Left özelliklerini taşıyan cihazları al
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, 
            devices
        );

        if (devices.Count > 0)
        {
            leftHandDevice = devices[0];
            Debug.Log("VR Sol Kontrolcü bulundu: " + leftHandDevice.name);
        }
        else
        {
            // Eğer Controller|Left bulamazsa, TrackedDevice|Left kombinasyonunu dene
             UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Left, 
                devices
            );
            
            if (devices.Count > 0)
            {
                leftHandDevice = devices[0];
                Debug.Log("VR Sol Kontrolcü bulundu (TrackedDevice): " + leftHandDevice.name);
            }
            else
            {
                Debug.Log("VR kontrolcü bulunamadı. PC 'E' tuşu yakalama için kullanılacak.");
            }
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRaycasting();
        HandleGrabInput();
    }
    
    // --- IŞIN YAYINLAMA ---
    void HandleRaycasting()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, grabRange, grabbableLayer))
        {
            if (hit.collider.GetComponent<Rigidbody>() != null)
            {
                crosshairImage.color = highlightColor;
            }
        }
        else
        {
            crosshairImage.color = defaultColor;
        }
    }

    // --- GRİP GİRİŞİ YÖNETİMİ ---
    void HandleGrabInput()
    {
        bool grabInputReceived = false;

        // A) VR Kontrolcü Girişi Kontrolü
        if (leftHandDevice.isValid)
        {
            bool buttonPressed;
            
            // Grip tuşunun anlık durumunu sorgula
            if (leftHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out buttonPressed) && buttonPressed)
            {
                grabInputReceived = buttonPressed; 
            }
        }

        // B) PC Girişi Kontrolü (E Tuşu)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            grabInputReceived = true;
        }

        if (grabInputReceived)
        {
            if (currentlyHeldObject != null)
            {
                ReleaseObject(); 
            }
            else
            {
                TryGrabObject(); 
            }
        }
    }
    
    // --- OBJEYİ TUTMA ---
    void TryGrabObject()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, grabRange, grabbableLayer))
        {
            Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();

            if (targetRb != null)
            {
                currentlyHeldObject = hit.collider.gameObject;
                heldRb = targetRb;

                heldRb.isKinematic = true; 
                currentlyHeldObject.transform.SetParent(handPosition);
                currentlyHeldObject.transform.localPosition = Vector3.zero;
            }
        }
    }

    // --- OBJEYİ BIRAKMA/FIRLATMA (Dikey Düşme) ---
    void ReleaseObject()
    {
        currentlyHeldObject.transform.SetParent(null);
        heldRb.isKinematic = false; 

        // SADECE DİKEY HAREKET İÇİN HIZ AYARI
        Vector3 currentVelocity = heldRb.velocity;
        heldRb.velocity = new Vector3(0f, currentVelocity.y, 0f); 

        // Çarpışma Çözümü (İçe girmeyi engelle)
        Collider heldCollider = currentlyHeldObject.GetComponent<Collider>();
        
        if (playerCollider != null && heldCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, heldCollider, true);
        }

        GameObject releasedObject = currentlyHeldObject;
        Collider releasedCollider = heldCollider;
        
        currentlyHeldObject = null;
        heldRb = null;

        if (releasedCollider != null)
        {
            StartCoroutine(ReEnableCollision(releasedObject, releasedCollider));
        }
    }

    // --- ÇARPIŞMAYI YENİDEN AKTİF ETMEK İÇİN COROUTINE ---
    IEnumerator ReEnableCollision(GameObject releasedObject, Collider releasedCollider)
    {
        yield return new WaitForSeconds(0.5f); 

        if (releasedObject != null && releasedCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, releasedCollider, false);
        }
    }
}