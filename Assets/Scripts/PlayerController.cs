using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Основные настройки")]
    public float speed = 5f; //2
    public float runSpeedMultiplier = 1.5f; //
    public float mouseSensitivity = 25f;

    [Header("Гравитация и прыжок")]
    public float gravity = -9.81f; //-35
    public float jumpForce = 1.5f; //0.5

    [Header("Выносливость (Sprint)")]
    public float maxStamina = 4f;        // Максимальное время бега (секунды)
    public float staminaRegenRate = 1f;  // Скорость восстановления (в секундах)
    public float staminaDrainRate = 1f;  // Скорость траты (в секундах)

    [Header("Компоненты")]
    public Transform playerCamera;
    private CharacterController controller;

    private bool gravityEnabled = true;

    // Приватные переменные
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float yVelocity;

    // Sprint/Stamina
    private bool isRunning = false;
    private float currentStamina;        // Текущая выносливость
    private bool canRun = true;          // Может ли бежать

    private InputSystem_Actions inputActions;

    void Awake()
    {
        inputActions = new InputSystem_Actions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => Jump();

        inputActions.Player.Sprint.performed += ctx => isRunning = true;
        inputActions.Player.Sprint.canceled += ctx => isRunning = false;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        // Инициализируем выносливость
        currentStamina = maxStamina;
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        HandleStamina();      // ← НОВОЕ: управление выносливостью
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
        // Проверка, чтобы избежать ошибок
        if (maxStamina <= 0) return;

        float staminaPercent = currentStamina / maxStamina;

        // Временный текст для отладки (покажет, работает ли OnGUI)
        GUI.Label(new Rect(10, 60, 200, 20), $"Stamina: {currentStamina:F1} / {maxStamina}");

        // Устанавливаем яркий голубой цвет
        GUI.backgroundColor = new Color(0.2f, 0.7f, 1f, 1f);
        GUI.color = Color.white;

        // Рисуем рамку с текстом
        GUI.Box(new Rect(10, 10, 200, 20), "STAMINA");

        // Рисуем полоску выносливости
        GUI.Box(new Rect(10, 32, 200 * staminaPercent, 12), "");

        // Сбрасываем цвет обратно
        GUI.backgroundColor = Color.white;
    }

    public void SetGravityEnabled(bool enabled)
    {
        gravityEnabled = enabled;
    }

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public void DrainStamina(float amount)
    {
        currentStamina -= amount;
        if (currentStamina < 0)
        {
            currentStamina = 0;
        }
    }
}