using UnityEngine;

public class SmashOnHit : MonoBehaviour
{
    // Inspector'da kırık tek parça modelinizi sürükleyip bırakın
    public GameObject shatteredPrefab; 
    
    // Kırılma eşiği: Fırlatmanın hızını algılaması için makul bir eşik.
    public float smashVelocityThreshold = 3.5f; 

    private bool isSmashed = false;
    private Rigidbody objectRb;

    void Start()
    {
        objectRb = GetComponent<Rigidbody>();
        if (shatteredPrefab != null)
        {
            shatteredPrefab.SetActive(false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isSmashed) return;
        
        float collisionForce = collision.relativeVelocity.magnitude;
        
        // Fırlatmanın kuvveti eşiği geçerse kırılmayı tetikle
        if (collisionForce > smashVelocityThreshold)
        {
            SmashObject();
        }
    }

    void SmashObject()
    {
        isSmashed = true;
        
        // Sağlam objeyi gizle
        gameObject.SetActive(false); 

        // Kırık objeyi göster ve fiziğini serbest bırak
        if (shatteredPrefab != null)
        {
            shatteredPrefab.transform.position = transform.position;
            shatteredPrefab.transform.rotation = transform.rotation;
            shatteredPrefab.SetActive(true); 
            
            Rigidbody shatteredRb = shatteredPrefab.GetComponent<Rigidbody>();
            if (shatteredRb != null)
            {
                shatteredRb.isKinematic = false; 
                shatteredRb.velocity = objectRb.velocity; 
                shatteredRb.angularVelocity = objectRb.angularVelocity;
            }
        }
        
        enabled = false;
    }
}