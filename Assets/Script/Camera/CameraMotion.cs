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
    private InputAction switchCamAction;

    private Camera cam;
    private float targetFOV;
    private float startFOV;

    private int camIndex = 0;
    private bool isTransitioning;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        startFOV = cam.fieldOfView;
        targetFOV = startFOV;

        cameraMap = inputActions.FindActionMap("Camera");
        panAction = cameraMap.FindAction("RightClick");
        rotateAction = cameraMap.FindAction("MiddleClick");
        mouseDeltaAction = cameraMap.FindAction("MouseDelta");
        scrollAction = cameraMap.FindAction("Scroll");
        switchCamAction = cameraMap.FindAction("Switch");

        switchCamAction.performed += OnSwitchCam;
        cameraMap.Enable();

        if (camPositions.Length > 0)
        {
            transform.position = camPositions[0].position;
            transform.rotation = camPositions[0].rotation;
        }
    }

    private void OnSwitchCam(InputAction.CallbackContext context)
    {
        if (camPositions.Length == 0)
            return;

        camIndex = (camIndex + 1) % camPositions.Length;
        isTransitioning = true;

        targetFOV = startFOV;
    }

    // Update is called once per frame
    private void Update()
    {
        Vector2 delta = mouseDeltaAction.ReadValue<Vector2>();
        float scroll = scrollAction.ReadValue<Vector2>().y;

        if (panAction.IsPressed() || rotateAction.IsPressed() || scroll != 0)
        {
            isTransitioning = false;
        }

        if (panAction.IsPressed())
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

        if (rotateAction.IsPressed())
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

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, zoomSmooth * Time.deltaTime);

        if (isTransitioning)
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