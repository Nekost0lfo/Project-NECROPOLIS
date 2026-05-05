using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 25f;
    public Transform playerCamera;

    [Header("Head Bobbing")]
    public bool enableHeadBobbing = true;
    public float bobFrequency = 10f;
    public float bobVerticalAmplitude = 0.05f;
    public float bobHorizontalAmplitude = 0.03f;

    [Header("Interaction")]
    public float grabRange = 5f;               // дистанция обнаружения объекта
    public float holdDistance = 2f;            // расстояние от камеры до удерживаемого объекта
    public LayerMask grabLayerMask = -1;       // слои, с которыми взаимодействуем

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float verticalVelocity;

    private Vector3 cameraInitialPosition;
    private float bobTimer;
    private bool isMoving;

    private InputSystem_Actions inputActions;

    // ---------- Поля для захвата объектов ----------
    private InputAction interactAction;        // отдельный InputAction для E
    private bool isHolding = false;
    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private Transform holdPoint;               // точка, в которой удерживается предмет

    void Awake()
    {
        inputActions = new InputSystem_Actions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => Jump();

        // Создаём отдельный InputAction для клавиши E
        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        interactAction.performed += _ => StartHolding();
        interactAction.canceled += _ => StopHolding();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        if (playerCamera != null)
        {
            cameraInitialPosition = playerCamera.localPosition;

            // Создаём пустую точку удержания, привязанную к камере
            GameObject point = new GameObject("HoldPoint");
            point.transform.SetParent(playerCamera);
            point.transform.localPosition = new Vector3(0f, 0f, holdDistance);
            point.transform.localRotation = Quaternion.identity;
            holdPoint = point.transform;
        }
    }

    void OnEnable()
    {
        inputActions.Enable();
        interactAction.Enable();   // включаем E вместе с остальными
    }

    void OnDisable()
    {
        inputActions.Disable();
        interactAction.Disable();
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        ApplyGravity();
        if (enableHeadBobbing) HandleHeadBobbing();
        HandleHeldObject();       // перемещаем удерживаемый объект (если есть)
    }

    void HandleMovement()
    {
        Vector3 horizontalMove = transform.right * moveInput.x + transform.forward * moveInput.y;
        horizontalMove *= speed;

        isMoving = moveInput.magnitude > 0.1f && controller.isGrounded;

        Vector3 move = horizontalMove + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
    }

    void Jump()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleHeadBobbing()
    {
        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;

            float verticalOffset = Mathf.Sin(bobTimer) * bobVerticalAmplitude;
            float horizontalOffset = Mathf.Cos(bobTimer) * bobHorizontalAmplitude;

            Vector3 newPos = cameraInitialPosition;
            newPos.y += verticalOffset;
            newPos.x += horizontalOffset;

            playerCamera.localPosition = newPos;
        }
        else
        {
            bobTimer = 0f;
            playerCamera.localPosition = Vector3.Lerp(
                playerCamera.localPosition,
                cameraInitialPosition,
                Time.deltaTime * bobFrequency
            );
        }
    }

    // ---------- Логика захвата ----------

    /// <summary> Вызывается при нажатии E (performed) </summary>
    void StartHolding()
    {
        // Если уже что-то держим — не пытаемся схватить новое
        if (isHolding) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabLayerMask))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            // Игнорируем кинематические тела (их двигать через физику не получится)
            if (rb != null && !rb.isKinematic)
            {
                isHolding = true;
                heldObject = hit.collider.gameObject;
                heldRigidbody = rb;

                // Настраиваем физику для плавного удержания
                heldRigidbody.useGravity = false;
                heldRigidbody.freezeRotation = true;
                heldRigidbody.linearDamping = 15f;   // гасим инерцию
            }
        }
    }

    /// <summary> Вызывается при отпускании E (canceled) </summary>
    void StopHolding()
    {
        if (!isHolding) return;

        // Возвращаем стандартные настройки физики
        heldRigidbody.useGravity = true;
        heldRigidbody.freezeRotation = false;
        heldRigidbody.linearDamping = 0f;

        heldObject = null;
        heldRigidbody = null;
        isHolding = false;
    }

    /// <summary> Каждый кадр перемещаем удерживаемый объект к точке holdPoint </summary>
    void HandleHeldObject()
    {
        if (isHolding && heldRigidbody != null && holdPoint != null)
        {
            // MovePosition обеспечивает физическое перемещение с учётом коллизий
            heldRigidbody.MovePosition(holdPoint.position);
        }
    }

    void OnDestroy()
    {
        // Освобождаем объект, если он ещё удерживается при уничтожении игрока
        if (isHolding)
            StopHolding();
    }
}