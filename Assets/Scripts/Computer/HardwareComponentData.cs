using UnityEngine;

public enum HardwareComponentType
{
    Case,
    Motherboard,
    Processor,
    CpuCooler,
    Memory,
    GraphicsCard,
    PowerSupply,
    Storage
}

/// <summary>
/// Информационная карточка комплектующего. Данные хранятся отдельно от 3D-модели.
/// </summary>
[CreateAssetMenu(menuName = "PC Simulator/Hardware Component Data", fileName = "HardwareComponentData")]
public sealed class HardwareComponentData : ScriptableObject
{
    [Header("Identification")]
    [SerializeField] private string componentId;
    [SerializeField] private string displayName;
    [SerializeField] private HardwareComponentType componentType;

    [Header("Description")]
    [TextArea(3, 6)] [SerializeField] private string description;
    [TextArea(2, 5)] [SerializeField] private string compatibilityNotes;
    [SerializeField] private string[] specifications;

    public string ComponentId => componentId;
    public string DisplayName => displayName;
    public HardwareComponentType ComponentType => componentType;
    public string Description => description;
    public string CompatibilityNotes => compatibilityNotes;
    public string[] Specifications => specifications;
}
