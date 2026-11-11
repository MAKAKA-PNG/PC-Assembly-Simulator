using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class RequiredAssemblySlot
{
    [Tooltip("Название посадочного места для отчёта, например: Сокет процессора")]
    public string DisplayName;

    [Tooltip("Ссылка на объект со скриптом ComponentSlot")]
    public ComponentSlot Slot;

    [Tooltip("Если выключено, слот не учитывается при проверке MVP")]
    public bool Required = true;
}

/// <summary>
/// Проверяет, заполнены ли обязательные посадочные места.
/// Не заменяет CompatibilityChecker: на этом этапе проверяется наличие
/// деталей и соответствие типа детали типу слота.
/// </summary>
[DisallowMultipleComponent]
public sealed class AssemblyValidator : MonoBehaviour
{
    [Header("Required slots")]
    [SerializeField] private List<RequiredAssemblySlot> requiredSlots =
        new List<RequiredAssemblySlot>();

    [Header("Optional result panel")]
    [SerializeField] private AssemblyValidationPanel resultPanel;

    public event Action<bool, string> ValidationCompleted;

    /// <summary>Назначается на Button -> OnClick() в Inspector.</summary>
    public void ValidateAssembly()
    {
        List<string> issues = new List<string>();
        HashSet<ComponentSlot> checkedSlots = new HashSet<ComponentSlot>();
        HashSet<HardwareComponentView> checkedComponents =
            new HashSet<HardwareComponentView>();

        int requiredCount = 0;
        int installedCount = 0;

        if (requiredSlots == null || requiredSlots.Count == 0)
        {
            issues.Add("В Inspector не назначены обязательные посадочные места.");
        }
        else
        {
            foreach (RequiredAssemblySlot requirement in requiredSlots)
            {
                if (requirement == null || !requirement.Required)
                    continue;

                requiredCount++;
                string slotName = !string.IsNullOrWhiteSpace(requirement.DisplayName)
                    ? requirement.DisplayName
                    : "Посадочное место без названия";

                if (requirement.Slot == null)
                {
                    issues.Add($"{slotName}: не назначен объект ComponentSlot в Inspector.");
                    continue;
                }

                ComponentSlot slot = requirement.Slot;

                if (!checkedSlots.Add(slot))
                {
                    issues.Add($"{slotName}: один и тот же слот добавлен в список проверки повторно.");
                    requiredCount--;
                    continue;
                }

                if (!slot.IsOccupied || slot.InstalledComponent == null)
                {
                    issues.Add($"{slotName}: комплектующая не установлена.");
                    continue;
                }

                HardwareComponentView component = slot.InstalledComponent;

                if (component.Data == null)
                {
                    issues.Add($"{slotName}: у установленной детали не назначен HardwareComponentData.");
                    continue;
                }

                if (component.Data.ComponentType != slot.AcceptedType)
                {
                    issues.Add(
                        $"{slotName}: тип детали {component.Data.ComponentType} " +
                        $"не соответствует типу слота {slot.AcceptedType}."
                    );
                    continue;
                }

                if (!component.IsInstalled || component.CurrentSlot != slot)
                {
                    issues.Add($"{slotName}: состояние установки детали не согласовано со слотом.");
                    continue;
                }

                if (!checkedComponents.Add(component))
                {
                    issues.Add($"{slotName}: одна и та же деталь назначена более чем одному слоту.");
                    continue;
                }

                installedCount++;
            }
        }

        bool isComplete = requiredCount > 0 && issues.Count == 0 &&
                          installedCount == requiredCount;

        string report = BuildReport(isComplete, installedCount, requiredCount, issues);

        if (resultPanel != null)
            resultPanel.ShowResult(isComplete, report);

        if (isComplete)
            Debug.Log(report, this);
        else
            Debug.LogWarning(report, this);

        ValidationCompleted?.Invoke(isComplete, report);
    }

    private static string BuildReport(
        bool isComplete,
        int installedCount,
        int requiredCount,
        List<string> issues)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(isComplete ? "СБОРКА ПРОШЛА ПРОВЕРКУ" : "СБОРКА НЕ ЗАВЕРШЕНА");
        builder.AppendLine($"Установлено обязательных компонентов: {installedCount} из {requiredCount}.");

        if (issues.Count > 0)
        {
            builder.AppendLine("Что нужно исправить:");
            for (int i = 0; i < issues.Count; i++)
                builder.AppendLine($"{i + 1}. {issues[i]}");
        }
        else if (isComplete)
        {
            builder.AppendLine("Все назначенные обязательные посадочные места заполнены.");
        }

        builder.AppendLine(
            "Примечание: проверка подтверждает заполнение слотов и тип деталей; " +
            "техническая совместимость оборудования будет проверяться отдельным модулем."
        );

        return builder.ToString();
    }
}
