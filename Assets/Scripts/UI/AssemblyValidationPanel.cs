using TMPro;
using UnityEngine;

/// <summary>Необязательная панель вывода результата AssemblyValidator.</summary>
public sealed class AssemblyValidationPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;

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
            titleText.text = success ? "Сборка завершена" : "Нужно исправить сборку";

        if (detailsText != null)
            detailsText.text = report;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
