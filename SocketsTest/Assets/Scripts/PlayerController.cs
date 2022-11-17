using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerManager pManager;

    private void FixedUpdate()
    {
        SendInputToServer();
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

    private void SendInputToServer()
    {
        //Gets all key inputs the player can send to the server so the server can move the player.
        bool[] _inputs =
        {
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            Input.GetKey(KeyCode.A),
            Input.GetKey(KeyCode.D),
            Input.GetKey(KeyCode.Space)
        };

        ClientSend.PlayerMovement(_inputs);
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