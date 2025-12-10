using UnityEngine;
using System.Collections; 

public class UniversalSmash : MonoBehaviour
{
    // SAĞLAM BİBLO ÜZERİNDE

    [Header("Hiyerarşi Bağlantısı")]
    // Kırık olan (gizli) alt objeyi buraya sürükleyin. (Tek Rigidbody olmalı)
    public GameObject brokenSibling; 
    
    [Header("Fizik Ayarları")]
    // Elle fırlatmada kırılması için gereken minimum hız
    public float smashVelocityThreshold = 20.0f; 

    private bool isSmashed = false;
    private Rigidbody solidRb;
    private Rigidbody brokenRb; // Kırık objenin Rigidbody'si

    void Start()
    {
        solidRb = GetComponent<Rigidbody>(); 
        
        // Kırık Sibling'in Rigidbody'sini al
        if (brokenSibling != null)
        {
            brokenRb = brokenSibling.GetComponent<Rigidbody>();
        }

        // Başlangıçta kırık objenin gizli olduğundan emin ol
        if (brokenSibling != null && brokenSibling.activeSelf)
        {
             brokenSibling.SetActive(false);
        }

        if (solidRb == null) 
        {
            Debug.LogError(gameObject.name + ": Rigidbody zorunludur!");
            enabled = false;
        }
    }

    // El ile fırlatıldığında tetiklenir (Hız kontrolü)
    private void OnCollisionEnter(Collision collision)
    {
        if (isSmashed) return;
        
        float collisionForce = collision.relativeVelocity.magnitude;
        
        if (collisionForce > smashVelocityThreshold)
        {
            // Debug.Log("Fırlatma/Yere Çarpma Algılandı ve Kırılma Başlatıldı.");
            SmashIt(); 
        }
    }

    // Kırma ve Geçiş İşlemini Yapar (Hem el fırlatması hem de Sopa tarafından çağrılır)
    public void SmashIt()
    {
        if (isSmashed) return;
        isSmashed = true;
        
        // 1. Sağlam objeden hızı al
        Vector3 initialVelocity = solidRb.velocity;
        Vector3 initialAngularVelocity = solidRb.angularVelocity;

        // 2. Kırık Sibling'i görünür yap
        if (brokenSibling != null)
        {
            // Konum senkronizasyonu
            brokenSibling.transform.position = transform.position; 
            brokenSibling.transform.rotation = transform.rotation; 
            
            brokenSibling.SetActive(true); 
        }
        
        // 3. Kırık objeye hızı aktar
        if (brokenRb != null)
        {
             brokenRb.isKinematic = false; 
             brokenRb.velocity = initialVelocity; 
             brokenRb.angularVelocity = initialAngularVelocity;
        }
        
        // 4. Sağlam objeyi gizle
        gameObject.SetActive(false);
    }
}