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
    public int Tick { get; private set; }

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

        Tick = 0;

        Server.Start(12, 50000);
    }

    private void FixedUpdate()
    {
        if (!isTimerRunning)
            return;

        Tick ++;
    }


    //Stops the server and tells all clients to close when the application is closed.
    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    /// <summary>
    /// Spawns player and generates random coordinates to spawn them at.
    /// </summary>
    /// <returns>A new player.</returns>
    public Player InstantiatePlayer(string _username)
    {
        //Changes in the editor for debugging purposes.
        playerPrefab.name = $"Player {_username}";
        //Random position to spawn
        var spawnPos = new Vector3(Random.Range(-7.0f, 7.0f), 6.0f, Random.Range(-5.0f, 5.0f));

        //Spawns the player.
        return Instantiate(playerPrefab, spawnPos, Quaternion.identity).GetComponent<Player>();
    }

    /// <summary>
    /// Pings the client to adjust their ping by a set amount.
    /// </summary>
    /// <param name="_ip">The IP address to ping.</param>
    /// <returns>The ping to the client (Note not the roundtrip time)</returns>
    public static int PingClient(string _ip)
    {
        //Removes the port from the ip.
        _ip = _ip.Remove(_ip.LastIndexOf(":", StringComparison.Ordinal));

        Ping pinger;
        pinger = new Ping();
        var alltimes = new List<int>();
        PingReply reply;
        //Pings the client 4 times for an average.
        for (var i = 0; i < 4; i++)
        {
            reply = pinger.Send(_ip);
            if (reply is { Status: IPStatus.Success })
            {
                //Divided by 2 as we only want the distance to the client, not the roundtrip time.
                var timeTaken = (int)(reply.RoundtripTime / 2);
                Debug.Log($"Ping: {timeTaken}");
                alltimes.Add(timeTaken);
            }
            else
            {
                Debug.Log("Pinger failed.");
                pinger.Dispose();
                return -1;
            }
        }

        pinger.Dispose();
        Debug.Log($"Ping {_ip}: {alltimes.Average()}");
        return (int)(alltimes.Average() / 2);
    }

    /// <summary>
    /// Starts the server side timer.
    /// </summary>
    public void StartTimer()
    {
        Tick = 0;
        isTimerRunning = true;
    }

    /// <summary>
    /// Stop the server side timer and resets it.
    /// </summary>
    public void StopTimer()
    {
        Tick = 0;
        isTimerRunning = false;
    }
}