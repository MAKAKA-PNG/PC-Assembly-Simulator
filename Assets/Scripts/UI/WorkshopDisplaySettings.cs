using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class WorkshopDisplaySettings : MonoBehaviour
{
    [SerializeField] private TMP_Text modeLabel, resolutionLabel, status;
    private const string Prefix = "Workshop.Display.";
    private readonly List<Vector2Int> sizes = new List<Vector2Int>();
    private bool fullscreen;
    private int index;

    private void Start()
    {
        if (Application.isEditor || !PlayerPrefs.HasKey(Prefix + "Width")) return;
        fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen") != 0;
        Populate(new Vector2Int(PlayerPrefs.GetInt(Prefix + "Width"), PlayerPrefs.GetInt(Prefix + "Height")));
        SetPlayerResolution();
    }

    public void ReadCurrent()
    {
        fullscreen = Screen.fullScreen;
        Populate(new Vector2Int(Screen.width, Screen.height));
        status.text = Application.isEditor ? "Размер окна меняется в Windows-версии игры." : "";
    }

    private void Populate(Vector2Int preferred)
    {
        sizes.Clear();
        foreach (var r in Screen.resolutions) Add(r.width, r.height);
        if (!fullscreen)
        {
            foreach (var s in new[] { new Vector2Int(960, 540), new Vector2Int(1280, 720),
                         new Vector2Int(1600, 900), new Vector2Int(1920, 1080) })
                if (s.x <= Display.main.systemWidth && s.y <= Display.main.systemHeight)
                    Add(s.x, s.y);
            Add(Screen.width, Screen.height);
        }
        if (sizes.Count == 0) Add(Screen.width, Screen.height);
        sizes.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        index = 0;
        for (int i = 1; i < sizes.Count; i++)
            if ((sizes[i] - preferred).sqrMagnitude < (sizes[index] - preferred).sqrMagnitude)
                index = i;
        UpdateLabels();
    }

    private void Add(int width, int height)
    {
        var size = new Vector2Int(width, height);
        if (width > 0 && height > 0 && !sizes.Contains(size)) sizes.Add(size);
    }

    public void ToggleFullscreen()
    {
        fullscreen = !fullscreen;
        Populate(sizes[index]);
        status.text = "Нажмите «Применить», чтобы изменить экран.";
    }

    public void NextResolution() => Cycle(1);
    public void PreviousResolution() => Cycle(-1);

    private void Cycle(int delta)
    {
        index = (index + delta + sizes.Count) % sizes.Count;
        UpdateLabels();
        status.text = "Нажмите «Применить», чтобы изменить экран.";
    }

    private void UpdateLabels()
    {
        modeLabel.text = fullscreen ? "Полный экран" : "В окне";
        resolutionLabel.text = $"{sizes[index].x} × {sizes[index].y}";
    }

    public void Apply()
    {
        if (Application.isEditor)
        {
            status.text = "В редакторе размер задаёт Game View. Настройка доступна в игре.";
            return;
        }
        SetPlayerResolution();
        PlayerPrefs.SetInt(Prefix + "Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "Width", sizes[index].x);
        PlayerPrefs.SetInt(Prefix + "Height", sizes[index].y);
        PlayerPrefs.Save();
        status.text = "Настройки экрана сохранены.";
    }

    private void SetPlayerResolution()
    {
        Screen.SetResolution(sizes[index].x, sizes[index].y,
            fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
    }
}
