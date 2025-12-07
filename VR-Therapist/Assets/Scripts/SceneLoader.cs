using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Inspector'da belirleyeceğimiz hedef sahne adı
    public string targetSceneName = "TaskRoom"; 

    // Bu, kapı/geçit objesine eklenecek bir kod olmalı.
    // Kapı objesinin bir Collider'ı ve isTrigger özelliği açık olmalıdır.
    private void OnTriggerEnter(Collider other)
    {
        // Sadece Player tag'ine sahip bir objeyle çarpışıyorsa geçişi yap.
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}