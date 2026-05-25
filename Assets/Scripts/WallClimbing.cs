using UnityEngine;

public class WallClimbing : MonoBehaviour
{
    [Header("Настройки обнаружения стены")]
    public float detectionDistance = 1.2f;
    public float sphereRadius = 0.4f;
    public float forwardOffset = 0.3f;
    public LayerMask wallLayer = -1;

    [Header("Настройки лазания")]
    public KeyCode climbKey = KeyCode.Space;
    public float climbSpeed = 5f;
    public float slideSpeed = 2f;

    [Header("Настройки перелезания")]
    public float climbUpHeight = 1.8f;
    public float climbUpDuration = 0.6f;

    [Header("Выносливость")]
    public bool useStamina = true;
    public float staminaDrainRateClimbing = 1.2f;
    public float minStaminaToClimb = 0.5f;

    [Header("Отладка")]
    public bool showGizmos = true;
    public bool DebugMode = false;

    // Состояния
    private bool isOnWall = false;
    private bool isClimbing = false;
    private bool isClimbingUp = false;
    private Vector3 wallNormal;

    // Для плавного поворота
    private Quaternion targetRotation;
    private Quaternion startRotation;
    private float rotationTime = 0f;
    private bool isRotating = false;

    // Переменные для перелезания
    private Vector3 climbUpStartPos;
    private Vector3 climbUpEndPos;
    private float climbUpTimer = 0f;

    // Компоненты
    private CharacterController controller;
    private PlayerController playerController;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (controller == null) return;

        CheckWallContact();

        if (!isClimbingUp)
        {
            if (isOnWall && Input.GetKey(climbKey) && CanClimbOver())
            {
                StartClimbUp();
            }
            else if (isOnWall && Input.GetKey(climbKey))
            {
                bool hasEnoughStamina = CheckStaminaForClimbing();

                if (hasEnoughStamina)
                {
                    if (!isClimbing)
                    {
                        isClimbing = true;
                        // ПЛАВНЫЙ ПОВОРОТ вместо резкого
                        StartSmoothRotation(Quaternion.LookRotation(-wallNormal));
                        Debug.Log("Начал лазание!");
                    }
                    DrainStaminaForClimbing();
                }
                else
                {
                    if (isClimbing)
                    {
                        FallFromWall();
                    }
                    isClimbing = false;
                }
            }
            else
            {
                if (isClimbing)
                {
                    isClimbing = false;
                    Debug.Log("Прекратил лазание");
                }
            }
        }

