using UnityEngine;

public class BreakableTarget : MonoBehaviour
{
    [Header("Kırık Hal")]
    public GameObject brokenPrefab; // Kırık modelin prefab'ını buraya bağlayın

    private bool isBroken = false;

    // Sopa tarafından çağrılacak kırma metodu
    public void Smash()
    {
        if (isBroken) return;
        isBroken = true;

        // Kırık objeyi oluştur
        Instantiate(brokenPrefab, transform.position, transform.rotation);

        // Sağlam objeyi yok et
        Destroy(gameObject);
    }
}