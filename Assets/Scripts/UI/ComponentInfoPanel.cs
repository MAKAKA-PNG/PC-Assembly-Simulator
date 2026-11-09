using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Показывает информацию о выбранном комплектующем.
/// Разместите этот скрипт на активном объекте Canvas, а PanelRoot — на панели карточки.
/// </summary>
public sealed class ComponentInfoPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComponentSelectionController selectionController;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text specificationsText;
    [SerializeField] private TMP_Text compatibilityText;

    private void OnEnable()
    {
        if (selectionController == null)
            return;

        selectionController.ComponentSelected += Show;
        selectionController.SelectionCleared += Hide;
    }

    private void OnDisable()
    {
        if (selectionController == null)
            return;

        selectionController.ComponentSelected -= Show;
        selectionController.SelectionCleared -= Hide;
    }

    private void Start()
    {
        Hide();
    }

    public void Show(HardwareComponentData data)
    {
        if (data == null)
        {
            Hide();
            return;
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = data.DisplayName;

        if (typeText != null)
            typeText.text = data.ComponentType.ToString();

        if (descriptionText != null)
            descriptionText.text = data.Description;

        if (compatibilityText != null)
            compatibilityText.text = data.CompatibilityNotes;

        if (specificationsText != null)
        {
            StringBuilder builder = new StringBuilder();
            if (data.Specifications != null)
            {
                foreach (string item in data.Specifications)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                        builder.AppendLine("• " + item);
                }
            }

            specificationsText.text = builder.ToString().TrimEnd();
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
