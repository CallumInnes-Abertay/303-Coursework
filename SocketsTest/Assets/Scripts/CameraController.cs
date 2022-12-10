using UnityEngine;

public class CameraController : MonoBehaviour
{
    private float horizontalRotation;
    private float verticalRotation;
    [SerializeField] private PlayerManager player;

    [Header("Camera Settings")]
    [SerializeField] private float sensitivity = 100f;
    [SerializeField] private float clampAngle = 85f;

    private void Awake()
    {
        //Locks the cursor.
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = player.transform.eulerAngles.y;
    }

    private void Update()
    {
        Look();
        //Shows which way the player is looking.
        Debug.DrawRay(transform.position, transform.forward * 2, Color.red);
    }

    /// <summary>
    /// Moves the camera by mouse.
    /// </summary>
    private void Look()
    {
        //Maps the vertical and horizontal mouse movements to vectors. 
        var _mouseVertical = -Input.GetAxis("Mouse Y");
        var _mouseHorizontal = Input.GetAxis("Mouse X");

        verticalRotation += _mouseVertical * sensitivity * Time.deltaTime;
        horizontalRotation += _mouseHorizontal * sensitivity * Time.deltaTime;

        //Clamps the camera from looping around vertically.
        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        //Rotates the camera.
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        player.transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }
}