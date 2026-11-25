using System;
using System.Linq;
using UnityEngine;

public sealed class WorkshopProgress : MonoBehaviour
{
    private static string Prefix => Application.isEditor ? "Workshop.Editor.v1." : "Workshop.Player.v1.";
    [Serializable] private class Snapshot
    {
        public string[] parts, sources, destinations;
        public int board, power, side, stage, cooling, gpu, storage;
        public bool practice;
        public bool latchOpen;
    }
    [SerializeField] private ComponentSlot[] slots;
    private bool ready, restarting;
    private WorkshopMenu menu;

    public static int Record(int score)
    {
        int best = Mathf.Max(score, PlayerPrefs.GetInt(Prefix + "Best"));
        PlayerPrefs.SetInt(Prefix + "Best", best);
        PlayerPrefs.Save();
        return best;
    }

    public bool Restore(bool fallback)
    {
        menu = GetComponent<WorkshopMenu>();
        ready = true;
        string json = PlayerPrefs.GetString(Prefix + "Build");
        if (string.IsNullOrEmpty(json)) return fallback;
        Snapshot s;
        try { s = JsonUtility.FromJson<Snapshot>(json); }
        catch (ArgumentException) { return fallback; }
        if (s?.parts == null || s.parts.Length != slots.Length ||
            s.sources == null || s.destinations == null || s.sources.Length != s.destinations.Length)
            return fallback;
        var parts = FindObjectsByType<HardwareComponentView>();
        var latch = GetComponent<CpuSocketLatch>();
        for (int i = 0; i < slots.Length; i++)
        {
            var part = Array.Find(parts, p => p.name == s.parts[i]);
            if (!part) continue;
            bool cpu = slots[i].AcceptedType == HardwareComponentType.Processor;
            if (cpu && latch) latch.Restore(true);
            slots[i].TryInstall(part, out _);
            if (cpu && latch) latch.Restore(s.latchOpen);
        }
        if (latch) latch.Restore(s.latchOpen);
        var ports = FindObjectsByType<ConnectorPort>();
        var manager = GetComponent<ConnectionManager>();
        for (int i = 0; i < s.sources.Length; i++)
            manager.TryConnect(Array.Find(ports, p => p.PortId == s.sources[i]),
                Array.Find(ports, p => p.PortId == s.destinations[i]), out _);
        GetComponent<CaseAssembly>().Restore(s.board, s.power, s.stage, s.side, s.cooling, s.gpu, s.storage);
        return s.practice;
    }

    public void Save()
    {
        if (!ready || restarting) return;
        var manager = GetComponent<ConnectionManager>();
        var chassis = GetComponent<CaseAssembly>();
        var snap = new Snapshot
        {
            parts = slots.Select(s => s.InstalledComponent ? s.InstalledComponent.name : "").ToArray(),
            sources = new string[manager.ConnectionCount], destinations = new string[manager.ConnectionCount],
            board = chassis.BoardCount, power = chassis.PowerCount, cooling = chassis.CoolerCount, gpu = chassis.GraphicsCount, storage = chassis.DriveCount,
            side = chassis.SideCount, stage = chassis.Stage,
            practice = menu.IsPractice,
            latchOpen = chassis.Latch && chassis.Latch.IsOpen
        };
        for (int i = 0; i < manager.ConnectionCount; i++)
            manager.GetPair(i, out snap.sources[i], out snap.destinations[i]);
        PlayerPrefs.SetString(Prefix + "Build", JsonUtility.ToJson(snap));
        PlayerPrefs.Save();
    }

    public void ClearBuild()
    {
        restarting = true;
        PlayerPrefs.DeleteKey(Prefix + "Build");
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused) { if (paused) Save(); }
    private void OnApplicationQuit() => Save();
}
