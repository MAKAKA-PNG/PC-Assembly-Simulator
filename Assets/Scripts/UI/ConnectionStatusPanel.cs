using TMPro;
using UnityEngine;

/// <summary>Необязательная панель сообщений о подключении кабелей.</summary>
public sealed class ConnectionStatusPanel : MonoBehaviour
{
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text countText;

    private void OnEnable()
    {
        if (connectionManager == null) return;
        connectionManager.StatusChanged += ShowStatus;
        connectionManager.ConnectionCountChanged += ShowCount;
        ShowCount(connectionManager.ConnectionCount);
    }

    private void OnDisable()
    {
        if (connectionManager == null) return;
        connectionManager.StatusChanged -= ShowStatus;
        connectionManager.ConnectionCountChanged -= ShowCount;
    }

    private void ShowStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void ShowCount(int count)
    {
        if (countText != null) countText.text = $"Подключено кабелей: {count}";
    }
}
