using UnityEngine;

public class ToolSmashTrigger : MonoBehaviour
{
    [Header("Kırma Impuls (Darbe) Eşiği")]
    // Kırmak için gereken minimum darbe kuvveti (Impulse).
    // Bu değer genellikle velocity'den farklıdır (Daha düşük bir değerle başlayın).
    public float minHitImpulse = 2.0f; 

    private bool isSmashed = false; // Kırılmayı engellemek için bayrak

    private void OnCollisionEnter(Collision collision)
    {
        // Bir kez kırıldıysa tekrar kırma
        if (isSmashed) return;
        
        UniversalSmash target = collision.gameObject.GetComponent<UniversalSmash>();

        if (target != null)
        {
            // 1. Çarpışma anındaki darbe kuvvetini hesapla (Impulse)
            float totalImpulse = 0f;
            
            // Tüm temas noktalarındaki kuvveti topla
            foreach (ContactPoint contact in collision.contacts)
            {
                // Impuls = Göreceli Hız * (Çarpan objenin kütlesi / 1 + (Çarpan kütlesi / Vurulan kütlesi))
                // Daha basit ve güvenilir olan relativeVelocity.magnitude * Rigidbody.mass'ı kullanıyoruz.
                totalImpulse += collision.relativeVelocity.magnitude * target.GetComponent<Rigidbody>().mass; 
            }
            
            // Debug.Log("Toplam Çarpma Impuls'u: " + totalImpulse); // Konsolu kontrol edin!

            // 2. Darbe kuvvetini (Impulse) eşiğimizle karşılaştır
            if (totalImpulse > minHitImpulse) 
            {
                isSmashed = true; // Kırılmayı işaretle
                target.SmashIt();
            }
        }
    }
}