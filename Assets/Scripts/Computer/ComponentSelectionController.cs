using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Выбирает комплектующую. Щелчки по ConnectorPort оставляет ConnectionManager.
/// Если идёт установка детали, передаёт щелчок PlacementController.
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
        if (worldCamera == null) worldCamera = Camera.main;
        if (placementController == null) placementController = GetComponent<PlacementController>();

        if (worldCamera == null)
        {
            Debug.LogError("ComponentSelectionController: назначьте World Camera.", this);
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
        RaycastHit[] hits = Physics.RaycastAll(
            ray, maxRayDistance, selectableLayers, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Сначала ищем разъём среди ВСЕХ попаданий луча.
        // Это важно, когда Collider материнской платы расположен перед
        // маленьким Collider разъёма и обычный Raycast попадает в плату.
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<ConnectorPort>() != null)
            {
                // Щелчок предназначен ConnectionManager. Не выбираем владельца разъёма.
                return;
            }
        }

        // Если разъём не найден, выбираем ближайшую комплектующую.
        foreach (RaycastHit hit in hits)
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
