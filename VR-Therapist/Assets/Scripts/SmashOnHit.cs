using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; 

public class SmashOnHit : MonoBehaviour
{
    // Inspector'da kırık tek parça modelinizi sürükleyip bırakın
    public GameObject shatteredPrefab; 
    public LayerMask floorLayer; 

    private Rigidbody objectRb;
    private bool isSmashed = false;

    void Start()
    {
        objectRb = GetComponent<Rigidbody>();
        // Kırık objeyi başlangıçta gizle
        if (shatteredPrefab != null)
        {
            shatteredPrefab.SetActive(false);
        }
    }

    // Fiziksel çarpışma anında çağrılır
    private void OnCollisionEnter(Collision collision)
    {
        if (isSmashed) return;
        
        // 1. Çarpma hızını hesapla
        float collisionForce = collision.relativeVelocity.magnitude;
        float smashVelocityThreshold = 3.5f; // Fırlatma sertliği eşiği

        // 2. Yeterli hızla çarptıysa kırılmayı tetikle
        if (collisionForce > smashVelocityThreshold)
        {
            SmashObject();
        }
    }

    void SmashObject()
    {
        isSmashed = true;
        
        // 1. Sağlam objeyi gizle
        gameObject.SetActive(false); 

        // 2. Kırık objeyi göster ve fiziğini serbest bırak
        if (shatteredPrefab != null)
        {
            // Kırık objeyi sağlam objenin son pozisyonunda aktif et
            shatteredPrefab.transform.position = transform.position;
            shatteredPrefab.transform.rotation = transform.rotation;
            shatteredPrefab.SetActive(true); 
            
            Rigidbody shatteredRb = shatteredPrefab.GetComponent<Rigidbody>();
            if (shatteredRb != null)
            {
                shatteredRb.isKinematic = false; // Parçaların düşmesini sağlar
            }
        }
        
        // Script'i devre dışı bırak
        enabled = false;
    }
}