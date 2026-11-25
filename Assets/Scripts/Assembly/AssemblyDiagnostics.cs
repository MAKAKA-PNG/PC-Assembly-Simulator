using System;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AssemblyValidator), typeof(CompatibilityChecker), typeof(AssemblyValidationPanel))]
public sealed class AssemblyDiagnostics : MonoBehaviour
{
    [Serializable]
    private struct Cable
    {
        public ConnectorPort Source;
        public ConnectorPort Destination;
    }

    [SerializeField] private Cable[] requiredCables = Array.Empty<Cable>();
    public int CableCount => requiredCables.Length;
    public event Action<bool> CheckFinished;

    public void CheckAssembly()
    {
        bool installed = GetComponent<AssemblyValidator>().EvaluateSlots(
            out int filled, out int total, out var slotIssues);
        bool compatible = GetComponent<CompatibilityChecker>().Evaluate(
            out int checks, out var errors, out var skipped);

        int connected = 0;
        var missing = new System.Collections.Generic.List<string>();
        foreach (var cable in requiredCables)
        {
            if (cable.Source != null && cable.Destination != null &&
                cable.Source.ConnectedPort == cable.Destination &&
                cable.Destination.ConnectedPort == cable.Source)
                connected++;
            else
                missing.Add($"{cable.Source?.PortId ?? "?"} → {cable.Destination?.PortId ?? "?"}");
        }

        var chassis = GetComponent<CaseAssembly>();
        bool complete = chassis.Finished && installed && compatible && requiredCables.Length > 0 &&
                        connected == requiredCables.Length;
        var report = new StringBuilder();
        int score = Mathf.RoundToInt(32f * filled / Mathf.Max(1, total) +
            24f * connected / Mathf.Max(1, requiredCables.Length)) +
            Mathf.RoundToInt(16f * chassis.ScrewCount / chassis.TotalScrews) + 6 * chassis.Stage + (compatible ? 10 : 0);
        int best = WorkshopProgress.Record(score);
        report.AppendLine($"Результат: {score}/100    Лучший: {best}/100");
        report.AppendLine($"Установка: {filled}/{total}    Кабели: {connected}/{requiredCables.Length}");
        report.AppendLine($"Винты платы: {chassis.BoardCount}/4    Винты БП: {chassis.PowerCount}/4    Крышка: {chassis.SideCount}/4");
        report.AppendLine($"Кулер: {chassis.CoolerCount}/4    Видеокарта: {chassis.GraphicsCount}/1    SSD: {chassis.DriveCount}/4");
        if (!chassis.Finished) report.AppendLine(chassis.NextAction() ?? "Завершите монтаж и поставьте корпус вертикально.");
        report.AppendLine(compatible ? $"Совместимость подтверждена ({checks} проверок)." :
            $"Совместимость не подтверждена. Проверено: {checks}, пропущено: {skipped.Count}.");
        foreach (string issue in slotIssues)
            report.AppendLine(issue);
        foreach (string error in errors)
            report.AppendLine(error);
        if (missing.Count > 0)
            report.AppendLine("Подключите:\n" + string.Join("\n", missing));
        GetComponent<AssemblyValidationPanel>().ShowResult(complete, report.ToString().TrimEnd());
        CheckFinished?.Invoke(complete);
        GetComponent<WorkshopProgress>().Save();
    }
}
