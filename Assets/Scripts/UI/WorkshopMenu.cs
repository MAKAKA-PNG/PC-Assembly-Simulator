using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public sealed class WorkshopMenu : MonoBehaviour
{
    [SerializeField] private GameObject overlay, mainPage, settingsPage, restartPage;
    [SerializeField] private CanvasGroup hud;
    [SerializeField] private GameObject hintsToggle;
    [SerializeField] private TMP_Text modeAction, practiceStatus;
    [SerializeField] private WorkshopDisplaySettings display;
    [SerializeField] private GameObject titlePage;
    private bool onTitle;
    private static bool restartingScene;
    private AssemblyTutorial tutorial;
    private AssemblyValidationPanel report;
    private PlacementController placement;
    private AssemblyValidator validator;
    private ConnectionManager connections;
    private bool practice, paused;
    private float previousTimeScale;
    private static int suppressFrame = -1;
    private static bool restartInPractice;
    public static bool InputBlocked => IsPaused || Time.frameCount == suppressFrame ||
        (CaseAssembly.OverScrew && Mouse.current != null && Mouse.current.leftButton.isPressed);
    public static bool IsPaused { get; private set; }
    public bool IsPractice => practice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsPaused = restartInPractice = restartingScene = false;
        suppressFrame = -1;
    }

    private void Start()
    {
        tutorial = GetComponent<AssemblyTutorial>();
        report = GetComponent<AssemblyValidationPanel>();
        placement = GetComponent<PlacementController>();
        validator = GetComponent<AssemblyValidator>();
        connections = GetComponent<ConnectionManager>();
        ApplyMode(GetComponent<WorkshopProgress>().Restore(restartInPractice));
        restartInPractice = false;
        if (!restartingScene) ShowTitle();
        restartingScene = false;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (!paused)
        {
            if (!report.Visible && !placement.IsPlacing) Open();
        }
        else if (onTitle) Page(titlePage);
        else if (!mainPage.activeSelf) ShowMain();
        else Resume();
    }

    public void Open()
    {
        if (paused) return;
        previousTimeScale = Time.timeScale;
        GetComponent<WorkshopProgress>().Save();
        paused = IsPaused = true;
        Time.timeScale = 0;
        hud.interactable = hud.blocksRaycasts = false;
        overlay.SetActive(true);
        ShowMain();
    }

    public void Resume()
    {
        if (!paused) return;
        paused = IsPaused = false;
        Time.timeScale = previousTimeScale;
        suppressFrame = Time.frameCount;
        if (overlay) overlay.SetActive(false);
        if (hud) hud.interactable = hud.blocksRaycasts = true;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void Page(GameObject active)
    {
        titlePage.SetActive(active == titlePage);
        mainPage.SetActive(active == mainPage);
        settingsPage.SetActive(active == settingsPage);
        restartPage.SetActive(active == restartPage);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void ShowMain() => Page(onTitle ? titlePage : mainPage);
    public void ShowTitle()
    {
        Open();
        onTitle = true;
        hud.alpha = 0;
        Page(titlePage);
    }

    public void Learn() => Enter(false);
    public void Practice() => Enter(true);

    private void Enter(bool independent)
    {
        onTitle = false;
        hud.alpha = 1;
        ApplyMode(independent);
        Resume();
    }

    public void Quit()
    {
        GetComponent<WorkshopProgress>().Save();
        Application.Quit();
    }
    public void ShowRestart() => Page(restartPage);
    public void ShowSettings()
    {
        Page(settingsPage);
        display.ReadCurrent();
    }

    public void ToggleMode()
    {
        ApplyMode(!practice);
        Resume();
    }

    private void ApplyMode(bool independent)
    {
        practice = independent;
        tutorial.SetPractice(practice);
        hintsToggle.SetActive(!practice);
        practiceStatus.transform.parent.gameObject.SetActive(practice);
        modeAction.text = "Главное меню";
    }

    private void LateUpdate()
    {
        if (!practice || paused) return;
        validator.CountSlots(out int filled, out int total);
        practiceStatus.SetText("Практика   ·   Детали {0}/{1}   ·   Кабели {2}/{3}",
            filled, total, connections.ConnectionCount, GetComponent<AssemblyDiagnostics>().CableCount);
    }

    public void Restart()
    {
        restartingScene = true;
        restartInPractice = practice;
        GetComponent<WorkshopProgress>().ClearBuild();
        Resume();
        SceneManager.LoadScene(gameObject.scene.buildIndex);
    }

    private void OnDisable()
    {
        if (paused) Resume();
    }
}
