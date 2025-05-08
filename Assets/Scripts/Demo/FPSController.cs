using System.Linq;
using UnityEngine;

public class FPSController : PortalTraveller
{
    public float walkSpeed = 3;
    public float runSpeed = 6;
    public float smoothMoveTime = 0.1f;
    public float jumpForce = 8;
    public float gravity = 18;
    public bool lockCursor;
    public float mouseSensitivity = 10;
    public Vector2 pitchMinMax = new(-40, 85);
    public float rotationSmoothTime = 0.1f;
    public float yaw;
    public float pitch;

    [SerializeField] private GameObject wallCubePrefab;
    [SerializeField] private LayerMask wallLayer;
    private Camera cam;

    private CharacterController controller;
    private Vector3 currentRotation;
    private bool disabled;

    private bool jumping;
    private float lastGroundedTime;
    private Portal leftMouse;
    private GameObject leftPortalObject;
    private float pitchSmoothV;
    private Portal rightMouse;
    private GameObject rightPortalObject;
    private Vector3 rotationSmoothVelocity;
    private float smoothPitch;
    private Vector3 smoothV;
    private float smoothYaw;
    private Vector3 velocity;
    private float verticalVelocity;
    private float yawSmoothV;

    private void Start()
    {
        cam = Camera.main;
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        controller = GetComponent<CharacterController>();

        yaw = transform.eulerAngles.y;
        pitch = cam.transform.localEulerAngles.x;
        smoothYaw = yaw;
        smoothPitch = pitch;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Break();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            disabled = !disabled;
        }

        if (leftMouse && rightMouse)
        {
            leftMouse.linkedPortal = rightMouse;
            rightMouse.linkedPortal = leftMouse;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Input.mousePosition;
            var ray = cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, wallLayer))
            {
                if (leftPortalObject) leftPortalObject.SetActive(false);

                // 获取碰撞点位置和法线方向
                var spawnPosition = new Vector3(hit.point.x, hit.point.y, hit.point.z + hit.normal.normalized.z * 4f);
                var spawnRotation = Quaternion.LookRotation(hit.normal);

                if (leftPortalObject)
                {
                    leftPortalObject.SetActive(true);
                    leftPortalObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                }
                else
                {
                    leftPortalObject = Instantiate(wallCubePrefab, spawnPosition, spawnRotation);
                    leftMouse = leftPortalObject.GetComponent<Portal>();
                }

                if (!transform.GetComponentInChildren<MainCamera>().portals.Contains(leftMouse))
                    transform.GetComponentInChildren<MainCamera>().portals =
                        transform.GetComponentInChildren<MainCamera>().portals.Append(leftMouse).ToArray();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            var mousePos = Input.mousePosition;
            var ray = cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, wallLayer))
            {
                if (rightPortalObject) rightPortalObject.SetActive(false);

                // 获取碰撞点位置和法线方向
                var spawnPosition = new Vector3(hit.point.x, hit.point.y, hit.point.z + hit.normal.normalized.z * 4f);
                var spawnRotation = Quaternion.LookRotation(-hit.normal);
                if (rightPortalObject)
                {
                    rightPortalObject.SetActive(true);
                    rightPortalObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                }
                else
                {
                    rightPortalObject = Instantiate(wallCubePrefab, spawnPosition, spawnRotation);
                    rightMouse = rightPortalObject.GetComponent<Portal>();
                }

                if (!transform.GetComponentInChildren<MainCamera>().portals.Contains(rightMouse))
                    transform.GetComponentInChildren<MainCamera>().portals =
                        transform.GetComponentInChildren<MainCamera>().portals.Append(rightMouse).ToArray();
            }
        }

        if (disabled) return;

        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        var inputDir = new Vector3(input.x, 0, input.y).normalized;
        var worldInputDir = transform.TransformDirection(inputDir);

        var currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        var targetVelocity = worldInputDir * currentSpeed;
        velocity = Vector3.SmoothDamp(velocity, targetVelocity, ref smoothV, smoothMoveTime);

        verticalVelocity -= gravity * Time.deltaTime;
        velocity = new Vector3(velocity.x, verticalVelocity, velocity.z);

        var flags = controller.Move(velocity * Time.deltaTime);
        if (flags == CollisionFlags.Below)
        {
            jumping = false;
            lastGroundedTime = Time.time;
            verticalVelocity = 0;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var timeSinceLastTouchedGround = Time.time - lastGroundedTime;
            if (controller.isGrounded || (!jumping && timeSinceLastTouchedGround < 0.15f))
            {
                jumping = true;
                verticalVelocity = jumpForce;
            }
        }

        var mX = Input.GetAxisRaw("Mouse X");
        var mY = Input.GetAxisRaw("Mouse Y");

        // Verrrrrry gross hack to stop camera swinging down at start
        var mMag = Mathf.Sqrt(mX * mX + mY * mY);
        if (mMag > 5)
        {
            mX = 0;
            mY = 0;
        }

        yaw += mX * mouseSensitivity;
        pitch -= mY * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchSmoothV, rotationSmoothTime);
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawSmoothV, rotationSmoothTime);

        transform.eulerAngles = Vector3.up * smoothYaw;
        cam.transform.localEulerAngles = Vector3.right * smoothPitch;
    }

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        var eulerRot = rot.eulerAngles;
        var delta = Mathf.DeltaAngle(smoothYaw, eulerRot.y);
        yaw += delta;
        smoothYaw += delta;
        transform.eulerAngles = Vector3.up * smoothYaw;
        velocity = toPortal.TransformVector(fromPortal.InverseTransformVector(velocity));
        Physics.SyncTransforms();
    }
}