using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Упрощённое подключение: пользователь выбирает два разъёма.
/// Совпадающие типы с противоположными ролями соединяются визуальной линией.
/// </summary>
[DisallowMultipleComponent]
public sealed class ConnectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private PlacementController placementController;

    [Header("Raycast")]
    [SerializeField] private LayerMask connectorLayers = ~0;
    [SerializeField, Min(1f)] private float maxRayDistance = 100f;

    [Header("Rules")]
    [Tooltip("Включить после того, как все детали будут размещаться в слотах.")]
    [SerializeField] private bool requireInstalledComponents;

    [Header("Connection visuals")]
    [SerializeField, Min(0.001f)] private float lineWidth = 0.025f;
    [SerializeField] private Material cableJacket;
    [SerializeField] private Transform cableTray;

    private readonly List<ConnectionRecord> connections = new List<ConnectionRecord>();
    private ConnectorPort firstSelectedPort;


    public event Action<string> StatusChanged;
    public event Action<int> ConnectionCountChanged;

    public int ConnectionCount => connections.Count;
    public void GetPair(int index, out string source, out string destination)
    {
        source = connections[index].First.PortId;
        destination = connections[index].Second.PortId;
    }
    public ConnectorPort FirstSelectedPort => firstSelectedPort;

    private sealed class ConnectionRecord
    {
        public ConnectorPort First;
        public ConnectorPort Second;
        public RoutedCable Line;
    }

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (placementController == null)
            placementController = GetComponent<PlacementController>();

        if (worldCamera == null)
            Debug.LogError("ConnectionManager: назначьте World Camera.", this);


    }

    private void Update()
    {
        if (WorkshopMenu.InputBlocked) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (placementController != null && placementController.IsPlacing)
            return;

        HandleClick();
    }

    private void HandleClick()
    {
        if (worldCamera == null)
            return;

        Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(
            ray, maxRayDistance, connectorLayers, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Ищем именно ConnectorPort среди всех попаданий. Это защищает
        // от ситуации, когда луч сначала встречает коллайдер родительской платы.
        ConnectorPort port = null;
        foreach (RaycastHit hit in hits)
        {
            port = hit.collider.GetComponentInParent<ConnectorPort>();
            if (port != null)
                break;
        }

        if (port == null)
        {
            ClearPendingSelection("Разъём не найден. Проверьте слой Connectors и положение Collider.");
            return;
        }

        HandlePortSelection(port);
    }

    private void HandlePortSelection(ConnectorPort port)
    {
        if (!TryValidateOwner(port, out string ownerError))
        {
            SetStatus(ownerError);
            return;
        }

        if (port.IsConnected)
        {
            SetStatus($"Разъём {port.Label} уже подключён к {port.ConnectedPort.Label}.");
            return;
        }

        if (firstSelectedPort == null)
        {
            firstSelectedPort = port;
            SetStatus($"Выбран {port.Label}. Теперь выберите второй совместимый разъём.");
            return;
        }

        if (firstSelectedPort == port)
        {
            ClearPendingSelection("Выбор разъёма отменён.");
            return;
        }

        if (!TryConnect(firstSelectedPort, port, out string result))
        {
            SetStatus(result);
            // Сохраняем первый порт, чтобы пользователь мог попробовать другой.
            return;
        }

        firstSelectedPort = null;
        SetStatus(result);
        ConnectionCountChanged?.Invoke(connections.Count);
    }

    public bool TryConnect(ConnectorPort first, ConnectorPort second, out string message)
    {
        if (first == null || second == null)
        {
            message = "Не выбраны оба разъёма.";
            return false;
        }

        if (first == second)
        {
            message = "Нельзя соединить разъём с самим собой.";
            return false;
        }

        if (first.IsConnected || second.IsConnected)
        {
            message = "Один из разъёмов уже подключён.";
            return false;
        }

        if (first.Type != second.Type)
        {
            message = $"Несовместимые типы: {first.Type} и {second.Type}.";
            return false;
        }

        if (first.Role == second.Role)
        {
            message = "Нужно выбрать один исходный и один целевой разъём.";
            return false;
        }

        if (!TryValidateOwner(first, out message) || !TryValidateOwner(second, out message))
            return false;

        HardwareComponentView firstOwner = first.Owner;
        HardwareComponentView secondOwner = second.Owner;
        if (firstOwner == secondOwner)
        {
            message = "Нельзя соединить два разъёма одного и того же комплектующего.";
            return false;
        }

        // Нормализуем порядок: Source -> Destination независимо от порядка щелчков.
        ConnectorPort source = first.Role == ConnectorRole.Source ? first : second;
        ConnectorPort destination = first.Role == ConnectorRole.Destination ? first : second;

        // TryConnectTo устанавливает двустороннюю связь сразу для обоих портов.
        // Второй вызов destination.TryConnectTo(source) здесь не нужен:
        // после первого вызова оба порта уже считаются занятыми.
        if (!source.TryConnectTo(destination))
        {
            message = "Не удалось зарегистрировать соединение. Один из портов уже занят или недоступен.";
            return false;
        }

        var line = new GameObject(source.PortId + " cable").AddComponent<RoutedCable>();
        line.Initialize(source, destination, cableTray, cableJacket, lineWidth);
        connections.Add(new ConnectionRecord
        {
            First = source,
            Second = destination,
            Line = line
        });

        message = $"Соединение создано: {source.Label} → {destination.Label}.";
        return true;
    }

    private bool TryValidateOwner(ConnectorPort port, out string message)
    {
        HardwareComponentView owner = port != null ? port.Owner : null;
        if (owner == null || owner.Data == null)
        {
            message = "У разъёма не найдено комплектующее с HardwareComponentView и HardwareComponentData.";
            return false;
        }

        if (owner.Data.ComponentType != port.ExpectedOwnerType)
        {
            message = $"Разъём {port.Label} должен принадлежать компоненту типа {port.ExpectedOwnerType}, " +
                      $"но найден {owner.Data.ComponentType}.";
            return false;
        }

        if (requireInstalledComponents && owner.Data.ComponentType != HardwareComponentType.Case && !owner.IsInstalled)
        {
            message = $"Сначала установите комплектующее {owner.Data.DisplayName}.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public bool UndoLastConnection()
    {
        if (connections.Count == 0)
        {
            SetStatus("Нет соединений для отмены.");
            return false;
        }

        int lastIndex = connections.Count - 1;
        ConnectionRecord record = connections[lastIndex];
        connections.RemoveAt(lastIndex);

        if (record.First != null) record.First.Disconnect();
        if (record.Second != null) record.Second.Disconnect();
        if (record.Line != null) Destroy(record.Line.gameObject);

        SetStatus("Последнее соединение отменено.");
        ConnectionCountChanged?.Invoke(connections.Count);
        return true;
    }

    public void ResetConnections()
    {
        for (int i = connections.Count - 1; i >= 0; i--)
        {
            ConnectionRecord record = connections[i];
            if (record.First != null) record.First.Disconnect();
            if (record.Second != null) record.Second.Disconnect();
            if (record.Line != null) Destroy(record.Line.gameObject);
        }

        connections.Clear();
        firstSelectedPort = null;
        SetStatus("Все соединения сброшены.");
        ConnectionCountChanged?.Invoke(0);
    }

    private void ClearPendingSelection(string message)
    {
        if (firstSelectedPort == null)
            return;

        firstSelectedPort = null;
        SetStatus(message);
    }

    private void SetStatus(string message)
    {
        Debug.Log(message, this);
        StatusChanged?.Invoke(message);
    }

}
