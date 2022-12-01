using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using Ping = System.Net.NetworkInformation.Ping;
using Random = UnityEngine.Random;


public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    private bool isTimerRunning;
    [SerializeField] private GameObject playerPrefab;
    public int Time { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object");
            Destroy(this);
        }
    }

    private void Start()
    {
        //Enforces 60fps for consistency 
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Application.runInBackground = true;

        Time = 0;

        Server.Start(12, 50000);
    }

    private void FixedUpdate()
    {
        if (!isTimerRunning)
            return;

        Time++;
        //Debug.Log(Time);
    }


    //Stops the server and tells all clients to close when the application is closed.
    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    /// <summary>
    ///     Spawns player
    /// </summary>
    /// <returns>A new player.</returns>
    public Player InstantiatePlayer(string _username)
    {
        //Changes in the editor for debugging purposes.
        playerPrefab.name = $"Player {_username}";
        //Random position to spawn
        Vector3 spawnPos = new Vector3(Random.Range(-7.0f, 7.0f), 6.0f, Random.Range(-5.0f, 5.0f));

        //Spawns the player.
        return Instantiate(playerPrefab, spawnPos, Quaternion.identity).GetComponent<Player>();
    }

    public int PingClient(string _ip)
    {
        _ip = _ip.Remove(_ip.LastIndexOf(":", StringComparison.Ordinal));

        Ping pinger;
        pinger = new Ping();
        var reply = pinger.Send(_ip);
        var alltimes = new List<int>();

        for (var i = 0; i < 5; i++)
            if (reply is { Status: IPStatus.Success })
            {
                var timeTaken = (int)(reply.RoundtripTime / 2);
                alltimes.Add(timeTaken);
            }
            else
            {
                pinger.Dispose();
                return -1;
            }

        pinger.Dispose();
        Debug.Log($"Ping:{alltimes.Average()}");
        return (int)(alltimes.Average() / 2);
    }

    public void StartTimer()
    {
        Time = 0;
        isTimerRunning = true;
    }

    public void StopTimer()
    {
        Time = 0;
        isTimerRunning = false;
    }
}