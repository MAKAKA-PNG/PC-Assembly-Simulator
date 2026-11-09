using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управляет выбором комплектующей и её установкой в совместимый слот.
/// Первый щелчок выбирает деталь. Повторный щелчок по слоту устанавливает её.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComponentSelectionController selectionController;
    [SerializeField] private Camera worldCamera;

    [Header("Slot Raycast")]
    [SerializeField] private LayerMask slotLayers;
    [SerializeField, Min(1f)] private float maxRayDistance = 100f;

    private HardwareComponentView selectedComponent;
    private ComponentSlot hoveredSlot;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool previewApplied;

    public bool IsPlacing => selectedComponent != null;
    public HardwareComponentView SelectedComponent => selectedComponent;

    private void Awake()
    {
        if (selectionController == null)
            selectionController = GetComponent<ComponentSelectionController>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (selectionController == null)
            Debug.LogError("PlacementController: назначьте ComponentSelectionController.", this);

        if (worldCamera == null)
            Debug.LogError("PlacementController: назначьте World Camera.", this);
    }

    private void OnEnable()
    {
        if (selectionController == null)
            return;

        selectionController.ComponentSelected += OnComponentSelected;
        selectionController.SelectionCleared += OnSelectionCleared;
    }

    private void OnDisable()
    {
        if (selectionController != null)
        {
            selectionController.ComponentSelected -= OnComponentSelected;
            selectionController.SelectionCleared -= OnSelectionCleared;
        }

        RestoreOriginalTransform();
    }

    private void Update()
    {
        if (!IsPlacing)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
            return;
        }

        RefreshHoveredSlot();
    }

    private void OnComponentSelected(HardwareComponentData data)
    {
        RestoreOriginalTransform();

        selectedComponent = selectionController != null
            ? selectionController.CurrentSelection
            : null;

        hoveredSlot = null;
        previewApplied = false;

        if (selectedComponent == null)
            return;

        originalPosition = selectedComponent.transform.position;
        originalRotation = selectedComponent.transform.rotation;

        Debug.Log(
            $"Выбрано: {data.DisplayName}. Наведите курсор на совместимый слот и нажмите ЛКМ. Esc — отмена.",
            selectedComponent
        );
    }

    private void OnSelectionCleared()
    {
        RestoreOriginalTransform();
        selectedComponent = null;
        hoveredSlot = null;
        previewApplied = false;
    }

    /// <summary>
    /// Вызывается ComponentSelectionController вместо обычного выбора,
    /// когда пользователь уже выбрал деталь и находится в режиме установки.
    /// </summary>
    public void HandlePlacementClick()
    {
        if (!IsPlacing || worldCamera == null || Mouse.current == null)
            return;

        // Обновляем слот по текущей позиции мыши, не полагаясь на порядок Update().
        RefreshHoveredSlot();

        if (hoveredSlot == null)
        {
            Debug.Log("Наведите курсор на посадочное место. Esc — отмена установки.");
            return;
        }

        if (!hoveredSlot.TryInstall(selectedComponent, out string reason))
        {
            Debug.LogWarning(reason, hoveredSlot);
            return;
        }

        Debug.Log(
            $"Комплектующая {selectedComponent.Data.DisplayName} установлена в слот {hoveredSlot.SlotId}.",
            selectedComponent
        );

        // Сбрасываем временное состояние до события SelectionCleared,
        // чтобы установленная деталь не вернулась на исходную позицию.
        selectedComponent = null;
        hoveredSlot = null;
        previewApplied = false;

        if (selectionController != null)
            selectionController.ClearSelection();
    }

    public void CancelPlacement()
    {
        if (!IsPlacing)
            return;

        RestoreOriginalTransform();
        selectedComponent = null;
        hoveredSlot = null;
        previewApplied = false;

        if (selectionController != null)
            selectionController.ClearSelection();

        Debug.Log("Установка отменена.");
    }

    private void RefreshHoveredSlot()
    {
        if (worldCamera == null || Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = worldCamera.ScreenPointToRay(mousePosition);

        ComponentSlot nextSlot = null;
        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxRayDistance,
                slotLayers,
                QueryTriggerInteraction.Collide))
        {
            nextSlot = hit.collider.GetComponentInParent<ComponentSlot>();
        }

        if (nextSlot == hoveredSlot)
            return;

        RestoreOriginalTransform();
        hoveredSlot = nextSlot;

        if (hoveredSlot != null &&
            hoveredSlot.CanAccept(selectedComponent, out _))
        {
            Transform snap = hoveredSlot.SnapPoint;
            selectedComponent.transform.SetPositionAndRotation(
                snap.position,
                snap.rotation
            );
            previewApplied = true;
        }
    }

    private void RestoreOriginalTransform()
    {
        if (!previewApplied || selectedComponent == null)
            return;

        selectedComponent.transform.SetPositionAndRotation(
            originalPosition,
            originalRotation
        );

        previewApplied = false;
    }
}
