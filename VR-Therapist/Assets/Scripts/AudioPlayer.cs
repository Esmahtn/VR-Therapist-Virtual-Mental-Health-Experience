// AudioPlayer.cs
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    // kirilmaSesi değişkenini private'dan public'e çekebiliriz (Unity Editor'da kolay kontrol için)
    // VEYA Start'ı kaldırdığımız için private bırakabiliriz.
    private AudioSource kirilmaSesi; 
    
    // Start() metodunu buradan tamamen SİLİN.

    public void PlaySoundOnly()
    {
        // HER SEFERİNDE AudioSource'u KONTROL ET!
        if (kirilmaSesi == null)
        {
            kirilmaSesi = GetComponent<AudioSource>();
        }

        if (kirilmaSesi != null && kirilmaSesi.clip != null)
        {
            kirilmaSesi.Play();
            Debug.Log("[AudioPlayer] Kırılma sesi başarıyla çalındı.");
        }
        else
        {
            Debug.LogError("[AudioPlayer] HATA: AudioSource'a ses atanmamış veya bulunamadı (Tekrar kontrol et).", this);
        }
    }
}