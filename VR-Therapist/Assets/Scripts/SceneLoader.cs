using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Yüklenecek hedef sahnenin adı
    public string targetSceneName = "TaskRoom"; 

    // Start() metodu oyun başladığında bir kez çalışır.
    void Start() 
    {
        // Sahnenin hemen yüklenmesi için 2 saniye bekleyelim.
        Invoke("StartSceneTransition", 2f); 
    }

    public void StartSceneTransition()
    {
        Debug.Log("Sahne Geçişi Kod İle Başlatıldı.");
        SceneManager.LoadScene(targetSceneName);
    }
    
    // NOT: Butonunuzun bu kodu tetiklemesi için StartSceneTransition() fonksiyonu gereklidir.
}