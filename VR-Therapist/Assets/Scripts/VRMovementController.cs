using UnityEngine;
using UnityEngine.InputSystem;

public class VRMovementController : MonoBehaviour
{
    // Inspector'da Ayarlanacaklar:
    public float movementSpeed = 3f;
    public CharacterController characterController; 
    
    // Özel: Artık Inspector'dan atanmayacak. Kod kendisi bulacak.
    private InputAction moveAction;
    private Transform headTransform;

    void Awake()
    {
        // 1. VRActions dosyasını Resources'dan yükle
        InputActionAsset actionAsset = Resources.Load<InputActionAsset>("VRActions");
        
        if (actionAsset != null)
        {
            // 2. Movement Map'ini ve MoveAction'ı bul
            moveAction = actionAsset.FindActionMap("Movement").FindAction("MoveAction");
        }
        
        if (moveAction == null)
        {
            Debug.LogError("VRActions/Movement/MoveAction bulunamadı! Resources klasörünü ve dosya adını kontrol et.");
            enabled = false;
            return;
        }

        // 3. CharacterController'ı ve Başlık Transform'unu al
        characterController = GetComponent<CharacterController>();
        headTransform = transform.GetComponentInChildren<Camera>()?.transform ?? transform;

        if (characterController == null)
        {
            Debug.LogError("CharacterController bulunamadı.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        // 4. Action'ı etkinleştir
        if (moveAction != null) moveAction.Enable();
    }

    void OnDisable()
    {
        // Action'ı devre dışı bırak
        if (moveAction != null) moveAction.Disable();
    }

    void Update()
    {
        if (moveAction == null) return;

        // 5. Hareketi Uygula
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 direction = GetMovementDirection(moveInput);
        ApplyMovement(direction);
    }

    // --- Yönlendirme ve Hareket Metotları (Aynı Kalır) ---

    Vector3 GetMovementDirection(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude > 0.1f) 
        {
            Quaternion headYaw = Quaternion.Euler(0, headTransform.rotation.eulerAngles.y, 0);
            Vector3 forward = headYaw * Vector3.forward;
            Vector3 right = headYaw * Vector3.right;

            Vector3 desiredMove = forward * moveInput.y + right * moveInput.x;
            return desiredMove.normalized;
        }
        return Vector3.zero;
    }

    void ApplyMovement(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0f)
        {
            Vector3 horizontalMove = direction * movementSpeed * Time.deltaTime;
            characterController.Move(horizontalMove);
        }

        if (!characterController.isGrounded)
        {
             Vector3 gravity = Vector3.down * 9.81f * Time.deltaTime; 
             characterController.Move(gravity);
        }
    }
}