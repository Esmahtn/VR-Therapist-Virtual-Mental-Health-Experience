using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; 
using UnityEngine.XR.Interaction.Toolkit.Utilities; 

public class RotationFixer : MonoBehaviour
{
    // GripPoint'in kendisi (Transform objesi) - Artık sadece referans için kalabilir
    public Transform gripPointTransform; 

    [Header("Manuel Rotasyon Ayarı")]
    // Sopanın ele alındığında almasını istediğiniz X, Y, Z açıları
    // Örneğin: X=20 (hafif ileri eğik), Y=180 (elinizin tersine dönük), Z=0
    public Vector3 fixedGrabRotation = new Vector3(20f, 180f, 0f); 

    // Kilitlenecek Rotasyon değeri
    private Quaternion fixedRotationValue; 
    
    // Obje tutuluyor mu?
    private bool isGrabbed = false;
    
    private XRGrabInteractable grabInteractable;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("RotationFixer: XRGrabInteractable bulunamadı!");
            enabled = false;
            return;
        }

        // Vector3 açısını Quaternion rotasyonuna çevir
        fixedRotationValue = Quaternion.Euler(fixedGrabRotation);
        
        // Tutuş ve Bırakma olaylarını dinle
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
    }

    void LateUpdate()
    {
        // Sadece objeyi tuttuğumuzda rotasyonu düzelt
        if (isGrabbed)
        {
            // Objenin yerel rotasyonunu (local rotation), Inspector'da ayarladığımız değere zorla
            // Bu, kontrolcü hareket etse bile rotasyonu kesin olarak kitler.
            transform.localRotation = fixedRotationValue;
        }
    }
}