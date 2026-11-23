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
    [SerializeField] private Color connectionColor = new Color(0.15f, 0.85f, 1f, 1f);

    private readonly List<ConnectionRecord> connections = new List<ConnectionRecord>();
    private ConnectorPort firstSelectedPort;
    private Material lineMaterial;

    public event Action<string> StatusChanged;
    public event Action<int> ConnectionCountChanged;

    public int ConnectionCount => connections.Count;
    public ConnectorPort FirstSelectedPort => firstSelectedPort;

    private sealed class ConnectionRecord
    {
        public ConnectorPort First;
        public ConnectorPort Second;
        public LineRenderer Line;
    }

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (placementController == null)
            placementController = GetComponent<PlacementController>();

        if (worldCamera == null)
            Debug.LogError("ConnectionManager: назначьте World Camera.", this);

        lineMaterial = CreateLineMaterial();
    }

    private void Update()
    {
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
            SetStatus($"Разъём {port.PortId} уже подключён к {port.ConnectedPort.PortId}.");
            return;
        }

        if (firstSelectedPort == null)
        {
            firstSelectedPort = port;
            SetStatus($"Выбран {port.PortId}. Теперь выберите второй совместимый разъём.");
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

        LineRenderer line = CreateConnectionLine(source.transform.position, destination.transform.position);
        connections.Add(new ConnectionRecord
        {
            First = source,
            Second = destination,
            Line = line
        });

        message = $"Соединение создано: {source.PortId} → {destination.PortId} ({source.Type}).";
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
            message = $"Разъём {port.PortId} должен принадлежать компоненту типа {port.ExpectedOwnerType}, " +
                      $"но найден {owner.Data.ComponentType}.";
            return false;
        }

        if (requireInstalledComponents && !owner.IsInstalled)
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

    private LineRenderer CreateConnectionLine(Vector3 start, Vector3 end)
    {
        GameObject lineObject = new GameObject("ConnectionLine");
        lineObject.transform.SetParent(transform, true);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = connectionColor;
        line.endColor = connectionColor;
        line.numCapVertices = 4;
        line.alignment = LineAlignment.View;
        line.material = lineMaterial;
        return line;
    }

    private Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            Debug.LogWarning("ConnectionManager: не найден Unlit shader. Линии могут отображаться без материала.", this);
            return null;
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", connectionColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", connectionColor);
        return material;
    }

    private void LateUpdate()
    {
        // Обновляем линии при перемещении камеры или компонентов.
        foreach (ConnectionRecord record in connections)
        {
            if (record.Line == null || record.First == null || record.Second == null)
                continue;

            record.Line.SetPosition(0, record.First.transform.position);
            record.Line.SetPosition(1, record.Second.transform.position);
        }
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}
