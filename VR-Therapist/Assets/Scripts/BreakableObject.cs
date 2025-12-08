using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    public GameObject solidObject;
    public GameObject brokenObject;

    public float breakForce = 1.5f;  // kırılması için gereken hız

    private bool isBroken = false;
    private Rigidbody rb;

    void Start()
    {
        rb = solidObject.GetComponent<Rigidbody>();
        brokenObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isBroken) return;

        // ❗ SADECE ZEMİNE ÇARPINCA KIRILSIN
        if (!collision.collider.CompareTag("Ground"))
            return;

        // ❗ HIZ BELİRLİ BİR EŞİĞİN ÜZERİNDEYSE KIRILSIN
        if (rb.velocity.magnitude >= breakForce)
        {
            Break();
        }
    }

    void Break()
    {
        isBroken = true;

        brokenObject.transform.position = solidObject.transform.position;
        brokenObject.transform.rotation = solidObject.transform.rotation;

        brokenObject.SetActive(true);
        solidObject.SetActive(false);

        Debug.Log("Yere çarpınca kırıldı!");
    }
}
