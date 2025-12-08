using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; 
using System.Collections; 

public class CustomPCGrabController : MonoBehaviour
{
    // Inspector'da ayarlanacaklar
    public Image crosshairImage;    
    public Transform handPosition;  
    public float grabRange = 3f;    
    public LayerMask grabbableLayer; 
    public float throwForce = 15f; // BU ARTIK KULLANILMAYACAK VEYA HAFİF AŞAĞI İTME İÇİN KULLANILABİLİR.
    
    // Renkler
    public Color defaultColor = Color.red;
    public Color highlightColor = Color.green;

    private GameObject currentlyHeldObject = null;
    private Rigidbody heldRb;
    private Collider playerCollider; 

    void Start()
    {
        // Karakterin ana Collider'ını (CharacterController) hiyerarşiden bulur.
        playerCollider = transform.root.GetComponentInChildren<CharacterController>();

        if (playerCollider == null)
        {
            Debug.LogError("XR Rig üzerinde CharacterController bileşeni bulunamadı! Çarpışma kapatılamaz.");
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRaycasting();
        HandleGrabInput();
    }

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

    void HandleGrabInput()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentlyHeldObject != null)
            {
                ReleaseObject(); // Fırlatma işlemi
            }
            else
            {
                TryGrabObject(); 
            }
        }
    }

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

                // Tutma
                heldRb.isKinematic = true; 
                currentlyHeldObject.transform.SetParent(handPosition);
                currentlyHeldObject.transform.localPosition = Vector3.zero;
            }
        }
    }

    void ReleaseObject()
    {
        // 1. Objenin ebeveynliğini kaldır ve fiziği aç
        currentlyHeldObject.transform.SetParent(null);
        heldRb.isKinematic = false; 

        // -----------------------------------------------------------
        // BURASI DÜZELTİLDİ: SADECE DİKEY HAREKET İÇİN HIZ AYARI
        // -----------------------------------------------------------

        // Mevcut hızı al (Bu hız, fırlatmadan hemen önceki anlık hızıdır)
        Vector3 currentVelocity = heldRb.velocity;

        // X (ileri/geri) ve Z (yan) hız bileşenlerini sıfırla.
        // Y bileşeni (dikey hız/yerçekimi) olduğu gibi kalır, böylece hemen düşmeye başlar.
        heldRb.velocity = new Vector3(0f, currentVelocity.y, 0f);

        // Not: Eski AddForce satırını yorum satırı yaptım. Artık yatay itme kuvveti uygulanmayacak.
        // Vector3 throwDirection = transform.root.forward;
        // heldRb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);


        // 3. Çarpışma Çözümü (İçe girmeyi engelle)
        Collider heldCollider = currentlyHeldObject.GetComponent<Collider>();
        
        if (playerCollider != null && heldCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, heldCollider, true);
        }

        // 4. Referansları temizle ve çarpışmayı tekrar açmak için Coroutine başlat
        GameObject releasedObject = currentlyHeldObject;
        Collider releasedCollider = heldCollider;
        
        currentlyHeldObject = null;
        heldRb = null;

        if (releasedCollider != null)
        {
            StartCoroutine(ReEnableCollision(releasedObject, releasedCollider));
        }
    }

    IEnumerator ReEnableCollision(GameObject releasedObject, Collider releasedCollider)
    {
        // 0.5 saniye bekle
        yield return new WaitForSeconds(0.5f); 

        if (releasedObject != null && releasedCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, releasedCollider, false);
        }
    }
}