using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public static Dictionary<int, PlayerManager> players = new();
    [SerializeField] private GameObject collectablePrefab;
    [SerializeField] private GameObject externalPlayerPrefab;
    [SerializeField] private GameObject localPlayerPrefab;

    [field: NonSerialized] public int Time { get; private set; }
    private bool isTimerRunning;

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

    private void Start()
    {
        //So the timer doesnt stop if you alt tab and desync the player and client.
        Application.runInBackground = true;
    }

    //Runs the timer
    private void FixedUpdate()
    {
        if (!isTimerRunning) 
            return;


        Time++;
        UIManager.instance.timerText.text = Time.ToString();
    }

    /// <summary>Spawns a player.</summary>
    /// <param name="_id">The player's ID.</param>
    /// <param name="_username">The player's username.</param>
    /// <param name="_position">The player's starting position.</param>
    /// <param name="_rotation">The player's starting rotation.</param>
    /// <param name="_colour">The colour of the player.</param>
    public void SpawnPlayer(int _id, string _username, Vector3 _position, Quaternion _rotation, Vector3 _colour)
    {
        GameObject _player;

        //If the id is the same as the current user id, then spawn the local player the user will be playing as.
        if (_id == Client.instance.myId)
        {
            _player = Instantiate(localPlayerPrefab, _position, _rotation);
        }
        //If not then it's a joining  player, so spawn the external prefab the connecting player will play as.
        else
        {
            _player = Instantiate(externalPlayerPrefab, _position, _rotation);

            //Changes their colour
            var colorHsv = new Color(_colour.x, _colour.y, _colour.z, 1);
            var renderer = _player.GetComponentInChildren(typeof(MeshRenderer)) as MeshRenderer;
            renderer.material.color = colorHsv;
        }

        //Caches player manager.
        var playerManager = _player.GetComponent<PlayerManager>();

        playerManager.Id = _id;
        playerManager.Username = _username;
        playerManager.Score = 0;


        //Adds the player (regardless if they're local or external_ to a dictionary of all players
        //for affecting all players
        players.Add(_id, playerManager);
    }

    /// <summary>
    /// Spawns collectable
    /// </summary>
    /// <param name="_collectablePos">Position to spawn collectable.</param>
    public void SpawnCollectable(Vector3 _collectablePos)
    {
        Instantiate(collectablePrefab, _collectablePos, Quaternion.identity);
    }

    /// <summary>
    /// Starts the timer client side by turning on FixedUpdate.
    /// </summary>
    /// <param name="_serverTime">The time the server has passed in (adjusted for ping time).</param>
    public void StartTimer(int _serverTime)
    {
        Time = _serverTime;
        isTimerRunning = true;
    }
}