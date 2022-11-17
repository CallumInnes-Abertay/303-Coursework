using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    private bool[] inputs;

    [SerializeField] private float jumpSpeed = 5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    public string username;
    public int id;
    public int score;
    [NonSerialized] public Vector3 colour;


    private float yVelocity;

    private void Start()
    {
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;

        var meshRenderer = GetComponentInChildren<MeshRenderer>();

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

    ///
    public void FixedUpdate()
    {
        var _inputDirection = Vector2.zero;

        //Moves the player depending on what keys the client pressed.
        if (inputs[0]) _inputDirection.y += 1;
        if (inputs[1]) _inputDirection.y -= 1;
        if (inputs[2]) _inputDirection.x -= 1;
        if (inputs[3]) _inputDirection.x += 1;

        Move(_inputDirection);
    }

    /// <summary>Calculates the player's desired direction and moves them.</summary>
    /// <param name="_inputDirection"></param>
    private void Move(Vector2 _inputDirection)
    {
        //Moves the player in the direction chosen by incoming keys.
        var moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        moveDirection *= moveSpeed;

        //Jump and gravity
        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (inputs[4]) yVelocity += jumpSpeed;
        }

        yVelocity += gravity;

        moveDirection.y = yVelocity;
        controller.Move(moveDirection);

       

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
    ///     Destroy this player, this is here so client doesnt have to inherit from monobehaviour.
    /// </summary>
    public void DestroyPlayer()
    {
        Destroy(gameObject);
    }
}