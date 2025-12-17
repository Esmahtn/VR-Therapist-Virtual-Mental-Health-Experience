using UnityEngine;

public class MenuKontrol : MonoBehaviour
{
    public GameObject anaMenuObjesi;

    public void OyunuBaslat()
    {
        anaMenuObjesi.SetActive(false);
        Debug.Log("Oyun Başladı!");
    }

    public void OyundanCik()
    {
        Debug.Log("Çıkış butonuna basıldı!");

        // 1. Eğer Unity içindeysek (Editor) çalışmayı durdur
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 2. Eğer gerçek oyundaysak (Build) uygulamayı kapat
            Application.Quit();
        #endif
    }
}