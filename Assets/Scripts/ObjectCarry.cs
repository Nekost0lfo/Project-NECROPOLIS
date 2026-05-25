using UnityEngine;

public class ObjectCarry : MonoBehaviour
{
    [Header("Настройки")]
    public float pickUpRange = 3f;      // Дистанция подбора
    public KeyCode carryKey = KeyCode.E; // Клавиша поднять/отпустить

    [Header("Настройки удержания")]
    public float carryForce = 100f;      // Сила удержания
    public float smoothSpeed = 10f;      // Плавность движения

    [Header("Ссылки (заполняются автоматически)")]
    public Transform holdPoint;          // Точка, куда крепится объект (перед игроком)

    private Rigidbody carriedObject;      // Текущий удерживаемый объект
    private Collider carriedCollider;     // Коллайдер объекта (чтобы отключить физику с игроком)
    private float originalDrag;           // Сохраняем original Drag объекта

    private WallClimbing wallClimbing;

    void Start()
    {
        wallClimbing = GetComponent<WallClimbing>();

        // Создаём точку удержания, если её нет (перед игроком)
        if (holdPoint == null)
        {
            GameObject hold = new GameObject("HoldPoint");
            hold.transform.parent = transform;
            hold.transform.localPosition = new Vector3(0, 0.5f, 1f); // перед игроком
            holdPoint = hold.transform;
        }
    }

    void Update()
    {
        // Проверяем нажатие E
        if (Input.GetKeyDown(carryKey))
        {
            if (carriedObject == null)
            {
                // Пытаемся поднять объект
                TryPickUp();
            }
            else
            {
                // Отпускаем объект
                DropObject();
            }
        }

        // Если объект удерживается, двигаем его к точке удержания
        if (carriedObject != null)
        {
            MoveCarriedObject();
        }
    }

    void TryPickUp()
    {
        // Создаём луч из камеры вперёд
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickUpRange))
        {
            // Проверяем, есть ли у объекта Rigidbody
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                carriedObject = rb;
                carriedCollider = hit.collider;

                // Настраиваем физику объекта при удержании
                originalDrag = carriedObject.linearDamping;
                carriedObject.linearDamping = 10f;           // Больше сопротивления для стабильности
                carriedObject.useGravity = false;    // Отключаем гравитацию

                // Отключаем коллизию между игроком и объектом (чтобы не толкало)
                Collider playerCollider = GetComponent<Collider>();
                if (playerCollider != null && carriedCollider != null)
                {
                    Physics.IgnoreCollision(playerCollider, carriedCollider, true);
                }

                // Отключаем лазание, пока несём объект
                //if (wallClimbing != null)
                    //wallClimbing.canClimb = false;

                Debug.Log($"Взял объект: {carriedObject.gameObject.name}");
            }
        }
    }

    void DropObject()
    {
        if (carriedObject == null) return;

        // Возвращаем физику объекта в нормальное состояние
        carriedObject.linearDamping = originalDrag;
        carriedObject.useGravity = true;

        // Включаем коллизию обратно
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null && carriedCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, carriedCollider, false);
        }

        Debug.Log($"Отпустил объект: {carriedObject.gameObject.name}");

        // Включаем лазание обратно
        //if (wallClimbing != null)
           // wallClimbing.canClimb = true;

        // Очищаем переменные
        carriedObject = null;
        carriedCollider = null;
    }

    void MoveCarriedObject()
    {
        if (carriedObject == null) return;

        // Вычисляем позицию, куда должен переместиться объект
        Vector3 targetPosition = holdPoint.position;

        // Используем MovePosition для плавного перемещения (без рывков)
        carriedObject.MovePosition(Vector3.Lerp(carriedObject.position, targetPosition, smoothSpeed * Time.deltaTime));

        // Дополнительно: сохраняем вращение объекта (или можно поворачивать как нужно)
        // carriedObject.MoveRotation(Quaternion.identity); // раскомментируйте, если нужно фиксировать вращение
    }

    // Опционально: визуализация радиуса подбора в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (Camera.main != null)
        {
            Vector3 rayDirection = Camera.main.transform.forward * pickUpRange;
            Gizmos.DrawRay(Camera.main.transform.position, rayDirection);
        }
    }
}