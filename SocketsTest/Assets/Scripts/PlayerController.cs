using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerManager pManager;

    [SerializeField]private float speed;
    [SerializeField] private float jumpHeight;
    private CharacterController controller;
    private Vector3 velocity;
    private float gravity = -9.81f;


    private void FixedUpdate()
    {
        //SendInputToServer();
        Movement();

        Falling();

        //If the Player presses the Jump button (spacebar by default) and is on the ground, only then can they jump.
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            Jumping();
        }

    }

    private void Start()
    {
        pManager = GetComponent<PlayerManager>();
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        //Locks the cursor.
        Cursor.lockState = CursorLockMode.Locked;
    }


    private void Movement()
    {
        //Gets the input. Unity defaults this to standard WASD movement in the input manager.
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        //Gets the players desired movement as a vector3.
        Vector3 movement = transform.right * x + transform.forward * z;
        //Moves the character controller by that amount by the speed the player needs to go. Multiplied by Time.delta time to make sure it's not frame rate dependent.
        controller.Move(speed * Time.deltaTime * movement);

        ClientSend.PlayerMovement(this);
    }

    private void Falling()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2.5f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Lets the player jump by set amount.
    /// </summary>
    private void Jumping()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
    }

    //If collision with a collectable.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectable"))
        {
            Destroy(other.gameObject);
            ClientSend.CollectableCollision();
            pManager.Score++;
            UIManager.instance.scoreText.text = $"Current score {pManager.Score}";
        }
    }
}