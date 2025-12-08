using UnityEngine;

public class BreakableOnImpact : MonoBehaviour
{
    // Inspector'da kırık tek parça modelinizi sürükleyip bırakın
    public GameObject brokenObject; 
    
    // Kırılmanın gerçekleşeceği zemin katmanını Inspector'da belirleyin (Örn: "Floor" veya "Ground")
    public LayerMask groundLayer; 

    private bool isBroken = false;

    void Start()
    {
        // Kırık objeyi başlangıçta devre dışı bırak
        if (brokenObject != null)
        {
            brokenObject.SetActive(false);
        }
    }

    // Bir çarpışma başladığında Unity bu metodu çağırır.
    private void OnCollisionEnter(Collision collision)
    {
        // 1. Obje daha önce kırılmadıysa kontrol et
        if (isBroken) return;

        // 2. Çarptığımız objenin Zemin olup olmadığını kontrol et
        // (Bu, havada başka bir şeye çarpınca kırılmasını önler)
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            BreakObject();
        }
        
        // EĞER SADECE DÜŞME HIZI İLE KIRILMASINI İSTİYORSANIZ:
        // float impactVelocity = collision.relativeVelocity.magnitude;
        // float breakThreshold = 3.0f; // Ayarlanabilir kuvvet eşiği
        // if (impactVelocity > breakThreshold) BreakObject();
    }

    void BreakObject()
    {
        isBroken = true;
        
        // 1. Sağlam objeyi gizle (Çarpışmanın hemen ardından)
        gameObject.SetActive(false); 

        // 2. Kırık objeyi göster
        if (brokenObject != null)
        {
            // Kırık objeyi sağlam objenin son pozisyonunda aktif et
            brokenObject.transform.position = transform.position;
            brokenObject.transform.rotation = transform.rotation;
            brokenObject.SetActive(true); 
            
            // Kırık objenin fiziğini serbest bırak (yerçekimiyle düşmeye devam etsin)
            Rigidbody brokenRb = brokenObject.GetComponent<Rigidbody>();
            if (brokenRb != null)
            {
                brokenRb.isKinematic = false; // Yerçekimi devreye girsin
                // Düşme etkisini daha iyi göstermek için küçük bir sarsıntı kuvveti ekleyebilirsiniz.
                brokenRb.AddForce(Vector3.down * 1f, ForceMode.Impulse); 
            }
        }
        
        // Script'i devre dışı bırak
        enabled = false;
    }
}