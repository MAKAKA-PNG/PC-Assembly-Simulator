using UnityEngine;

public sealed class CpuSocketLatch : MonoBehaviour
{
    [SerializeField] private Transform hinge;
    [SerializeField] private ComponentSlot board, socket;
    private float angle;
    public bool IsOpen { get; private set; }
    public bool ReadyForCpu => IsOpen && angle >= 85f;
    public bool Locked => !IsOpen && angle <= 0f;

    public string ActionLabel => !board.IsOccupied ? null :
        !socket.IsOccupied && Locked ? "Поднять рычаг сокета" :
        socket.IsOccupied && ReadyForCpu ? "Зафиксировать процессор" : null;

    public void Toggle()
    {
        if (!WorkshopMenu.InputBlocked && ActionLabel != null) IsOpen = !IsOpen;
    }

    public void Restore(bool open)
    {
        IsOpen = open;
        SetAngle(open ? 85f : 0f);
    }

    private void Update()
    {
        float next = Mathf.MoveTowards(angle, IsOpen ? 85f : 0f, Time.deltaTime * 240f);
        if (next != angle) SetAngle(next);
    }

    private void SetAngle(float value)
    {
        angle = value;
        hinge.localRotation = Quaternion.Euler(value, 0, 0);
    }
}
