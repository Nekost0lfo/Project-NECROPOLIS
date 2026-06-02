using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        // Дожидаемся полной инициализации NetworkManager
        if (IsServer)
        {
            // Подписываемся на новых клиентов
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Хост тоже является клиентом — спавним его игрока
            SpawnPlayerForClient(NetworkManager.Singleton.LocalClientId);
        }
    }

    public override void OnDestroy()
    {
        // Отписываемся от события, чтобы не было утечек
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // Проверяем, не заспавнен ли уже игрок для этого клиента
        if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) == null)
        {
            SpawnPlayerForClient(clientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        Vector3 spawnPos = GetSpawnPositionForPlayer(clientId);
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private Vector3 GetSpawnPositionForPlayer(ulong clientId)
    {
        int index = (int)(clientId % (ulong)spawnPoints.Length);
        return spawnPoints[index].position;
    }
}