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

    private static string CableName(ConnectorType type) => type switch
    {
        ConnectorType.Atx24Pin => "главное питание материнской платы (24 контакта)",
        ConnectorType.CpuEps8Pin => "питание процессора от блока питания",
        ConnectorType.Pcie8Pin => "дополнительное питание видеокарты",
        ConnectorType.CpuFan => "вентилятор процессора к материнской плате",
        ConnectorType.SataPower => "питание накопителя от блока питания",
        ConnectorType.SataData => "кабель данных накопителя к плате",
        ConnectorType.PowerSwitch => "кнопку включения корпуса к плате",
        ConnectorType.ResetSwitch => "кнопку перезагрузки корпуса к плате",
        ConnectorType.PowerLed => "индикатор питания корпуса к плате",
        ConnectorType.DriveLed => "индикатор накопителя к плате",
        ConnectorType.FrontUsb2 => "передние порты USB 2 к плате",
        ConnectorType.FrontUsb3 => "передние порты USB 3 к плате",
        _ => "передние гнёзда наушников и микрофона к плате"
    };

    private int CountCables(out string next)
    {
        int connected = 0;
        next = null;
        foreach (var cable in requiredCables)
        {
            if (cable.Source && cable.Destination && cable.Source.ConnectedPort == cable.Destination &&
                cable.Destination.ConnectedPort == cable.Source) connected++;
            else if (next == null && cable.Source) next = CableName(cable.Source.Type);
        }
        return connected;
    }

    public bool ReadyToBoot() =>
        GetComponent<CaseAssembly>().Finished && CableCount > 0 && CountCables(out _) == CableCount &&
        GetComponent<AssemblyValidator>().EvaluateSlots(out _, out _, out _) &&
        GetComponent<CompatibilityChecker>().Evaluate(out _, out _, out _);

    public void CheckAssembly()
    {
        bool installed = GetComponent<AssemblyValidator>().EvaluateSlots(out int filled, out int total, out var issues);
        bool compatible = GetComponent<CompatibilityChecker>().Evaluate(out _, out var errors, out _);
        var chassis = GetComponent<CaseAssembly>();
        var power = GetComponent<WorkstationPower>();
        int connected = CountCables(out string nextCable);
        bool complete = installed && compatible && connected == CableCount && chassis.Finished && power.Running && power.Connections == 3;
        int score = Mathf.RoundToInt(30f * filled / Mathf.Max(1, total) + 24f * connected / Mathf.Max(1, CableCount) +
            18f * chassis.ScrewCount / chassis.TotalScrews + 3f * chassis.Stage + (compatible ? 9 : 0) +
            2 * power.Connections + (power.Running ? 4 : 0));
        int best = WorkshopProgress.Record(score);
        var report = new StringBuilder();
        report.AppendLine($"<color=#77E4CE><b>{score} / 100</b></color>     Лучший результат  {best}");
        report.AppendLine();
        report.AppendLine($"Комплектующие    <b>{filled} / {total}</b>       Винты    <b>{chassis.ScrewCount} / {chassis.TotalScrews}</b>");
        report.AppendLine($"Кабели внутри корпуса    <b>{connected} / {CableCount}</b>");
        report.AppendLine($"Розетка, монитор и сеть    <b>{power.Connections} / 3</b>");
        report.AppendLine();
        report.AppendLine(complete ? "<color=#77E4CE>ПРОВЕРКА ПРОЙДЕНА</color>" : "<color=#FFD58C>СЛЕДУЮЩЕЕ ДЕЙСТВИЕ</color>");
        if (!installed && issues.Count > 0) report.Append(issues[0]);
        else if (!compatible && errors.Count > 0) report.Append(errors[0]);
        else if (nextCable != null) report.Append("Подключите " + nextCable + ".");
        else if (!complete) report.Append(chassis.NextAction() ?? "Проверьте монтаж деталей.");
        else report.Append("Компьютер загрузился. Изображение выведено на монитор.");
        GetComponent<AssemblyValidationPanel>().ShowResult(complete, report.ToString());
        CheckFinished?.Invoke(complete);
        GetComponent<WorkshopProgress>().Save();
    }
}
