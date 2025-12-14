using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class UniversalSmash : MonoBehaviour
{
    [Header("Kırık Obje")]
    public GameObject brokenSibling;

    [Header("Kırılma Eşikleri")]
    public float requiredImpulse = 30f;      // SOPA (deneyerek artır)
    public float requiredThrowSpeed = 2.2f;   // ELLE FIRLATMA

    [Header("Stress Etkisi")]
    public float stressDamage = 10f;

    [Header("Kırıcı Nesne")]
    public string breakerTag = "Stick";

    private bool isSmashed = false;
    private bool wasReleased = false;

    private Rigidbody solidRb;
    private Rigidbody brokenRb;
    private XRGrabInteractable grab;

    void Start()
    {
        solidRb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        if (brokenSibling != null)
        {
            brokenRb = brokenSibling.GetComponent<Rigidbody>();
            brokenSibling.SetActive(false);
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

        // 🔴 STRESS MANAGER'A HABER VER
        if (StressManager.Instance != null)
        {
            StressManager.Instance.ReduceStress(stressDamage);
        }

        solidRb.isKinematic = false;
        solidRb.useGravity = true;

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

        gameObject.SetActive(false);
    }

}
