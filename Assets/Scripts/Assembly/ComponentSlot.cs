using UnityEngine;

/// <summary>
/// Посадочное место для одного типа комплектующего.
/// Collider этого объекта используется для наведения мышью.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class ComponentSlot : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private string slotId = "CPU_SOCKET";
    [SerializeField] private HardwareComponentType acceptedType = HardwareComponentType.Processor;
    [SerializeField] private Transform snapPoint;

    public string SlotId => slotId;
    public HardwareComponentType AcceptedType => acceptedType;
    public Transform SnapPoint => snapPoint != null ? snapPoint : transform;
    public HardwareComponentView InstalledComponent { get; private set; }
    public bool IsOccupied => InstalledComponent != null;

    public bool CanAccept(HardwareComponentView component, out string reason)
    {
        if (component == null)
        {
            reason = "Не выбрана комплектующая.";
            return false;
        }

        if (component.Data == null)
        {
            reason = "У комплектующей не заполнены информационные данные.";
            return false;
        }

        if (component.IsInstalled)
        {
            reason = component.CurrentSlot == this
                ? "Эта комплектующая уже установлена в данном слоте."
                : "Эта комплектующая уже установлена в другом слоте.";
            return false;
        }

        if (IsOccupied)
        {
            reason = "Посадочное место уже занято.";
            return false;
        }

        if (component.Data.ComponentType != acceptedType)
        {
            reason = $"Неверный тип детали. Слот {slotId} принимает: {acceptedType}.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool TryInstall(HardwareComponentView component, out string reason)
    {
        if (!CanAccept(component, out reason))
            return false;

        if (!component.TryMarkInstalled(this))
        {
            reason = "Не удалось изменить состояние комплектующей.";
            return false;
        }

        Transform target = SnapPoint;
        component.transform.SetParent(target, true);
        component.transform.SetPositionAndRotation(target.position, target.rotation);

        InstalledComponent = component;
        reason = string.Empty;
        return true;
    }

    /// <summary>Освобождает слот. Будет использоваться для механики демонтажа.</summary>
    public void Uninstall()
    {
        if (InstalledComponent == null)
            return;

        InstalledComponent.ClearInstalledSlot(this);
        InstalledComponent = null;
    }

    private void OnDrawGizmosSelected()
    {
        Collider slotCollider = GetComponent<Collider>();
        if (slotCollider == null)
            return;

        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireCube(slotCollider.bounds.center, slotCollider.bounds.size);
    }
}
