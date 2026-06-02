using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ObjectInteraction : NetworkBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float _interactDistance = 3f;
    [SerializeField] private float _holdDistance = 1.5f;
    [SerializeField] private float _throwForce = 10f;
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private Transform _holdPoint;

    [Header("Клавиши")]
    [SerializeField] private KeyCode _grabKey = KeyCode.E;
    [SerializeField] private KeyCode _kickKey = KeyCode.F;

    private Camera _playerCamera;
    private NetworkObject _heldNetworkObject;
    private Rigidbody _heldRigidbody;

    void Start()
    {
        _playerCamera = GetComponentInChildren<Camera>();
        if (_holdPoint == null) _holdPoint = transform;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(_grabKey))
        {
            if (_heldNetworkObject == null) TryGrabObject();
            else TryDropObject();
        }

        if (Input.GetKeyDown(_kickKey)) TryKickObject();
    }

    void FixedUpdate()
    {
        // Сервер двигает удерживаемый объект к точке захвата
        if (_heldNetworkObject != null && IsServer)
        {
            Vector3 targetPos = _holdPoint.position + _holdPoint.forward * _holdDistance;
            _heldNetworkObject.transform.position = targetPos;
        }
    }

    private void TryGrabObject()
    {
        Ray ray = _playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance, _interactableLayer))
        {
            var netObj = hit.collider.GetComponent<NetworkObject>();
            if (netObj != null) GrabObjectServerRpc(netObj.NetworkObjectId);
        }
    }

    [ServerRpc]
    private void GrabObjectServerRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            // Передаём владение серверу, чтобы позиция синхронизировалась на всех
            netObj.ChangeOwnership(NetworkManager.ServerClientId);

            // Делаем объект кинематическим
            var rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Сообщаем всем клиентам (включая владельца)
            GrabObjectClientRpc(objectId);
        }
    }

    [ClientRpc]
    private void GrabObjectClientRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            _heldNetworkObject = netObj;
            _heldRigidbody = netObj.GetComponent<Rigidbody>();
        }
    }

    private void TryDropObject()
    {
        if (_heldNetworkObject != null)
        {
            DropObjectServerRpc(_heldNetworkObject.NetworkObjectId);
        }
    }

    [ServerRpc]
    private void DropObjectServerRpc(ulong objectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            // Возвращаем владение игроку, который держал
            netObj.ChangeOwnership(OwnerClientId);

            // Включаем физику обратно
            var rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            DropObjectClientRpc(objectId);
        }
    }

    [ClientRpc]
    private void DropObjectClientRpc(ulong objectId)
    {
        if (_heldNetworkObject != null && _heldNetworkObject.NetworkObjectId == objectId)
        {
            _heldNetworkObject = null;
            _heldRigidbody = null;
        }
    }

    private void TryKickObject()
    {
        Ray ray = _playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance, _interactableLayer))
        {
            var netObj = hit.collider.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                Vector3 dir = (hit.point - transform.position).normalized;
                KickObjectServerRpc(netObj.NetworkObjectId, dir);
            }
        }
    }

    [ServerRpc]
    private void KickObjectServerRpc(ulong objectId, Vector3 direction)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            var rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(direction * _throwForce, ForceMode.Impulse);
            }
        }
    }
}