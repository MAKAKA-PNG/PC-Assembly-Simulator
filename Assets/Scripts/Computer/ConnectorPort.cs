using UnityEngine;

public enum ConnectorType
{
    Atx24Pin,
    CpuEps8Pin,
    Pcie8Pin,
    CpuFan,
    SataPower,
    SataData
}

public enum ConnectorRole
{
    Source,
    Destination
}

/// <summary>
/// Маркер отдельного разъёма. Разместите его на дочернем объекте
/// комплектующего вместе с небольшим Collider.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class ConnectorPort : MonoBehaviour
{
    [Header("Port identification")]
    [SerializeField] private string portId = "PORT_ID";
    [SerializeField] private ConnectorType connectorType;
    [SerializeField] private ConnectorRole role;
    [SerializeField] private HardwareComponentType expectedOwnerType;

    public string PortId => portId;
    public ConnectorType Type => connectorType;
    public ConnectorRole Role => role;
    public HardwareComponentType ExpectedOwnerType => expectedOwnerType;
    public HardwareComponentView Owner => GetComponentInParent<HardwareComponentView>();
    public ConnectorPort ConnectedPort { get; private set; }
    public bool IsConnected => ConnectedPort != null;

    /// <summary>
    /// Создаёт двустороннюю связь за один вызов.
    /// Оба порта либо подключаются вместе, либо остаются свободными.
    /// </summary>
    public bool TryConnectTo(ConnectorPort other)
    {
        if (other == null || other == this)
            return false;

        if (IsConnected || other.IsConnected)
            return false;

        // Регистрируем связь сразу с обеих сторон.
        ConnectedPort = other;
        other.ConnectedPort = this;
        return true;
    }

    /// <summary>
    /// Снимает связь с обеих сторон, если она существует.
    /// </summary>
    public void Disconnect()
    {
        ConnectorPort other = ConnectedPort;
        ConnectedPort = null;

        if (other != null && other.ConnectedPort == this)
            other.ConnectedPort = null;
    }

    private void Reset()
    {
        Collider portCollider = GetComponent<Collider>();
        if (portCollider != null)
            portCollider.isTrigger = false;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(portId))
            portId = name;

        Collider portCollider = GetComponent<Collider>();
        if (portCollider != null && portCollider.isTrigger)
        {
            Debug.LogWarning(
                $"Разъём {name}: для этого этапа отключите Is Trigger у Collider.",
                this
            );
        }
    }
}
