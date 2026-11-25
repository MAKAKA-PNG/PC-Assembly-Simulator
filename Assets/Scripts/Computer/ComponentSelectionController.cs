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
    public ConnectorPort HoveredPort { get; private set; }
    private RaycastHit[] hitBuffer = new RaycastHit[16];
    private Renderer hoveredRenderer;
    private MaterialPropertyBlock previousProperties;
    private MaterialPropertyBlock hoverProperties;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        previousProperties = new MaterialPropertyBlock();
        hoverProperties = new MaterialPropertyBlock();
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
        if (WorkshopMenu.InputBlocked) return;
        if (Mouse.current == null ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            SetHover(null, false);
            return;
        }

        if (placementController != null && placementController.IsPlacing)
        {
            SetHover(null, false);
            if (Mouse.current.leftButton.wasPressedThisFrame)
                placementController.HandlePlacementClick();
            return;
        }

        UpdatePointer(Mouse.current.leftButton.wasPressedThisFrame);
    }

    private void UpdatePointer(bool select)
    {
        ResolvePointer(out var port, out var component, out var selectable);
        HoveredPort = port;
        var part = selectable != null ? selectable : component;
        SetHover(port != null ? port.GetComponent<Renderer>() :
            part != null ? part.GetComponent<Renderer>() : null, port != null);
        if (select && port == null)
            SelectComponent(selectable);
    }

    private void ResolvePointer(out ConnectorPort port, out HardwareComponentView component,
        out HardwareComponentView selectable)
    {
        Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        int count;
        while ((count = Physics.RaycastNonAlloc(ray, hitBuffer, maxRayDistance,
                   selectableLayers, QueryTriggerInteraction.Ignore)) == hitBuffer.Length)
            Array.Resize(ref hitBuffer, hitBuffer.Length * 2);

        port = null;
        component = selectable = null;
        float portDistance = float.PositiveInfinity;
        float componentDistance = float.PositiveInfinity;
        float selectableDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            var candidate = hit.collider.GetComponentInParent<ConnectorPort>();
            if (candidate != null && hit.distance < portDistance)
            {
                port = candidate;
                portDistance = hit.distance;
            }
            var view = hit.collider.GetComponentInParent<HardwareComponentView>();
            if (view == null || view.Data == null)
                continue;
            if (hit.distance < componentDistance)
            {
                component = view;
                componentDistance = hit.distance;
            }
            if (!view.IsInstalled && hit.distance < selectableDistance)
            {
                selectable = view;
                selectableDistance = hit.distance;
            }
        }
    }

    private void SelectComponent(HardwareComponentView component)
    {
        if (component == null)
            ClearSelection();
        else
        {
            CurrentSelection = component;
            ComponentSelected?.Invoke(component.Data);
        }
    }

    private void SetHover(Renderer target, bool isPort)
    {
        if (!isPort) HoveredPort = null;
        if (hoveredRenderer == target)
            return;
        if (hoveredRenderer != null)
            hoveredRenderer.SetPropertyBlock(previousProperties.isEmpty ? null : previousProperties);
        hoveredRenderer = target;
        if (target == null)
            return;
        target.GetPropertyBlock(previousProperties);
        target.GetPropertyBlock(hoverProperties);
        hoverProperties.SetColor(BaseColor, isPort ? new Color(1f, .58f, .08f) :
            new Color(.20f, .84f, .91f));
        target.SetPropertyBlock(hoverProperties);
    }

    private void OnDisable()
    {
        SetHover(null, false);
    }

    public void ClearSelection()
    {
        CurrentSelection = null;
        SelectionCleared?.Invoke();
    }
}
