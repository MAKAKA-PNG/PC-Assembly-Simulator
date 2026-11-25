using System;
using TMPro;
using UnityEngine;

public sealed class AssemblyTutorial : MonoBehaviour
{
    [Serializable]
    private struct Step
    {
        public string Instruction;
        public ComponentSlot Slot;
        public ConnectorPort First;
        public ConnectorPort Second;

        public bool Done => Slot != null ? Slot.IsOccupied :
            First != null && Second != null && First.ConnectedPort == Second && Second.ConnectedPort == First;
    }

    [SerializeField] private Step[] steps;
    [SerializeField] private GameObject card;
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text hint;
    [SerializeField] private TMP_Text toggleLabel;
    private PlacementController placement;
    private ConnectionManager connections;
    private AssemblyDiagnostics diagnostics;
    private ComponentSelectionController selection;
    private ConnectorPort shownPort;
    private int shown = -1;
    private string message;
    private float messageUntil;
    private bool passed;
    private CaseAssembly chassis;
    private bool hintsBeforePractice = true;

    public void SetPractice(bool practice)
    {
        if (practice)
        {
            if (enabled) hintsBeforePractice = card.activeSelf;
            card.SetActive(false);
        }
        else
            card.SetActive(hintsBeforePractice);
        enabled = !practice;
        shown = -1;
    }

    private void Awake()
    {
        placement = GetComponent<PlacementController>();
        connections = GetComponent<ConnectionManager>();
        diagnostics = GetComponent<AssemblyDiagnostics>();
        selection = GetComponent<ComponentSelectionController>();
        chassis = GetComponent<CaseAssembly>();
    }

    private void OnEnable()
    {
        placement.PlacementRejected += Feedback;
        connections.StatusChanged += Feedback;
        diagnostics.CheckFinished += Checked;
    }

    private void OnDisable()
    {
        placement.PlacementRejected -= Feedback;
        connections.StatusChanged -= Feedback;
        diagnostics.CheckFinished -= Checked;
    }

    public void ToggleHints()
    {
        card.SetActive(!card.activeSelf);
        toggleLabel.text = card.activeSelf ? "Скрыть подсказки" : "Обучение";
        shown = -1;
    }

    private void Feedback(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 3f;
    }

    private void Checked(bool success)
    {
        passed = success;
        shown = -1;
        if (success) messageUntil = 0;
        if (!success) Feedback("Посмотрите замечания в отчёте. Esc закрывает его.");
    }

    private void LateUpdate()
    {
        int index = 0;
        while (index < steps.Length && steps[index].Done) index++;
        if (index < steps.Length || !chassis.Finished) passed = false;
        if (!card.activeSelf) return;
        var port = selection.HoveredPort;
        int phase = index == steps.Length && chassis.Finished ? index + 1 : index;
        if (shown != phase || port != shownPort)
        {
            if (shown >= 0 && shown != phase) messageUntil = 0;
            heading.text = passed ? "Обучение завершено" :
                $"Шаг {phase + 1} из {steps.Length + 2}" + (port != null ? $"   ·   {port.PortId}" : "");
            shown = phase;
            shownPort = port;
        }

        string text = index < steps.Length ? steps[index].Instruction :
            passed ? "ПК собран, кабели подключены, совместимость подтверждена." :
            !chassis.Finished ? chassis.NextAction() ?? "Завершите крепление корпуса." :
            chassis.NextAction() ?? "Нажмите «Диагностика ПК», чтобы получить результат и оценку.";
        if (Time.unscaledTime < messageUntil) text = message;
        else if ((index == 1 || index == 2) && chassis.Latch && chassis.Latch.ActionLabel != null)
            text = "Нажмите «" + chassis.Latch.ActionLabel + "».";
        if (hint.text != text) hint.text = text;
    }
}
