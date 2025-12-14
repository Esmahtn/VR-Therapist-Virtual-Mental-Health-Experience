using UnityEngine;

public class QuitGame : MonoBehaviour
{
    // Bu metot, butona tıklandığında veya başka bir olayla çağrılır.
    public void QuitApplication()
    {
        // Editörde çalışırken bu komut işe yaramaz, bu yüzden Debug.Log ekledik.
        Debug.Log("Uygulamadan Çıkış İsteği Alındı. (Sadece build edilmiş oyunda çalışır)");

        #if UNITY_EDITOR
            // Unity Editör'ünde oyunu durdurmak için.
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Build edilmiş oyunda uygulamayı kapatır.
            Application.Quit();
        #endif
    }
}