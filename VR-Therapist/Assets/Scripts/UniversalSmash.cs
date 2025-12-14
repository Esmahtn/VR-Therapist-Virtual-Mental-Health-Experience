using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class UniversalSmash : MonoBehaviour
{
    [Header("Kırık Obje")]
    public GameObject brokenSibling;

    [Header("Kırılma Eşikleri")]
    public float requiredImpulse = 30f;      
    public float requiredThrowSpeed = 2.2f;  

    [Header("Stress Etkisi")]
    public float stressDamage = 10f; // Stres yöneticisine gönderilecek değer

    [Header("Kırıcı Nesne")]
    public string breakerTag = "Stick";

    private bool isSmashed = false;
    private bool wasReleased = false;

    private Rigidbody solidRb;
    private Rigidbody brokenRb;
    private XRGrabInteractable grab;
    
    private AudioPlayer audioPlayer; 
    
    // 💥 YENİ EKLENTİ: Parçacık Sistemi referansı
    private ParticleSystem smashParticles;

    void Start()
    {
        solidRb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        if (brokenSibling != null)
        {
            brokenRb = brokenSibling.GetComponent<Rigidbody>();
            brokenSibling.SetActive(false);
            
            // AudioPlayer'ı kırık objeden alma
            audioPlayer = brokenSibling.GetComponent<AudioPlayer>();
            if (audioPlayer == null)
            {
                Debug.LogError("Ses çalmak için AudioPlayer script'i KIRIK NESNEDE eksik!", this);
            }
            
            // 💥 YENİ EKLENTİ: ParticleSystem'i kırık objeden alma
            smashParticles = brokenSibling.GetComponent<ParticleSystem>();
            if (smashParticles == null)
            {
                Debug.LogError("Görsel efekt için Particle System script'i KIRIK NESNEDE eksik! (brokenSibling'e eklenmeli)", this);
            }
        }

        if (grab != null)
            grab.selectExited.AddListener(OnReleased);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        wasReleased = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isSmashed) return;

        // 🟡 SOPA / ALET ÇARPMASI
        if (collision.collider.CompareTag(breakerTag))
        {
            float impulse = collision.impulse.magnitude;
            Debug.Log($"[IMPULSE] {impulse}");

            if (impulse >= requiredImpulse)
                Smash();
        }
        // 🟢 ELLE FIRLATMA
        else if (wasReleased)
        {
            float speed = solidRb.velocity.magnitude;
            Debug.Log($"[FIRLATMA HIZI] {speed}");

            if (speed >= requiredThrowSpeed)
                Smash();

            wasReleased = false;
        }
    }

    void Smash()
    {
        if (isSmashed) return;
        isSmashed = true;
        
        // 🔴 STRESS MANAGER'A HABER VER (Mevcut kodunuzdaki çağrı)
        if (StressManager.Instance != null)
        {
            StressManager.Instance.ReduceStress(stressDamage); 
        }

        // -----------------------------------------------------------------
        // ÖN-TETİKLEME: Kırık obje aktive edilmeli ve görsel/ses tetiklenmeli
        // -----------------------------------------------------------------
        
        // Kırık Modeli Aktifleştirme (Konum ve rotasyon sağlam nesneden alınır)
        brokenSibling.transform.position = transform.position;
        brokenSibling.transform.rotation = transform.rotation;
        brokenSibling.SetActive(true);

        // Sesi Çal
        if (audioPlayer != null)
        {
            audioPlayer.PlaySoundOnly(); 
        }

        // 💥 YENİ EKLENTİ: Parçacığı Tetikle
        if (smashParticles != null)
        {
            smashParticles.Play(); 
        }
        
        // -----------------------------------------------------------------
        // FİZİK VE SON TEMİZLİK
        // -----------------------------------------------------------------
        
        // Kırılma Fizik Ayarları
        solidRb.isKinematic = false;
        solidRb.useGravity = true;

        if (brokenRb != null)
        {
            brokenRb.isKinematic = false;
            brokenRb.useGravity = true;
            // Kırılma hızını sağlam nesneden aktar
            brokenRb.velocity = solidRb.velocity;
            brokenRb.angularVelocity = solidRb.angularVelocity;
        }

        // Sağlam nesneyi sahneden kaldır.
        gameObject.SetActive(false); 
    }
}