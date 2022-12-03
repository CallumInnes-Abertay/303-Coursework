using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [NonSerialized] public PlayerManager pManager;

   [SerializeField] private float speed;
   [SerializeField] private float jumpHeight;
    private float gravity = -9.81f;
    private CharacterController controller;
    private Vector3 velocity;


    private void Update()
    {
        //SendInputToServer();
        //Movement();
        //Falling();


        ////If the Player presses the Jump button (spacebar by default) and is on the ground, only then can they jump.
        //if (Input.GetButtonDown("Jump") && controller.isGrounded)
        //{
        //    Jumping();
        //}

    }

    private void Start()
    {
        pManager = GetComponent<PlayerManager>();

    }

    private void Awake()
    {
        //Locks the cursor.
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        SendInputToServer();
    }

    // <summary>Sends player input to the server.</summary>
    private void SendInputToServer()
    {
        bool[] inputs = {
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            Input.GetKey(KeyCode.A),
            Input.GetKey(KeyCode.D),
            Input.GetKey(KeyCode.Space)
        };

        ClientSend.PlayerMovement(inputs);
    }


    private void Movement()
    {
        
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