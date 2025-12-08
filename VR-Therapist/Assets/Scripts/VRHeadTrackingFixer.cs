using UnityEngine;
using UnityEngine.XR; // XRInput kullanmak için gerekli

public class VRHeadTrackingFixer : MonoBehaviour
{
    // VR başlığından gelen verileri alacak değişken
    private InputDevice headDevice;

    void Start()
    {
        InitializeHeadDevice();
    }

    void InitializeHeadDevice()
    {
        var inputDevices = new System.Collections.Generic.List<InputDevice>();
        // Başlık cihazını (HMD) bulmaya çalış
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice,
            inputDevices
        );

        if (inputDevices.Count > 0)
        {
            headDevice = inputDevices[0]; 
            Debug.Log("VR Başlık Cihazı (HMD) bulundu: " + headDevice.name);
        }
        else
        {
            Debug.LogWarning("VR Başlık Cihazı (HMD) bulunamadı. Lütfen cihazın bağlı olduğundan emin olun.");
            enabled = false; 
        }
    }

    void Update()
    {
        if (headDevice.isValid)
        {
            // HATA DÜZELTME BURADA YAPILDI: rotation yerine CommonUsages.deviceRotation kullanıldı.
            if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation))
            {
                // Objenin (XR Rig'in) Y eksenindeki dönüşünü, başlığın Y eksenindeki dönüşü ile eşitle.
                // Bu, yatayda bakış açısını VR başlığına bağlar.
                
                float yAngle = headRotation.eulerAngles.y;
                
                // Bu scriptin bağlı olduğu objenin (XR Rig'in) Y eksenini döndür.
                transform.localRotation = Quaternion.Euler(0f, yAngle, 0f);
            }
        }
    }
}