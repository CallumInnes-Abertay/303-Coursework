using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [NonSerialized] public PlayerManager pManager;

    public  float speed;
    [SerializeField] private float jumpHeight;
    private CharacterController controller;
    private Vector3 velocity;
    private float gravity = -9.81f;


    private void Update()
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

        //Tells the server where the player now is.
        ClientSend.PlayerMovement(this);
    }

    private void Falling()
    {
        
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2.5f;
        }

        //Makes them fall.
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        //Respawning player
        if (transform.position.y < -40)
        {
            Debug.Log("Player respawning");

            gameObject.transform.position = new Vector3(0, 10, 0);
        }
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
            //Then destroy the collectable and then tell the server (to tell all player and spawn a new one.)
            Destroy(other.gameObject);
            ClientSend.CollectableCollision();
            pManager.Score++;
            UIManager.instance.scoreText.text = $"Current score {pManager.Score}";
        }
    }
}