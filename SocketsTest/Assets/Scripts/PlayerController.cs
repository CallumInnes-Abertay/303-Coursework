using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    
    [NonSerialized] private PlayerManager pManager;
   [SerializeField] private float speed;
   [SerializeField] private float jumpHeight;
    private float gravity = -18;
    private CharacterController controller;
    private float yVelocity;
    [SerializeField][Range(0.1f, 5)] private float correctionThreshold = 1;

    private void Start()
    {
        //Reduce movement variables to account for tick rate.

        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        speed *= Time.fixedDeltaTime;
        jumpHeight *= Time.fixedDeltaTime;
        
        controller = GetComponent<CharacterController>();
        pManager = GetComponent<PlayerManager>();
    }

    private void FixedUpdate()
    {
        SendInputToServer();
    }

    // <summary>Sends player input to the server.</summary>
    private void SendInputToServer()
    {
        //Bool array of every possible movement input.
        bool[] inputs = {
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            Input.GetKey(KeyCode.A),
            Input.GetKey(KeyCode.D),
            Input.GetKey(KeyCode.Space)
        };

        var inputDirection = Vector2.zero;

        //Translates based on input
        if (inputs[0]) inputDirection.y += 1;
        if (inputs[1]) inputDirection.y -= 1;
        if (inputs[2]) inputDirection.x -= 1;
        if (inputs[3]) inputDirection.x += 1;

        ClientSend.PlayerMovement(inputs);
        ClientPrediction(inputs,inputDirection);
    }

    /// <summary>
    /// Input prediction where inputs are ran first client side.
    /// </summary>
    /// <param name="_inputs">Bool array of player inputs</param>
    /// <param name="_inputDirection">Vector2 to increment or decrement movements off.</param>
    private void ClientPrediction(bool[] _inputs,Vector2 _inputDirection)
    {
        //Gets direction the player if moving in
        var moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        moveDirection *= speed;

        //Jump (only if grounded)
        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (_inputs[4]) yVelocity = jumpHeight;
        }

        //Gravity
        yVelocity += gravity;
        moveDirection.y = yVelocity;

        //Moves the player
        controller.Move(moveDirection);

        //Reset the player to spawn if they fall off.
        if (gameObject.transform.position.y < -20)
        {
            gameObject.transform.position = new Vector3(0, 10, 0);
        }

        //Adds the position along with the tick it was performed at,
        //to a list of previousPosition for the server to snap to.
        pManager.Positions.Add(new PreviousPositions(
            GameManager.instance.Tick,
            transform.position
            ));
    }

    ///     Snaps the player back to the correct position if the clients prediction is too far off.
    /// </summary>
    /// <param name="_position">The position to check</param>
    /// <param name="_tick">The tick this value relates to</param>
    /// <returns>If they could sync the client to the server or not.</returns>
    public bool ServerCorrection(Vector3 _position, int _tick)
    {
        var currentTime = pManager.Positions.Where(x => x.Tick == _tick).FirstOrDefault();

        if (currentTime == null)
            return false;
        Debug.Log($"Correcting position at Server time: {_tick} Client time: {currentTime.Tick}");

        //Check if the clients predicted position is higher than a set threshold.
        var distance =
            Vector3.Distance(new Vector3(currentTime.Position.x, currentTime.Position.y, currentTime.Position.z),
                _position);

        //If in the case that it is, then snap the player back.
        if (distance > correctionThreshold)
        {
            //And in the case that it has strayed too far off.
            Debug.Log($"Threshold exceeded by {distance}, fixing time");
            //Snap the player back
            transform.position = _position;
        }

        //If it's not then we can accept the clients predicted movement was good enough
        //Either way we remove all positions past the incoming server tick to free up memory.
        pManager.Positions.RemoveAll(x => x.Tick <= _tick);
        return true;
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