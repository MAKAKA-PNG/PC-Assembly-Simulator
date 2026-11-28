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
    private string hoverName;
    private GUIStyle tooltipStyle;
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
        bool portChanged = HoveredPort != port;
        HoveredPort = port;
        var part = selectable != null ? selectable : component;
        SetHover(port != null ? port.GetComponent<Renderer>() :
            part != null ? part.GetComponent<Renderer>() : null, port != null);
        if (port)
        {
            if (portChanged || hoverName == null) hoverName = port.Label;
        }
        else hoverName = part && part.Data ? part.Data.DisplayName : null;
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
        if (!target) hoverName = null;
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

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(hoverName) || WorkshopMenu.InputBlocked ||
            Mouse.current == null || Event.current.type != EventType.Repaint) return;
        tooltipStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter };
        tooltipStyle.fontSize = Mathf.Clamp(Screen.height / 48, 14, 24);
        var content = new GUIContent(hoverName);
        Vector2 size = tooltipStyle.CalcSize(content) + new Vector2(16, 6);
        Vector2 cursor = Mouse.current.position.ReadValue();
        GUI.Box(new Rect(Mathf.Clamp(cursor.x + 16, 0, Screen.width - size.x),
            Mathf.Clamp(Screen.height - cursor.y + 18, 0, Screen.height - size.y), size.x, size.y),
            content, tooltipStyle);
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
