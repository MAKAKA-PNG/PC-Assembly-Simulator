using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine;

[DefaultExecutionOrder(-75)]
public sealed class CaseAssembly : MonoBehaviour
{
    [SerializeField] private Transform rig, front, cover;
    [SerializeField] private ComponentSlot motherboard, psu;
    [SerializeField] private Transform[] boardScrews, psuScrews;
    [SerializeField] private GameObject action;
    [SerializeField] private TMP_Text label;
    private AssemblyValidator slots;
    private ConnectionManager cables;
    [SerializeField] private Transform[] sideScrews;
    [SerializeField] private ComponentSlot cooler, graphics, drive;
    [SerializeField] private Transform[] coolerScrews, graphicsScrews, driveScrews;
    private Camera worldCamera;
    private AssemblyDiagnostics diagnostics;
    public int CoolerCount { get; private set; }
    public int GraphicsCount { get; private set; }
    public int DriveCount { get; private set; }
    public int TotalScrews => boardScrews.Length + psuScrews.Length + sideScrews.Length +
        coolerScrews.Length + graphicsScrews.Length + driveScrews.Length;
    private Vector2 marker;
    private bool markerVisible;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    public static bool OverScrew { get; private set; }

    public int SideCount { get; private set; }
    private Transform turning;
    private float turn;
    public CpuSocketLatch Latch { get; private set; }
    public int BoardCount { get; private set; }
    public int PowerCount { get; private set; }
    public int Stage { get; private set; }
    public int ScrewCount => BoardCount + PowerCount + SideCount + CoolerCount + GraphicsCount + DriveCount;
    public bool Finished => Stage == 3;
    private bool BoardReady => motherboard.IsOccupied && BoardCount < boardScrews.Length;
    private bool PowerReady => psu.IsOccupied && PowerCount < psuScrews.Length;

    private void Awake()
    {
        worldCamera = Camera.main;
        diagnostics = GetComponent<AssemblyDiagnostics>();
        slots = GetComponent<AssemblyValidator>();
        cables = GetComponent<ConnectionManager>();
        Latch = GetComponent<CpuSocketLatch>();
    }

    public string NextAction()
    {
        if (Latch && Latch.ActionLabel != null) return Latch.ActionLabel;
        if (BoardReady) return "Винт платы · удерживайте ЛКМ на винте";
        if (PowerReady) return "Винт БП · удерживайте ЛКМ на винте";
        if (cooler.IsOccupied && CoolerCount < coolerScrews.Length) return "Винт кулера · удерживайте ЛКМ на винте";
        if (graphics.IsOccupied && GraphicsCount < graphicsScrews.Length) return "Винт видеокарты · удерживайте ЛКМ на винте";
        if (drive.IsOccupied && DriveCount < driveScrews.Length) return "Винт SSD · удерживайте ЛКМ на винте";
        if (Stage == 2 && SideCount < sideScrews.Length) return "Винт крышки · удерживайте ЛКМ на винте";
        slots.CountSlots(out int filled, out int total);
        if (filled != total || cables.ConnectionCount != diagnostics.CableCount || BoardCount + PowerCount != 8) return null;
        return Stage switch
        {
            0 => "Установить переднюю панель",
            1 => "Закрыть боковую крышку",
            2 => "Поставить корпус вертикально",
            _ => null
        };
    }

