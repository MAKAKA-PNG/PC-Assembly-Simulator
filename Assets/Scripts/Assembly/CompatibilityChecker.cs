using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Проверяет учебные правила совместимости между уже установленными комплектующими.
/// Это ограниченная модель MVP, а не универсальная база всех устройств рынка.
/// </summary>
[DisallowMultipleComponent]
public sealed class CompatibilityChecker : MonoBehaviour
{
    [Header("Power estimate")]
    [Tooltip("Дополнительный резерв мощности, учитываемый в упрощённой учебной оценке.")]
    [Min(0)] [SerializeField] private int basePowerReserveWatts = 100;
    [Tooltip("Множитель запаса к сумме CPU + GPU + базовый резерв.")]
    [Min(1f)] [SerializeField] private float powerSafetyFactor = 1.25f;

    public event Action<bool, string> CheckCompleted;

    /// <summary>Назначается на Button -> OnClick() в Inspector.</summary>
    public void CheckCompatibility()
    {
        List<HardwareComponentView> installed = FindInstalledComponents();
        List<string> errors = new List<string>();
        List<string> skipped = new List<string>();
        int performedChecks = 0;

        HardwareComponentView motherboard = FindFirst(installed, HardwareComponentType.Motherboard);
        HardwareComponentView cpu = FindFirst(installed, HardwareComponentType.Processor);
        HardwareComponentView cooler = FindFirst(installed, HardwareComponentType.CpuCooler);
        HardwareComponentView computerCase = FindFirst(installed, HardwareComponentType.Case);
        HardwareComponentView gpu = FindFirst(installed, HardwareComponentType.GraphicsCard);
        HardwareComponentView psu = FindFirst(installed, HardwareComponentType.PowerSupply);
        List<HardwareComponentView> memoryModules = FindAll(installed, HardwareComponentType.Memory);
        List<HardwareComponentView> storageDevices = FindAll(installed, HardwareComponentType.Storage);

        CheckPair(
            motherboard, cpu, "Сокет процессора",
            (board, processor) => CompareRequired(
                board.Socket, processor.Socket,
                "Сокет процессора не совпадает с сокетом материнской платы.", errors),
            skipped, errors, ref performedChecks);

        if (motherboard != null && memoryModules.Count > 0)
        {
            HardwareCompatibilityProfile boardProfile = GetProfile(motherboard, "Материнская плата", errors);
            if (boardProfile != null)
            {
                foreach (HardwareComponentView module in memoryModules)
                {
                    HardwareCompatibilityProfile ramProfile = GetProfile(module, "Модуль ОЗУ", errors);
                    if (ramProfile == null)
                        continue;

                    performedChecks++;
                    CompareRequired(
                        boardProfile.MemoryStandard,
                        ramProfile.MemoryStandard,
                        $"Поколение памяти модуля {module.Data.DisplayName} не поддерживается материнской платой.",
                        errors);

                    if (boardProfile.SupportedMemoryFormFactors != null &&
                        boardProfile.SupportedMemoryFormFactors.Length > 0 &&
                        !Contains(boardProfile.SupportedMemoryFormFactors, ramProfile.MemoryFormFactor))
                    {
                        errors.Add($"Форм-фактор памяти {ramProfile.MemoryFormFactor} не поддерживается материнской платой.");
                    }
                }
            }
            else
            {
                skipped.Add("Тип/форм-фактор ОЗУ: нет профиля совместимости материнской платы.");
            }
        }
        else
        {
            skipped.Add("ОЗУ: для проверки необходимо установить материнскую плату и хотя бы один модуль памяти.");
        }

        CheckPair(
            motherboard, computerCase, "Форм-фактор материнской платы и корпуса",
            (board, pcCase) =>
            {
                if (!Contains(pcCase.SupportedMotherboardFormFactors, board.FormFactor))
                    errors.Add($"Корпус не поддерживает форм-фактор материнской платы: {board.FormFactor}.");
            },
            skipped, errors, ref performedChecks);

        CheckPair(
            cpu, cooler, "Сокет процессора и кулера",
            (processor, cpuCooler) =>
            {
                if (!Contains(cpuCooler.SupportedSockets, processor.Socket))
                    errors.Add($"Кулер не поддерживает сокет процессора: {processor.Socket}.");
            },
            skipped, errors, ref performedChecks);

        CheckPair(
            computerCase, gpu, "Длина видеокарты и корпуса",
            (pcCase, graphicsCard) =>
            {
                if (pcCase.MaxGpuLengthMm <= 0f || graphicsCard.LengthMm <= 0f)
                    errors.Add("Для проверки габаритов GPU заполните длину видеокарты и максимальную длину GPU корпуса.");
                else if (graphicsCard.LengthMm > pcCase.MaxGpuLengthMm)
                    errors.Add($"Видеокарта длиной {graphicsCard.LengthMm:0} мм не помещается в корпус (лимит {pcCase.MaxGpuLengthMm:0} мм).");
            },
            skipped, errors, ref performedChecks);

        CheckPair(
            computerCase, cooler, "Высота кулера и корпуса",
            (pcCase, cpuCooler) =>
            {
                if (pcCase.MaxCpuCoolerHeightMm <= 0f || cpuCooler.HeightMm <= 0f)
                    errors.Add("Для проверки высоты заполните высоту кулера и максимальную высоту кулера корпуса.");
                else if (cpuCooler.HeightMm > pcCase.MaxCpuCoolerHeightMm)
                    errors.Add($"Кулер высотой {cpuCooler.HeightMm:0} мм не помещается в корпус (лимит {pcCase.MaxCpuCoolerHeightMm:0} мм).");
            },
            skipped, errors, ref performedChecks);

        if (motherboard != null && storageDevices.Count > 0)
        {
            HardwareCompatibilityProfile boardProfile = GetProfile(motherboard, "Материнская плата", errors);
            if (boardProfile != null)
            {
                foreach (HardwareComponentView storage in storageDevices)
                {
                    HardwareCompatibilityProfile storageProfile = GetProfile(storage, "Накопитель", errors);
                    if (storageProfile == null)
                        continue;

                    performedChecks++;
                    if (!Contains(boardProfile.SupportedInterfaces, storageProfile.InterfaceType))
                    {
                        errors.Add($"Материнская плата не поддерживает интерфейс накопителя {storageProfile.InterfaceType}.");
                    }
                }
            }
        }
        else
        {
            skipped.Add("Интерфейс накопителя: для проверки установите материнскую плату и накопитель.");
        }

        CheckPair(
            computerCase, psu, "Форм-фактор блока питания и корпуса",
            (pcCase, powerSupply) =>
            {
                if (!Contains(pcCase.SupportedPsuFormFactors, powerSupply.FormFactor))
                    errors.Add($"Корпус не поддерживает форм-фактор блока питания: {powerSupply.FormFactor}.");
            },
            skipped, errors, ref performedChecks);

        if (psu != null && cpu != null && gpu != null)
        {
            HardwareCompatibilityProfile psuProfile = GetProfile(psu, "Блок питания", errors);
            HardwareCompatibilityProfile cpuProfile = GetProfile(cpu, "Процессор", errors);
            HardwareCompatibilityProfile gpuProfile = GetProfile(gpu, "Видеокарта", errors);

            if (psuProfile != null && cpuProfile != null && gpuProfile != null)
            {
                performedChecks++;
                int estimatedRequired = Mathf.CeilToInt(
                    (cpuProfile.PowerDrawWatts + gpuProfile.PowerDrawWatts + basePowerReserveWatts) * powerSafetyFactor);

                if (psuProfile.RatedPowerWatts <= 0)
                    errors.Add("Не указана номинальная мощность блока питания.");
                else if (psuProfile.RatedPowerWatts < estimatedRequired)
                    errors.Add($"Мощность БП недостаточна по учебной оценке: требуется около {estimatedRequired} Вт, доступно {psuProfile.RatedPowerWatts} Вт.");
            }
        }
        else
        {
            skipped.Add("Мощность БП: для проверки необходимы установленный БП, процессор и видеокарта.");
        }

        bool compatible = errors.Count == 0 && skipped.Count == 0 && performedChecks > 0;
        string report = BuildReport(compatible, performedChecks, errors, skipped);

        if (compatible)
            Debug.Log(report, this);
        else
            Debug.LogWarning(report, this);

        CheckCompleted?.Invoke(compatible, report);
    }

