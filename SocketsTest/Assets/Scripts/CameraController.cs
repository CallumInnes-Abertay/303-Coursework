using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private readonly float clampAngle = 85f;

    private float horizontalRotation;
    [SerializeField] private PlayerManager player;
    [SerializeField] private readonly float sensitivity = 100f;
    private float verticalRotation;

    private void Start()
    {
        verticalRotation = transform.localEulerAngles.x;
        horizontalRotation = player.transform.eulerAngles.y;
    }

    private void Update()
    {
        Look();
        Debug.DrawRay(transform.position, transform.forward * 2, Color.red);
    }

    private void Look()
    {
        var _mouseVertical = -Input.GetAxis("Mouse Y");
        var _mouseHorizontal = Input.GetAxis("Mouse X");

        verticalRotation += _mouseVertical * sensitivity * Time.deltaTime;
        horizontalRotation += _mouseHorizontal * sensitivity * Time.deltaTime;

        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        player.transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }
}