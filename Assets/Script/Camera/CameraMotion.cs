using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Motion")]
    [SerializeField] private float panSpeed = 0.04f;
    [SerializeField] private float rotateSpeed = 0.2f;
    [SerializeField] private float minPitch = 0f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minHeight = 4f;
    [SerializeField] private float maxHeight = 25f;

    [Header("Focus")]
    [SerializeField] private Transform startPosition;
    [SerializeField] private float focusDistance = 25f;
    [SerializeField] private float smoothSpeed = 8f;

    private InputActionMap cameraMap;
    private InputAction panAction;
    private InputAction rotateAction;
    private InputAction mouseDeltaAction;
    private InputAction scrollAction;

    private Camera cam;
    private float targetFOV;
    private float startFOV;

    private Quaternion focusRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isTransitioning;

    private bool subscribed;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            startFOV = cam.fieldOfView;
            targetFOV = startFOV;
        }

        if (inputActions != null)
        {
            cameraMap = inputActions.FindActionMap("Camera");
            if (cameraMap != null)
            {
                panAction = cameraMap.FindAction("RightClick");
                rotateAction = cameraMap.FindAction("MiddleClick");
                mouseDeltaAction = cameraMap.FindAction("MouseDelta");
                scrollAction = cameraMap.FindAction("Scroll");

                cameraMap.Enable();
            }
        }

        if (startPosition != null)
        {
            transform.position = startPosition.position;
            transform.rotation = startPosition.rotation;
        }

        focusRotation = transform.rotation;
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }
    private void Subscribe()
    {
        if (subscribed || TurnManager.Instance == null) return;
        TurnManager.Instance.HeroTurnStarted += OnHeroTurnStarted;
        subscribed = true;
    }

    private void Start() { Subscribe(); }
    private void OnEnable() { Subscribe(); }

    private void OnDisable()
    {
        if (!subscribed || TurnManager.Instance == null) return;
        TurnManager.Instance.HeroTurnStarted -= OnHeroTurnStarted;
        subscribed = false;
    }
    private void OnHeroTurnStarted(Hero hero)
    {
        FocusOnHero(hero);
    }

    public void FocusOnHero(Hero hero)
    {
        if (hero  == null) return;

        targetRotation = focusRotation;
        targetPosition = hero.transform.position - (focusRotation * Vector3.forward) * focusDistance;

        targetFOV = startFOV;
        isTransitioning = true;
    }

    private void Update()
    {
        if (mouseDeltaAction == null || scrollAction == null) return;

        Vector2 delta = mouseDeltaAction.ReadValue<Vector2>();
        float scroll = scrollAction.ReadValue<Vector2>().y;

        bool isPanning = panAction != null && panAction.IsPressed();
        bool isRotating = rotateAction != null && rotateAction.IsPressed();

        if (isPanning || isRotating || scroll != 0)
        {
            isTransitioning = false;
        }

        if (isPanning)
        {
            Vector3 forward = targetRotation * Vector3.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = targetRotation * Vector3.right;
            right.y = 0;
            right.Normalize();

            Vector3 move = (right * -delta.x) + (forward * -delta.y);
            targetPosition += move * panSpeed;
        }

        if (isRotating)
        {
            Vector3 currentEuler = targetRotation.eulerAngles;

            float pitch = Mathf.DeltaAngle(0f, currentEuler.x);
            pitch += -delta.y * rotateSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            float yaw = currentEuler.y + (delta.x * rotateSpeed);

            targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        if (scroll != 0)
        {
            Vector3 zoomDirection = targetRotation * Vector3.forward;
            Vector3 nextPos = targetPosition + (zoomDirection * (Mathf.Sign(scroll) * zoomSpeed));

            if (nextPos.y >= minHeight && nextPos.y <= maxHeight)
            {
                targetPosition = nextPos;
            }
        }

        float t = smoothSpeed * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);

        if (isTransitioning && Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            isTransitioning = false;
        }
    }
}