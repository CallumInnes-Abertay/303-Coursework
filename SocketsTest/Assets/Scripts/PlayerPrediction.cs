using System;
using System.Linq;
using UnityEngine;

public class  PlayerPrediction : MonoBehaviour
{
    public static PlayerPrediction instance;
    [SerializeField] private PlayerManager localPlayerId;

    [Range(0.0f, 50.0f)] [SerializeField] private int extrapolationThreshold;
    public bool isPredicting = true;


    //Singleton pattern.
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Update()
    {
        //Toggle predicting or not. (cant be in fixed update or will miss inputs)
        if (Input.GetKeyDown(KeyCode.P))
        {
            isPredicting = !isPredicting;
        }
    }

    private void FixedUpdate()
    {
        //If timer hasnt started yet then return early.
        if (GameManager.instance.Tick <= 1)
        {
            return;
        }

        if (isPredicting)
        {
            UIManager.instance.isPredictingText.text = "Predicting:True";
            PredictMovement();
            return;
        }

        UIManager.instance.isPredictingText.text = "Predicting:False";
    }


    /// <summary>
    /// Predicts the next movement each external player will take at all times.
    /// </summary>
    void PredictMovement()
    {
        foreach (var (key, player) in GameManager.players)
        {
            /*We're not predicting the player movement since that's ran client side,
             instead we're guessing the position of all externals players, and thus should
            return on any instance of the local player as so to not cause jerky player movement.
            */
            if (player.gameObject.CompareTag("Player"))
            {
                continue;
            }

            //if(player.Id == localPlayerId)
            //If there is no past data to work off.
            if (player.Positions == null || !player.Positions.Any())
            {
                continue;
            }

            var newPosition = CalculateNewPosition(player);

            //If there is a new position set that as the players new position.
            if (newPosition != Vector3.zero)
            {
                player.SetPosition(newPosition);
            }
        }
    }
    /// <summary>
    /// Calculates the new player position using D=VT calculation.
    /// </summary>
    /// <param name="_player">The player to predict on</param>
    /// <returns>The new player position (or 0,0,0 if no new position).</returns>
    private Vector3 CalculateNewPosition(PlayerManager _player)
    {

        var lastPosition = _player.Positions.LastOrDefault() ?? throw new ArgumentNullException("_player.Positions.Last()");
        var secondLastPosition = _player.Positions.FirstOrDefault() ?? throw new ArgumentNullException("_player.Positions.First()");

        if (lastPosition == null) throw new ArgumentNullException(nameof(lastPosition));
        if (secondLastPosition == null) throw new ArgumentNullException(nameof(secondLastPosition));

        Debug.Log($"lastPosition Time: {lastPosition.Tick}\n secondLastPosition Time:{secondLastPosition.Tick}");
        Debug.Log($"lastPosition Position: {lastPosition.Position}\n secondLastPosition:{secondLastPosition.Position}");


        if (lastPosition.Tick == 0 || secondLastPosition.Tick == 0)
        {
            return Vector3.zero;
        }

        float timeBetweenPositions = lastPosition.Tick - secondLastPosition.Tick;

        if (timeBetweenPositions == 0)
        {
            return Vector3.zero;
        }


        var speed = (secondLastPosition.Position - lastPosition.Position) / timeBetweenPositions;


        float timeSinceLastTick = GameManager.instance.Tick - lastPosition.Tick;

        Vector3 displacement = new(speed.x * timeSinceLastTick, speed.y * timeSinceLastTick,
            speed.z * timeSinceLastTick);

        var newPos = gameObject.transform.position + displacement;

        Debug.Log($"Speed: {speed}\n newPos:{newPos}");
        return newPos;


    }

}