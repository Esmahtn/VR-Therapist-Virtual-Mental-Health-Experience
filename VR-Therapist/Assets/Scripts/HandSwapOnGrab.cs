using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandSwapOnGrab : MonoBehaviour
{
    [Tooltip("Çıplak sol el modelinin GameObject'i")]
    public GameObject bareHandModel; // Çıplak el objesini buraya bağlayacağız

    private XRGrabInteractable grabInteractable;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    // Sopa tutulduğunda
    private void OnGrab(SelectEnterEventArgs args)
    {
        if (bareHandModel != null)
        {
            bareHandModel.SetActive(false); // Çıplak eli gizle
        }
    }

    // Sopa bırakıldığında
    private void OnRelease(SelectExitEventArgs args)
    {
        if (bareHandModel != null)
        {
            bareHandModel.SetActive(true); // Çıplak eli göster
        }
    }
}