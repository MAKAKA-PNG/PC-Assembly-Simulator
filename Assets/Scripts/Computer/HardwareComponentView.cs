using UnityEngine;

/// <summary>
/// Связывает 3D-модель комплектующего с информационными данными,
/// профилем совместимости и состоянием установки в текущей сборке.
/// </summary>
[DisallowMultipleComponent]
public sealed class HardwareComponentView : MonoBehaviour
{
    [Header("Component data")]
    [SerializeField] private HardwareComponentData data;

    [Header("Compatibility")]
    [SerializeField] private HardwareCompatibilityProfile compatibilityProfile;

    public HardwareComponentData Data => data;
    public HardwareCompatibilityProfile CompatibilityProfile => compatibilityProfile;
    public ComponentSlot CurrentSlot { get; private set; }
    public bool IsInstalled => CurrentSlot != null;

    public bool TryMarkInstalled(ComponentSlot slot)
    {
        if (slot == null || IsInstalled)
            return false;

        CurrentSlot = slot;
        return true;
    }

    public void ClearInstalledSlot(ComponentSlot slot)
    {
        if (CurrentSlot == slot)
            CurrentSlot = null;
    }

    private void OnValidate()
    {
        if (data != null && string.IsNullOrWhiteSpace(data.DisplayName))
            Debug.LogWarning($"У объекта {name} назначены данные без названия.", this);
    }
}
