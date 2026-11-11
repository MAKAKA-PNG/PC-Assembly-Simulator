using UnityEngine;

/// <summary>
/// Учебный профиль совместимости комплектующего.
/// Значения должны подтверждаться документацией выбранной модели.
/// Пустые поля означают, что соответствующее свойство не задано.
/// </summary>
[CreateAssetMenu(
    menuName = "PC Simulator/Hardware Compatibility Profile",
    fileName = "HardwareCompatibilityProfile")]
public sealed class HardwareCompatibilityProfile : ScriptableObject
{
    [Header("CPU / Socket")]
    [SerializeField] private string socket;
    [SerializeField] private string[] supportedSockets;

    [Header("Memory")]
    [SerializeField] private string memoryStandard;
    [SerializeField] private string memoryFormFactor;
    [SerializeField] private string[] supportedMemoryFormFactors;

    [Header("Form factor")]
    [SerializeField] private string formFactor;
    [SerializeField] private string[] supportedMotherboardFormFactors;
    [SerializeField] private string[] supportedPsuFormFactors;

    [Header("Interfaces")]
    [SerializeField] private string interfaceType;
    [SerializeField] private string[] supportedInterfaces;

    [Header("Dimensions (mm)")]
    [Min(0f)] [SerializeField] private float lengthMm;
    [Min(0f)] [SerializeField] private float heightMm;
    [Min(0f)] [SerializeField] private float maxGpuLengthMm;
    [Min(0f)] [SerializeField] private float maxCpuCoolerHeightMm;

    [Header("Power (W)")]
    [Min(0)] [SerializeField] private int powerDrawWatts;
    [Min(0)] [SerializeField] private int ratedPowerWatts;

    public string Socket => socket;
    public string[] SupportedSockets => supportedSockets;
    public string MemoryStandard => memoryStandard;
    public string MemoryFormFactor => memoryFormFactor;
    public string[] SupportedMemoryFormFactors => supportedMemoryFormFactors;
    public string FormFactor => formFactor;
    public string[] SupportedMotherboardFormFactors => supportedMotherboardFormFactors;
    public string[] SupportedPsuFormFactors => supportedPsuFormFactors;
    public string InterfaceType => interfaceType;
    public string[] SupportedInterfaces => supportedInterfaces;
    public float LengthMm => lengthMm;
    public float HeightMm => heightMm;
    public float MaxGpuLengthMm => maxGpuLengthMm;
    public float MaxCpuCoolerHeightMm => maxCpuCoolerHeightMm;
    public int PowerDrawWatts => powerDrawWatts;
    public int RatedPowerWatts => ratedPowerWatts;
}
