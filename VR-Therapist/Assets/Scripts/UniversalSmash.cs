using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections; // Gerekli değil ama hata ayıklama için bırakılabilir

public class UniversalSmash : MonoBehaviour
{
    [Header("Kırık Obje")]
    public GameObject brokenSibling;

    [Header("Kırılma Eşikleri")]
    public float requiredImpulse = 30f;      // SOPA (deneyerek artır)
    public float requiredThrowSpeed = 2.2f;  // ELLE FIRLATMA

    [Header("Stress Etkisi")]
    public float stressDamage = 10f;

    [Header("Kırıcı Nesne")]
    public string breakerTag = "Stick";

    private bool isSmashed = false;
    private bool wasReleased = false;

    private Rigidbody solidRb;
    private Rigidbody brokenRb;
    private XRGrabInteractable grab;
    
    // YENİ EKLENTİ: AudioPlayer'a referans
    private AudioPlayer audioPlayer; // Referans tutulmaya devam edilecek

    void Start()
    {
        solidRb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        if (brokenSibling != null)
        {
            brokenRb = brokenSibling.GetComponent<Rigidbody>();
            brokenSibling.SetActive(false);
            
            // 🟡 KRİTİK GÜNCELLEME: AudioPlayer'ı KIRIK objeden alıyoruz.
            audioPlayer = brokenSibling.GetComponent<AudioPlayer>();
            if (audioPlayer == null)
            {
                Debug.LogError("Ses çalmak için AudioPlayer script'i KIRIK NESNEDE eksik! Lütfen AudioPlayer ve AudioSource'u brokenSibling'e ekleyin.", this);
            }
        }

        if (grab != null)
            grab.selectExited.AddListener(OnReleased);

        // Eski audioPlayer bulma mantığı artık yukarıdaki if bloğunda kırık obje kontrolü ile birleşti ve kaldırıldı.
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
            // Impulse değeri konsolda görünüyorsa, çarpışma algılanıyor demektir!
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
        
        // Bu noktada kırılma tetikleniyor.

        // 🔴 STRESS MANAGER'A HABER VER
        if (StressManager.Instance != null)
        {
            StressManager.Instance.ReduceStress(stressDamage);
        }

        // Kırılma Fizik Ayarları
        solidRb.isKinematic = false;
        solidRb.useGravity = true;

        // Kırık Modeli Aktifleştirme
        brokenSibling.transform.position = transform.position;
        brokenSibling.transform.rotation = transform.rotation;
        brokenSibling.SetActive(true);

        if (brokenRb != null)
        {
            brokenRb.isKinematic = false;
            brokenRb.useGravity = true;
            brokenRb.velocity = solidRb.velocity;
            brokenRb.angularVelocity = solidRb.angularVelocity;
        }

        // 1. ANINDA KAPAT: Sağlam nesneyi hemen sahneden kaldır.
        // Bu komut artık güvenli çünkü AudioSource ve AudioPlayer KIRIK objede!
        gameObject.SetActive(false); 

        // 2. SADECE SESİ ÇAL: Kırık objeden alınan AudioPlayer'ı çağırıp sesi başlat.
        if (audioPlayer != null)
        {
            audioPlayer.PlaySoundOnly(); 
        }
    }
}