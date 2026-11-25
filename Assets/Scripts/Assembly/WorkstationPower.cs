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