        ApplyMovement();
    }

    // ПЛАВНЫЙ ПОВОРОТ К СТЕНЕ
    void StartSmoothRotation(Quaternion newTarget)
    {
        startRotation = transform.rotation;
        targetRotation = newTarget;
        rotationTime = 0f;
        isRotating = true;
    }

    void UpdateSmoothRotation()
    {
        if (!isRotating) return;

        rotationTime += Time.deltaTime * 3f; // Скорость поворота
        float t = Mathf.Clamp01(rotationTime);

        transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

        if (t >= 1f)
        {
            isRotating = false;
        }
    }

    // ИСПРАВЛЕННОЕ ОБНАРУЖЕНИЕ СТЕНЫ (с учётом близкого расстояния)
    void CheckWallContact()
    {
        if (controller == null) return;

        // ПРАВИЛЬНЫЕ ГРАНИЦЫ
        float halfHeight = controller.height * 0.5f;
        float bottomY = transform.position.y - halfHeight + 0.2f;  // Ноги
        float centerY = transform.position.y;                      // Центр
        float topY = transform.position.y + halfHeight - 0.15f;    // Голова

        // СМЕЩАЕМ ТОЧКИ ВПЕРЁД (перед персонажем)
        float forwardOffset = 0.3f;  // На сколько вперёд от центра персонажа

        Vector3[] origins = {
        new Vector3(transform.position.x, bottomY, transform.position.z) + transform.forward * forwardOffset,
        new Vector3(transform.position.x, centerY, transform.position.z) + transform.forward * forwardOffset,
        new Vector3(transform.position.x, topY, transform.position.z) + transform.forward * forwardOffset
    };

        bool foundWall = false;
        Vector3 closestNormal = Vector3.zero;
        float closestDistance = detectionDistance;

        float currentMaxDistance = detectionDistance;

        foreach (Vector3 origin in origins)
        {
            RaycastHit hit;

            if (Physics.SphereCast(origin, sphereRadius, transform.forward, out hit, currentMaxDistance, wallLayer))
            {
                if (Mathf.Abs(hit.normal.y) < 0.3f)
                {
                    foundWall = true;
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        closestNormal = hit.normal;
                    }
                }
            }
        }

        // ПРОВЕРКА ПРИ ПРЫЖКЕ
        if (!foundWall && !controller.isGrounded)
        {
            float jumpDistance = detectionDistance * 1.5f;

            foreach (Vector3 origin in origins)
            {
                RaycastHit hit;
                if (Physics.SphereCast(origin, sphereRadius, transform.forward, out hit, jumpDistance, wallLayer))
                {
                    if (Mathf.Abs(hit.normal.y) < 0.3f)
                    {
                        foundWall = true;
                        wallNormal = hit.normal;
                        break;
                    }
                }
            }
        }

        isOnWall = foundWall;
        if (foundWall)
        {
            wallNormal = closestNormal;
        }
    }

    bool CheckStaminaForClimbing()
    {
        if (!useStamina) return true;
        if (playerController == null) return true;
        return playerController.GetCurrentStamina() > minStaminaToClimb;
    }

    void DrainStaminaForClimbing()
    {
        if (!useStamina) return;
        if (playerController == null) return;
        if (!isClimbing) return;

        playerController.DrainStamina(staminaDrainRateClimbing * Time.deltaTime);
    }

    void FallFromWall()
    {
        isClimbing = false;
        if (playerController != null)
        {
            playerController.SetGravityEnabled(true);
        }
        Debug.Log("Выносливость кончилась! Срыв со стены!");
    }

    bool CanClimbOver()
    {
        Vector3 checkPos = transform.position + transform.forward * 1.2f + Vector3.up * 2f;
        RaycastHit hit;
        return Physics.Raycast(checkPos, Vector3.down, out hit, 2f, wallLayer);
    }

    void StartClimbUp()
    {
        isClimbingUp = true;
        isClimbing = false;
        climbUpTimer = 0f;
        climbUpStartPos = transform.position;

        Vector3 forwardOffset = transform.forward * 1.2f;
        float targetY = climbUpStartPos.y + climbUpHeight;

        RaycastHit groundHit;
        Vector3 checkPos = climbUpStartPos + forwardOffset + Vector3.up * climbUpHeight;
        if (Physics.Raycast(checkPos + Vector3.up, Vector3.down, out groundHit, climbUpHeight + 1f, wallLayer))
        {
            targetY = groundHit.point.y + 0.5f;
        }

        climbUpEndPos = new Vector3(climbUpStartPos.x + forwardOffset.x, targetY, climbUpStartPos.z + forwardOffset.z);
        Debug.Log("Начинаю перелезать!");
    }

    void UpdateClimbUp()
    {
        climbUpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(climbUpTimer / climbUpDuration);
        float easedT = Mathf.SmoothStep(0, 1, t);

        Vector3 newPos = Vector3.Lerp(climbUpStartPos, climbUpEndPos, easedT);
        controller.Move(newPos - transform.position);

        if (t >= 1f)
        {
            FinishClimbUp();
        }
    }

    void FinishClimbUp()
    {
        isClimbingUp = false;
        isClimbing = false;
        controller.Move(transform.forward * 0.5f);
        Debug.Log("Перелез!");
    }

    void ApplyMovement()
    {
        // Плавный поворот
        //UpdateSmoothRotation();

        if (isClimbingUp)
        {
            UpdateClimbUp();
            return;
        }

        if (isClimbing)
        {
            float vertical = Input.GetAxisRaw("Vertical");
            float horizontal = Input.GetAxisRaw("Horizontal");

            Vector3 moveDirection = Vector3.zero;
            moveDirection += transform.up * vertical;

            Vector3 rightDirection = Vector3.Cross(wallNormal, transform.up).normalized;
            moveDirection += rightDirection * horizontal;
            moveDirection.Normalize();

            Vector3 movement = moveDirection * climbSpeed * Time.deltaTime;
            controller.Move(movement);

            if (playerController != null)
            {
                playerController.SetGravityEnabled(false);
            }
        }
        else if (isOnWall && !isClimbing)
        {
            controller.Move(Vector3.down * slideSpeed * Time.deltaTime);
            if (playerController != null)
            {
                playerController.SetGravityEnabled(true);
            }
        }
        else if (!isClimbing)
        {
            if (playerController != null)
            {
                playerController.SetGravityEnabled(true);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        if (controller == null) return;

        float halfHeight = controller.height * 0.5f;
        float bottomY = transform.position.y - halfHeight + 0.2f;
        float centerY = transform.position.y;
        float topY = transform.position.y + halfHeight - 0.15f;

        Vector3[] origins = {
        new Vector3(transform.position.x, bottomY, transform.position.z) + transform.forward * forwardOffset,
        new Vector3(transform.position.x, centerY, transform.position.z) + transform.forward * forwardOffset,
        new Vector3(transform.position.x, topY, transform.position.z) + transform.forward * forwardOffset
    };

        foreach (Vector3 origin in origins)
        {
            // Жёлтая сфера (начало луча) - теперь перед персонажем
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, sphereRadius);

            // Линия луча от начала и вперёд
            Gizmos.color = isOnWall ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + transform.forward * detectionDistance);
        }
    }
}