    private void Update()
    {
        OverScrew = markerVisible = false;
        if (WorkshopMenu.IsPaused) return;
        Transform[] group = null;
        int index = 0;
        if (BoardReady) { group = boardScrews; index = BoardCount; }
        else if (PowerReady) { group = psuScrews; index = PowerCount; }
        else if (cooler.IsOccupied && CoolerCount < coolerScrews.Length) { group = coolerScrews; index = CoolerCount; }
        else if (graphics.IsOccupied && GraphicsCount < graphicsScrews.Length) { group = graphicsScrews; index = GraphicsCount; }
        else if (drive.IsOccupied && DriveCount < driveScrews.Length) { group = driveScrews; index = DriveCount; }
        else if (Stage == 2 && SideCount < sideScrews.Length) { group = sideScrews; index = SideCount; }
        Transform target = group == null ? null : group[index];
        if (target != turning)
        {
            if (turning)
            {
                turning.localPosition = savedPosition;
                turning.localRotation = savedRotation;
                turning.gameObject.SetActive(false);
            }
            turning = target;
            if (target)
            {
                savedPosition = target.localPosition;
                savedRotation = target.localRotation;
                turn = 0;
                target.gameObject.SetActive(true);
                target.localPosition += savedRotation * Vector3.up * .075f;
            }
        }
        if (turning && worldCamera && Mouse.current != null)
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(turning.position);
            marker = screen;
            markerVisible = screen.z > 0;
            OverScrew = markerVisible && Vector2.Distance(Mouse.current.position.ReadValue(), marker) < Mathf.Max(14, Screen.height * .024f)
                && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject());
            if (OverScrew && Mouse.current.leftButton.isPressed)
            {
                turn = Mathf.Min(1, turn + Time.deltaTime * 1.4f);
                turning.localPosition = savedPosition + savedRotation * Vector3.up * (.075f * (1 - turn));
                turning.localRotation = savedRotation * Quaternion.AngleAxis(turn * 720f, Vector3.up);
                if (turn >= 1)
                {
                    if (group == boardScrews) BoardCount++;
                    else if (group == psuScrews) PowerCount++;
                    else if (group == coolerScrews) CoolerCount++;
                    else if (group == graphicsScrews) GraphicsCount++;
                    else if (group == driveScrews) DriveCount++;
                    else SideCount++;
                    turning = null;
                }
            }
        }
        string next = NextAction();
        action.SetActive(next != null);
        if (next != null && label.text != next) label.text = next;
    }

    private void OnGUI()
    {
        if (!markerVisible || WorkshopMenu.IsPaused || Event.current.type != EventType.Repaint) return;
        Color old = GUI.color;
        GUI.color = OverScrew ? new Color(.32f,1f,.68f) : new Color(1f,.78f,.27f);
        float x = marker.x, y = Screen.height - marker.y;
        GUI.DrawTexture(new Rect(x-14,y-8,2,16),Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x+12,y-8,2,16),Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x-14,y+10,28*turn,3),Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void OnDisable() { OverScrew = false; }

    public void Advance()
    {
        if (WorkshopMenu.InputBlocked || NextAction() == null) return;
        if (Latch && Latch.ActionLabel != null)
        {
            Latch.Toggle();
            return;
        }
        if (ScrewCount - SideCount < TotalScrews - sideScrews.Length || (Stage == 2 && SideCount < sideScrews.Length)) return;
        ApplyStage(Stage + 1);
    }

    public void Restore(int board, int power, int stage, int side = 0, int cooling = 0, int gpu = 0, int storage = 0)
    {
        BoardCount = motherboard.IsOccupied ? Mathf.Clamp(board, 0, boardScrews.Length) : 0;
        PowerCount = psu.IsOccupied ? Mathf.Clamp(power, 0, psuScrews.Length) : 0;
        for (int i = 0; i < BoardCount; i++) boardScrews[i].gameObject.SetActive(true);
        for (int i = 0; i < PowerCount; i++) psuScrews[i].gameObject.SetActive(true);
        int RestoreGroup(ComponentSlot slot, Transform[] screws, int count)
        {
            int installed = slot.IsOccupied ? Mathf.Clamp(count, 0, screws.Length) : 0;
            for (int i = 0; i < installed; i++) screws[i].gameObject.SetActive(true);
            return installed;
        }
        CoolerCount = RestoreGroup(cooler, coolerScrews, cooling);
        GraphicsCount = RestoreGroup(graphics, graphicsScrews, gpu);
        DriveCount = RestoreGroup(drive, driveScrews, storage);
        if (ScrewCount == TotalScrews - sideScrews.Length && cables.ConnectionCount == diagnostics.CableCount)
        {
            SideCount = Mathf.Clamp(side, 0, sideScrews.Length);
            for (int i = 0; i < SideCount; i++) sideScrews[i].gameObject.SetActive(true);
            ApplyStage(Mathf.Clamp(stage, 0, SideCount == sideScrews.Length ? 3 : 2));
        }
    }

    private void ApplyStage(int stage)
    {
        front.gameObject.SetActive(stage >= 1);
        cover.gameObject.SetActive(stage >= 2);
        if (stage == 3 && Stage != 3)
        {
            rig.rotation = Quaternion.Euler(0, -60, -90);
            float bottom = float.PositiveInfinity;
            foreach (var renderer in rig.GetComponentsInChildren<Renderer>())
                if (renderer.enabled) bottom = Mathf.Min(bottom, renderer.bounds.min.y);
            rig.position += Vector3.up * (.149f - bottom);
            Physics.SyncTransforms();
        }
        Stage = stage;
    }
}
