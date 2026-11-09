using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Выбирает комплектующую щелчком ЛКМ.
/// Если деталь уже выбрана для установки, передаёт следующий щелчок PlacementController.
/// </summary>
[DisallowMultipleComponent]
public sealed class ComponentSelectionController : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField, Min(1f)] private float maxRayDistance = 100f;

    [Header("Placement")]
    [SerializeField] private PlacementController placementController;

    public event Action<HardwareComponentData> ComponentSelected;
    public event Action SelectionCleared;

    public HardwareComponentView CurrentSelection { get; private set; }

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (placementController == null)
            placementController = GetComponent<PlacementController>();

        if (worldCamera == null)
        {
            Debug.LogError("ComponentSelectionController: не найдена камера. Назначьте World Camera.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (placementController != null && placementController.IsPlacing)
        {
            placementController.HandlePlacementClick();
            return;
        }

        SelectUnderCursor();
    }

    private void SelectUnderCursor()
    {
        Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxRayDistance,
                selectableLayers,
                QueryTriggerInteraction.Ignore))
        {
            HardwareComponentView component =
                hit.collider.GetComponentInParent<HardwareComponentView>();

            if (component != null && component.Data != null && !component.IsInstalled)
            {
                CurrentSelection = component;
                ComponentSelected?.Invoke(component.Data);
                return;
            }
        }

        ClearSelection();
    }

    public void ClearSelection()
    {
        CurrentSelection = null;
        SelectionCleared?.Invoke();
    }
}
