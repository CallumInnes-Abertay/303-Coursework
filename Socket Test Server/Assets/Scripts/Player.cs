using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [NonSerialized] public Vector3 colour;
    private CharacterController controller;
    [SerializeField] private float gravity = -18;
    public int id;
    private bool[] inputs;

    [SerializeField] private float jumpHeight = 10;
    public int score;
    [SerializeField] private float speed = 5f;

    public string username;
    private float yVelocity;

    private void Start()
    {
        //Reduce movement variables to account for tick rate.
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        speed *= Time.fixedDeltaTime;
        jumpHeight *= Time.fixedDeltaTime;


        controller = GetComponent<CharacterController>();
        var meshRenderer = GetComponentInChildren<MeshRenderer>();

        //Gets a random colour
        if (meshRenderer != null)
        {
            var colorHsv = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

            meshRenderer.material.color = colorHsv;
            colour.Set(colorHsv.r, colorHsv.b, colorHsv.g);
        }
        else
        {
            colour.Set(0, 0, 0);
        }

        ServerSend.PlayerColour(this);
    }

    private void FixedUpdate()
    {
        var inputDirection = Vector2.zero;
        if (inputs[0]) inputDirection.y += 1;
        if (inputs[1]) inputDirection.y -= 1;
        if (inputs[2]) inputDirection.x -= 1;
        if (inputs[3]) inputDirection.x += 1;

        Move(inputDirection);
    }

    /// <summary>
    ///     Spawns the player
    /// </summary>
    /// <param name="_id">Id of the player.</param>
    /// <param name="_username">Username client entered</param>
    public void Initialise(int _id, string _username)
    {
        id = _id;
        username = _username;
        score = 0;
        inputs = new bool[5];
    }


    private void ProcessInputs()
    {
        var inputDirection = Vector2.zero;
        if (inputs[0]) inputDirection.y += 1;
        if (inputs[1]) inputDirection.y -= 1;
        if (inputs[2]) inputDirection.x -= 1;
        if (inputs[3]) inputDirection.x += 1;

        Move(inputDirection);
    }

    /// <summary>Calculates the player's desired direction and moves them.</summary>
    /// <param name="_inputDirection"></param>
    private void Move(Vector2 _inputDirection)
    {
        //Gets direction the player if moving in
        var moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        moveDirection *= speed;

        //Jump (only if grounded)
        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (inputs[4]) yVelocity = jumpHeight;
        }

        //Gravity
        yVelocity += gravity;
        moveDirection.y = yVelocity;

        //Moves the player
        controller.Move(moveDirection);

        //Reset the player to spawn if they fall off.
        if (gameObject.transform.position.y < -20)
            gameObject.transform.position = new Vector3(0, 10, 0);

        //Sends their new position and rotation back to the client.
        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    /// <summary>Updates the player input with newly received input.</summary>
    /// <param name="_inputs">The new key inputs.</param>
    /// <param name="_rotation">The new rotation.</param>
    public void SetInput(bool[] _inputs, Quaternion _rotation)
    {
        inputs = _inputs;
        transform.rotation = _rotation;
    }

    /// <summary>
    /// Destroy this player, this is here so client doesn't have to inherit from monobehaviour unnecessarily.
    /// </summary>
    public void DestroyPlayer()
    {
        Destroy(gameObject);
    }
}