    private static List<HardwareComponentView> FindInstalledComponents()
    {
        ComponentSlot[] slots = FindObjectsByType<ComponentSlot>(FindObjectsSortMode.None);
        HashSet<HardwareComponentView> unique = new HashSet<HardwareComponentView>();

        foreach (ComponentSlot slot in slots)
        {
            if (slot != null && slot.InstalledComponent != null)
                unique.Add(slot.InstalledComponent);
        }

        return unique.Where(component => component != null && component.Data != null).ToList();
    }

    private static HardwareComponentView FindFirst(
        List<HardwareComponentView> components,
        HardwareComponentType type)
    {
        return components.FirstOrDefault(component => component.Data.ComponentType == type);
    }

    private static List<HardwareComponentView> FindAll(
        List<HardwareComponentView> components,
        HardwareComponentType type)
    {
        return components.Where(component => component.Data.ComponentType == type).ToList();
    }

    private static HardwareCompatibilityProfile GetProfile(
        HardwareComponentView component,
        string label,
        List<string> errors)
    {
        if (component.CompatibilityProfile != null)
            return component.CompatibilityProfile;

        errors.Add($"Для комплектующего «{label}» не назначен HardwareCompatibilityProfile.");
        return null;
    }

    private static bool Contains(string[] values, string expected)
    {
        if (values == null || string.IsNullOrWhiteSpace(expected))
            return false;

        return values.Any(value =>
            string.Equals(value?.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static void CompareRequired(
        string first,
        string second,
        string error,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
        {
            errors.Add("Не заполнены данные, необходимые для проверки совместимости.");
            return;
        }

        if (!string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase))
            errors.Add(error);
    }

    private static void CheckPair(
        HardwareComponentView firstComponent,
        HardwareComponentView secondComponent,
        string checkName,
        Action<HardwareCompatibilityProfile, HardwareCompatibilityProfile> check,
        List<string> skipped,
        List<string> errors,
        ref int performedChecks)
    {
        if (firstComponent == null || secondComponent == null)
        {
            skipped.Add($"{checkName}: проверка пропущена — установите оба компонента.");
            return;
        }

        HardwareCompatibilityProfile first = GetProfile(firstComponent, firstComponent.Data.DisplayName, errors);
        HardwareCompatibilityProfile second = GetProfile(secondComponent, secondComponent.Data.DisplayName, errors);

        if (first == null || second == null)
            return;

        performedChecks++;
        check(first, second);
    }

    private static string BuildReport(
        bool compatible,
        int performedChecks,
        List<string> errors,
        List<string> skipped)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(compatible ? "ПРОВЕРКА СОВМЕСТИМОСТИ ПРОЙДЕНА" : "СОВМЕСТИМОСТЬ НЕ ПОДТВЕРЖДЕНА");
        builder.AppendLine($"Выполнено проверок: {performedChecks}.");

        if (errors.Count > 0)
        {
            builder.AppendLine("Обнаруженные проблемы:");
            for (int i = 0; i < errors.Count; i++)
                builder.AppendLine($"{i + 1}. {errors[i]}");
        }
        else if (compatible)
        {
            builder.AppendLine("По заполненным профилям все выполненные проверки пройдены.");
        }

        if (skipped.Count > 0)
        {
            builder.AppendLine("Не выполнено — не хватает установленных компонентов:");
            foreach (string item in skipped)
                builder.AppendLine("- " + item);
        }

        builder.AppendLine("Важно: это ограниченная учебная проверка MVP. Она не заменяет проверку спецификаций производителя и не учитывает все аппаратные нюансы.");
        return builder.ToString();
    }
}
