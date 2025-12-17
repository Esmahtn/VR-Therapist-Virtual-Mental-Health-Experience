using UnityEngine;

public class ForceTeleport : MonoBehaviour
{
    public Transform playerRig;       
    public Transform targetLocation;  
    public AudioSource odaMuzigi;     // Pembe odanın müziğini buraya bağla

    public void DoTeleport()
    {
        if (playerRig != null && targetLocation != null)
        {
            var controller = playerRig.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            playerRig.position = targetLocation.position;
            playerRig.rotation = targetLocation.rotation;

            if (controller != null) controller.enabled = true;

            // MÜZİK BURADA BAŞLIYOR
            if (odaMuzigi != null && !odaMuzigi.isPlaying)
            {
                odaMuzigi.Play();
            }
            
            Debug.Log("Işınlanma tamam, müzik başladı!");
        }
    }
}