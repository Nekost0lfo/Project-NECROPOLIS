using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [Header("Основные настройки")]
    public float speed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float mouseSensitivity = 25f;

    [Header("Гравитация и прыжок")]
    public float gravity = -9.81f;
    public float jumpForce = 1.5f;

    [Header("Выносливость (Sprint)")]
    public float maxStamina = 4f;
    public float staminaRegenRate = 1f;
    public float staminaDrainRate = 1f;

    [Header("Компоненты")]
    public Transform playerCamera;   // назначьте в инспекторе дочернюю камеру
    private CharacterController controller;

    private bool gravityEnabled = true;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float yVelocity;

    private bool isRunning = false;
    private float currentStamina;
    private bool canRun = true;

    private InputSystem_Actions inputActions;

    void Awake()
    {
        // Создаём экземпляр, но не подписываемся пока
        inputActions = new InputSystem_Actions();
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            // ------ НАСТРОЙКА КАМЕРЫ ------
            if (playerCamera != null)
            {
                // Делаем камеру дочерней (если ещё нет) – чтобы она следовала за игроком
                playerCamera.SetParent(transform);
                playerCamera.localPosition = new Vector3(0, 0.6f, 0); // подберите высоту
                playerCamera.localRotation = Quaternion.identity;

                var cam = playerCamera.GetComponent<Camera>();
                if (cam != null) cam.enabled = true;

                var listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }

            // ------ БЛОКИРОВКА КУРСОРА ------
            Cursor.lockState = CursorLockMode.Locked;

            // ------ ПОДПИСКА НА ВВОД ------
            inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
            inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
            inputActions.Player.Jump.performed += ctx => Jump();
            inputActions.Player.Sprint.performed += ctx => isRunning = true;
            inputActions.Player.Sprint.canceled += ctx => isRunning = false;

            // ВКЛЮЧАЕМ Input Actions
            inputActions.Enable();

            // Инициализация выносливости
            currentStamina = maxStamina;
        }
        else
        {
            // Отключаем камеру и весь скрипт для чужих игроков
            if (playerCamera != null)
            {
                var cam = playerCamera.GetComponent<Camera>();
                if (cam != null) cam.enabled = false;

                var listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
            enabled = false;  // скрипт не работает на чужих объектах
        }
    }

    void OnEnable()
    {
        // Можно оставить пустым, активация ввода теперь в OnNetworkSpawn
    }

    void OnDisable()
    {
        if (IsOwner)
            inputActions?.Disable();
    }

    void Update()
    {
        if (!IsOwner) return;   // дополнительная страховка

        HandleStamina();
        HandleMovement();
        HandleLook();
        ApplyGravity();
    }

    void HandleStamina()
    {
        // Если пытаемся бежать и можем бежать
        if (isRunning && canRun && currentStamina > 0)
        {
            // Тратим выносливость
            currentStamina -= staminaDrainRate * Time.deltaTime;

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                canRun = false;
                isRunning = false;  // Принудительно останавливаем бег
                Debug.Log("Выносливость кончилась! Бег временно недоступен.");
            }
        }
        else
        {
            // Восстанавливаем выносливость, если не бежим
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;

                if (currentStamina >= maxStamina)
                {
                    currentStamina = maxStamina;
                    canRun = true;   // Снова можно бежать
                }
            }
        }
    }

    void HandleMovement()
    {
        // Вычисляем текущую скорость (с учётом бега и выносливости)
        float currentSpeed = speed;
        if (isRunning && canRun && currentStamina > 0)
        {
            currentSpeed = speed * runSpeedMultiplier;
        }

        // Горизонтальное движение (WASD)
        Vector3 horizontalMove = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(horizontalMove * currentSpeed * Time.deltaTime);

        // Вертикальное движение (гравитация/прыжок)
        Vector3 verticalMove = Vector3.up * yVelocity;
        controller.Move(verticalMove * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (!gravityEnabled) return;

        if (controller.isGrounded && yVelocity < 0)
        {
            yVelocity = -2f;
        }

        if (yVelocity > 0)
        {
            yVelocity += gravity * 2f * Time.deltaTime;
        }
        else
        {
            yVelocity += gravity * 2f * Time.deltaTime;
        }
    }

    void Jump()
    {
        if (controller.isGrounded)
        {
            yVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
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

    void OnGUI()
    {
        if (!IsOwner) return;

        if (maxStamina <= 0) return;
        float staminaPercent = currentStamina / maxStamina;

        GUI.Label(new Rect(10, 60, 200, 20), $"Stamina: {currentStamina:F1} / {maxStamina}");
        GUI.backgroundColor = new Color(0.2f, 0.7f, 1f, 1f);
        GUI.color = Color.white;
        GUI.Box(new Rect(10, 10, 200, 20), "STAMINA");
        GUI.Box(new Rect(10, 32, 200 * staminaPercent, 12), "");
        GUI.backgroundColor = Color.white;
    }

    public void SetGravityEnabled(bool enabled)
    {
        gravityEnabled = enabled;
    }

    public float GetCurrentStamina() => currentStamina;
    public void DrainStamina(float amount)
    {
        currentStamina -= amount;
        if (currentStamina < 0) currentStamina = 0;
    }
}