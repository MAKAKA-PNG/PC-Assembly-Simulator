using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Необязательная панель вывода результата AssemblyValidator.</summary>
public sealed class AssemblyValidationPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    public bool Visible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        // Этот компонент должен находиться на активном объекте (например, UIManager).
        // Скрываем только дочернюю панель, а не сам объект со скриптом.
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowResult(bool success, string report)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = success ? "Компьютер готов к работе" : "Проверка компьютера";

        if (detailsText != null)
            detailsText.text = report;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowCompatibility(bool compatible, System.Collections.Generic.List<string> errors, int skipped)
    {
        if (detailsText == null)
            return;

        detailsText.text += compatible
            ? "\nСовместимость проверена."
            : "\nСовместимость не подтверждена.";
        foreach (string error in errors)
            detailsText.text += "\n• " + error;
        if (skipped > 0)
            detailsText.text += $"\nПропущено проверок: {skipped}.";
    }

    private void Update()
    {
        if (WorkshopMenu.InputBlocked) return;
        if (panelRoot != null && panelRoot.activeSelf &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Hide();
    }
}
