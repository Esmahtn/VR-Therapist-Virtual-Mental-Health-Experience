using UnityEngine;

public class TherapistAnimationController : MonoBehaviour
{
    private Animator animator;
    private float talkDuration = 3.0f; // Konuşma süresi (Server cevabının uzunluğuna göre değişebilir)
    private bool isCurrentlyTalking = false;

    void Start()
    {
        animator = GetComponent<Animator>(); // Model üzerindeki Animator'ü bul.
    }

    void Update()
    {
        // Eğer şu anda konuşuyorsa, sayacı azalt.
        if (isCurrentlyTalking)
        {
            talkDuration -= Time.deltaTime;
            if (talkDuration <= 0)
            {
                StopTalking();
            }
        }
    }

    // Server'dan veya diyalog yöneticisinden çağrılacak metot.
    public void StartTalking()
    {
        if (animator != null && !isCurrentlyTalking)
        {
            animator.SetBool("IsTalking", true); // "IsTalking" parametresini True yap.
            isCurrentlyTalking = true;
            talkDuration = 3.0f; // VEYA gelen mesajın uzunluğuna göre ayarla
        }
    }

    // Konuşma süresi bitince otomatik çağrılacak metot.
    private void StopTalking()
    {
        if (animator != null)
        {
            animator.SetBool("IsTalking", false); // "IsTalking" parametresini False yap.
            isCurrentlyTalking = false;
        }
    }
}