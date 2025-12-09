using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit; 

public class OpenXRLocomotion : MonoBehaviour
{
    // ⚙️ Inspector'dan Bağlanacak Değişkenler
    [Header("Input Ayarları")]
    // Buraya Sol El Joystick'in Vector2 eylemini bağlayın (örn: 'XRI LeftHand Interaction/Move')
    public InputActionProperty moveAction; 
    
    [Header("Hareket Ayarları")]
    public float speed = 2.0f; 
    public float gravity = -9.81f; 

    // 🕹️ Referanslar
    private CharacterController characterController; 
    private Transform headTransform;
    private Vector3 velocity; 

    void Start()
    {
        // Gerekli Bileşenleri ve Referansları Al
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            // Eğer CharacterController yoksa hata verip script'i kapat
            Debug.LogError("OpenXRLocomotion: CharacterController bu objede bulunamadı. Lütfen ekleyin.");
            enabled = false;
            return;
        }

        // Başın (Headset) Transform'unu bulma (Genellikle ana kamera)
        // Eğer sahnede bir XR Rig varsa, headTransform genelde ana kameradır.
        headTransform = FindObjectOfType<Camera>().transform; 
        if (headTransform == null)
        {
            Debug.LogError("OpenXRLocomotion: Sahnede aktif bir kamera (Headset) transformu bulunamadı.");
            enabled = false;
            return;
        }
        
        // Input Action'ı aktif hale getir (XR Interaction Toolkit kullanırken genellikle 
        // XRController bileşeni bunu yapar, ancak manuel kontrol için burada tutmakta fayda var)
        if (moveAction.action != null)
        {
            moveAction.action.Enable();
        }
    }

    void OnDisable()
    {
        // Script devreden çıkınca Input Action'ı kapat
        if (moveAction.action != null)
        {
            moveAction.action.Disable();
        }
    }

    void Update()
    {
        // Yer çekimini zeminde (Grounded) sıfırla
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        // 2. Joystick Verisini Oku (Vector2)
        Vector2 inputVector = Vector2.zero;
        if (moveAction.action != null)
        {
            inputVector = moveAction.action.ReadValue<Vector2>();
        }

        // 3. Başın Yönüne Göre Yürüme Vektörünü Hesapla
        
        // Başın Sadece Y rotasyonunu al (Başın eğimi hareketi etkilemesin)
        Quaternion headRotation = Quaternion.Euler(0, headTransform.rotation.eulerAngles.y, 0);

        // Hareket yönlerini baş rotasyonuna göre hesapla
        Vector3 forward = headRotation * Vector3.forward;
        Vector3 right = headRotation * Vector3.right;
        
        // Hareket vektörünü oluştur (Joystick Y -> İleri/Geri, Joystick X -> Sağ/Sol)
        Vector3 movementDirection = (forward * inputVector.y) + (right * inputVector.x);
        
        // Yatay hareket hızı
        Vector3 horizontalMovement = movementDirection * speed;

        // 4. Hareketi Uygula (Yatay ve Düşey)
        
        // Yer çekimi (düşey hareket)
        velocity.y += gravity * Time.deltaTime;
        
        // Toplam hareket vektörü
        Vector3 totalMovement = (horizontalMovement + velocity) * Time.deltaTime;

        // CharacterController ile hareketi uygula
        characterController.Move(totalMovement);
    }
}