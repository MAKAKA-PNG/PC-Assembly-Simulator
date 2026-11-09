using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Свободная орбитальная камера для осмотра сборочного стенда.
/// ПКМ + движение мыши — вращение; колесо — приближение;
/// СКМ + движение мыши — панорамирование.
/// </summary>
[DisallowMultipleComponent]
public sealed class OrbitCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.8f, 0f);

    [Header("Orbit")]
    [SerializeField, Min(0.01f)] private float rotationSensitivity = 0.15f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Zoom")]
    [SerializeField] private float distance = 4.5f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 9f;
    [SerializeField] private float zoomSensitivity = 0.5f;

    [Header("Pan")]
    [SerializeField] private float panSensitivity = 0.0025f;

    [Header("Smoothing")]
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.06f;
    [SerializeField, Min(0f)] private float rotationSmoothSpeed = 18f;

    private float yaw;
    private float pitch = 25f;
    private Vector3 panOffset;
    private Vector3 positionVelocity;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("OrbitCameraController: назначьте Target в Inspector.", this);
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = NormalizeAngle(angles.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        UpdateCameraPosition(true);
    }

    private void LateUpdate()
    {
        if (target == null || Mouse.current == null)
            return;

        Mouse mouse = Mouse.current;
        bool pointerOverUi = EventSystem.current != null &&
                             EventSystem.current.IsPointerOverGameObject();

        if (!pointerOverUi && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * rotationSensitivity;
            pitch -= delta.y * rotationSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        if (!pointerOverUi && mouse.middleButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float panScale = panSensitivity * Mathf.Max(distance, 1f);
            panOffset += (-transform.right * delta.x - transform.up * delta.y) * panScale;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (!pointerOverUi && Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSensitivity * 0.01f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        UpdateCameraPosition(false);
    }

    private void UpdateCameraPosition(bool instant)
    {
        Vector3 focusPoint = target.position + targetOffset + panOffset;
        Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = focusPoint + desiredRotation * (Vector3.back * distance);

        if (instant || positionSmoothTime <= 0f)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);
        }

        if (instant || rotationSmoothSpeed <= 0f)
        {
            transform.rotation = desiredRotation;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime));
        }

    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    private void OnValidate()
    {
        minDistance = Mathf.Max(0.1f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        maxPitch = Mathf.Max(minPitch, maxPitch);
    }
}
