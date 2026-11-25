using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class WorkstationPower : MonoBehaviour
{
    [SerializeField] private Transform[] mains, video, ethernet;
    [SerializeField] private Material jacket;
    [SerializeField] private Collider caseSwitch;
    [SerializeField] private GameObject screen;
    [SerializeField] private TMP_Text screenText;
    [SerializeField] private Transform workstation, monitorSocket;
    private bool moved;
    public void PresentWorkstation()
    {
        if (moved || !workstation) return;
        Vector3 delta = new Vector3(-2.70f, workstation.position.y, -.57f) - workstation.position;
        workstation.position += delta;
        monitorSocket.position += delta;
        // Follow the rear of the case to the tabletop, then approach the monitor from behind.
        Vector3 end = monitorSocket.position;
        var low = video[2].position;
        video[3].position = new Vector3(low.x, low.y, end.z + .09f);
        video[4].position = new Vector3(end.x + .16f, low.y, end.z + .09f);
        video[5].position = new Vector3(end.x, low.y, end.z + .09f);
        video[6].position = new Vector3(end.x, end.y, end.z + .09f);
        video[7].position = end;
        moved = true;
    }
    private CaseAssembly chassis;
    private AssemblyDiagnostics diagnostics;
    public int Connections { get; private set; }
    public bool Running { get; private set; }
    public string NextAction => Connections switch
    {
        0 => "Подключить питание к розетке",
        1 => "Подключить монитор к видеокарте",
        2 => "Подключить сетевой кабель",
        _ => Running ? null : "Нажать кнопку питания"
    };

    private void Awake()
    {
        chassis = GetComponent<CaseAssembly>();
        diagnostics = GetComponent<AssemblyDiagnostics>();
        screen.SetActive(false);
    }

    private void Update()
    {
        if (WorkshopMenu.InputBlocked || !chassis.Finished || Running || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame ||
            (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
        if (caseSwitch.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out _, 100))
            StartComputer();
    }

    public void Advance()
    {
        if (!chassis.Finished) return;
        if (Connections < 3)
        {
            AttachCable();
            if (Running) ShowDesktop();
        }
        else StartComputer();
    }

    private void AttachCable()
    {
        Transform[] route = Connections == 0 ? mains : Connections == 1 ? video : ethernet;
        var cable = new GameObject(route[0].name + " cable");
        cable.transform.SetParent(transform, false);
        var points = new List<Vector3>(route.Length);
        foreach (var anchor in route) points.Add(cable.transform.InverseTransformPoint(anchor.position));
        cable.AddComponent<RoutedCable>().FitFixedRoute(points, jacket, Connections == 0 ? .024f : .017f);
        Connections++;
    }

    private void StartComputer()
    {
        if (Running) return;
        if (!chassis.Finished || Connections < 2 || !diagnostics.ReadyToBoot())
        {
            diagnostics.CheckAssembly();
            return;
        }
        Running = true;
        ShowDesktop();
        GetComponent<WorkshopProgress>().Save();
    }

    private void ShowDesktop()
    {
        screen.SetActive(true);
        screenText.text = "PC  /  SYSTEM ONLINE\n\nПроверка оборудования пройдена\nСистема загружена\n" +
            (Connections == 3 ? "Локальная сеть подключена" : "Автономный режим");
    }

    public void Restore(int connected, bool running)
    {
        if (!chassis.Finished) return;
        while (Connections < Mathf.Clamp(connected, 0, 3)) AttachCable();
        Running = running && Connections >= 2 && diagnostics.ReadyToBoot();
        if (Running) ShowDesktop();
    }
}
