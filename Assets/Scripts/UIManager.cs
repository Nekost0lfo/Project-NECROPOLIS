using UnityEngine;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas; // Перетащите Canvas с кнопками
    [SerializeField] private NetworkManager networkManager;

    public void StartHostClicked()
    {
        menuCanvas.SetActive(false);   // Скрываем UI
        networkManager.StartHost();     // Запускаем хост
    }

    public void StartClientClicked()
    {
        menuCanvas.SetActive(false);   // Скрываем UI
        networkManager.StartClient();   // Запускаем клиент
    }
}
