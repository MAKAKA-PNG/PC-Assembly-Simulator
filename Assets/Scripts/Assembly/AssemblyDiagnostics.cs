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

        bool complete = installed && compatible && requiredCables.Length > 0 &&
                        connected == requiredCables.Length;
        var report = new StringBuilder();
        report.AppendLine($"Установка: {filled}/{total}    Кабели: {connected}/{requiredCables.Length}");
        report.AppendLine(compatible ? $"Совместимость подтверждена ({checks} проверок)." :
            $"Совместимость не подтверждена. Проверено: {checks}, пропущено: {skipped.Count}.");
        foreach (string issue in slotIssues)
            report.AppendLine(issue);
        foreach (string error in errors)
            report.AppendLine(error);
        if (missing.Count > 0)
            report.AppendLine("Подключите:\n" + string.Join("\n", missing));
        GetComponent<AssemblyValidationPanel>().ShowResult(complete, report.ToString().TrimEnd());
    }
}
