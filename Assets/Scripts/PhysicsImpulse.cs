using UnityEngine;

public class PhysicsImpulseMouse : MonoBehaviour
{
    public float impulseForce = 5f;
    public KeyCode activationKey = KeyCode.F;

    // Ссылка на игрока (перетащите в инспекторе)
    public GameObject player;

    void Update()
    {
        if (Input.GetKeyDown(activationKey))
        {
            // Создаём луч от камеры в точку курсора
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Получаем Rigidbody у объекта, на который навели мышь
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // ВЫЧИСЛЯЕМ НАПРАВЛЕНИЕ ОТ ИГРОКА К ОБЪЕКТУ
                    Vector3 directionFromPlayer = hit.transform.position - player.transform.position;

                    // Нормализуем направление (чтобы длина вектора = 1, сила не зависела от расстояния)
                    directionFromPlayer.Normalize();

                    // Прикладываем силу в этом направлении
                    rb.AddForce(directionFromPlayer * impulseForce, ForceMode.Impulse);

                    Debug.Log($"Толкнули {hit.collider.gameObject.name} в направлении от игрока!");
                }
            }
        }
    }
}