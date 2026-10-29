using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Motion")]
    [SerializeField] private float panSpeed = 0.01f;
    [SerializeField] private float rotateSpeed = 0.2f;
    [SerializeField] private float minPitch = 0f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Zoom")]
    [SerializeField] private float zoomStep = 5f;
    [SerializeField] private float zoomSmooth = 8f;
    [SerializeField] private float minFOV = 20f;
    [SerializeField] private float maxFOV = 70f;

    [Header("Positions")]
    [SerializeField] private Transform[] camPositions;
    [SerializeField] private float camMoveSpeed = 3f;
    [SerializeField] private float camRotateSpeed = 3f;

    private InputActionMap cameraMap;
    private InputAction panAction;
    private InputAction rotateAction;
    private InputAction mouseDeltaAction;
    private InputAction scrollAction;

    private Camera cam;
    private float targetFOV;
    private float startFOV;
    private int turnCount;

    private int camIndex = 0;
    private bool isTransitioning;

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

        if (camPositions != null && camPositions.Length > 0 && camPositions[0] != null)
        {
            transform.position = camPositions[0].position;
            transform.rotation = camPositions[0].rotation;
        }
    }

    private void OnEnable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.HeroTurnStarted += OnHeroTurnStarted;
        }
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.HeroTurnStarted -= OnHeroTurnStarted;
        }
    }

    private void OnHeroTurnStarted(Hero hero)
    {
        if (camPositions == null || camPositions.Length == 0) return;

        GoToPosition(turnCount % camPositions.Length);
        turnCount++;
    }

    public void GoToPosition(int index)
    {
        if (camPositions == null || camPositions.Length == 0 || index < 0 || index >= camPositions.Length)
            return;

        camIndex = index;
        isTransitioning = true;
        targetFOV = startFOV;
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
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            Vector3 move = right * -delta.x + forward * -delta.y;
            transform.position += move * panSpeed;
        }

        if (isRotating)
        {
            float pitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);

            pitch += -delta.y * rotateSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.eulerAngles = new Vector3(pitch, transform.eulerAngles.y, transform.eulerAngles.z);
        }

        if (scroll != 0)
        {
            targetFOV -= Mathf.Sign(scroll) * zoomStep;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, zoomSmooth * Time.deltaTime);
        }

        if (isTransitioning && camPositions != null && camPositions.Length > camIndex && camPositions[camIndex] != null)
        {
            Transform target = camPositions[camIndex];

            transform.position = Vector3.Lerp(transform.position, target.position, camMoveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, camRotateSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                isTransitioning = false;
            }
        }
    }
}