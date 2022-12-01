using System;
using System.Linq;
using UnityEngine;

public class PlayerPrediction : MonoBehaviour
{
    private static PlayerPrediction instance;
    [SerializeField] private PlayerManager localPlayerId;

    [Range(0.0f, 50.0f)] [SerializeField] private int extrapolationThreshold;
    [SerializeField] bool isPredicting = true;


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

    private void FixedUpdate()
    {
        //If timer hasnt started yet then return early.
        if (GameManager.instance.Time <= 0)
        {
            return;
        }

        //Toggle predicting or not.
        if (Input.GetKeyDown(KeyCode.P))
        {
            isPredicting = !isPredicting;

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
        foreach (var (key, value) in GameManager.players)
        {
            /*We're not predicting the player movement since that's ran client side,
             instead we're guessing the position of all externals players, and thus should
            return on any instance of the local player as so to not cause jerky player movement.
            */
            if (value.gameObject.gameObject.CompareTag("Player"))
            {
                return;
            }

            //if(player.Id == localPlayerId)
            //If there is no past data to work off.
            if (value.Positions == null || !value.Positions.Any())
            {
                return;
            }

            var newPosition = CalculateNewPosition(value);

            //If there is a new position set that as the players new position.
            if (newPosition != Vector3.zero)
            {
                value.gameObject.transform.position = newPosition;
            }
            else
            {
                return;
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

        Debug.Log($"lastPosition Time: {lastPosition.Time}\n secondLastPosition Time:{secondLastPosition.Time}");
        Debug.Log($"lastPosition Position: {lastPosition.Position}\n secondLastPosition:{secondLastPosition.Position}");


        if (lastPosition.Time == 0 || secondLastPosition.Time == 0)
        {
            return Vector3.zero;
        }

        float timeBetweenPositions = lastPosition.Time - secondLastPosition.Time;

        if (timeBetweenPositions == 0)
        {
            return Vector3.zero;
        }


        var speed = (secondLastPosition.Position - lastPosition.Position) / timeBetweenPositions;


        float timeSinceLastTick = GameManager.instance.Time - lastPosition.Time;

        Vector3 displacement = new Vector3(speed.x * timeSinceLastTick, speed.y * timeSinceLastTick,
            speed.z * timeSinceLastTick);

        var newPos = gameObject.transform.position + displacement;

        Debug.Log($"Speed: {speed}\n newPos:{newPos}");
        return newPos;


    }

}