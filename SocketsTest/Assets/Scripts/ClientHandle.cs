using System;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientHandle : MonoBehaviour
{
    /// <summary>
    ///     Reads out the
    /// </summary>
    /// <param name="_packet"></param>
    public static void Welcome(Packet _packet)
    {
        var msg = _packet.ReadString();
        var myId = _packet.ReadInt();

        Debug.Log($"Message from server: {msg}");
        Client.instance.myId = myId;
        ClientSend.WelcomeReceived();

        // Now that we have the client's id, we can by connect UDP
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    //Starts the client side timer synced up with the server (adjusted for ping).
    public static void StartTimer(Packet _packet)
    {
        var time = _packet.ReadInt();

        GameManager.instance.StartTimer(time);
    }


    
    /// <summary>
    ///     Spawns the player into the game.
    /// </summary>
    /// <param name="_packet">The id, username, position,rotation and colour of the player.</param>
    public static void SpawnPlayer(Packet _packet)
    {
        var id = _packet.ReadInt();
        var username = _packet.ReadString();
        var position = _packet.ReadVector3();
        var rotation = _packet.ReadQuaternion();
        var colour = _packet.ReadVector3();

        //Spawns the player the same as the server.
        GameManager.instance.SpawnPlayer(id, username, position, rotation, colour);
    }

    /// <summary>
    ///     Reads in the players current position
    /// </summary>
    /// <param name="_packet">Players id and the position of the player as a vector3.</param>
    public static void UpdatePlayerTransform(Packet _packet)
    {
        var id = _packet.ReadInt();
        var time = _packet.ReadInt();
        var position = _packet.ReadVector3();
        var rotation = _packet.ReadQuaternion();

        PlayerManager currentPlayer;

        //Caches the player we're dealing with for performance.
        try
        {
            currentPlayer = GameManager.players[id];
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }
        //Sets player position and transform
        currentPlayer.transform.SetPositionAndRotation(position, rotation);

        //Sets the values to
        if (GameManager.players.TryGetValue(id, out PlayerManager _player))
        {
            _player.transform.position = position;
            _player.transform.rotation = rotation;

        }

        currentPlayer.Positions.Add(new PreviousPositions(time, position));

        if (currentPlayer.Positions.Count >= 2) currentPlayer.Positions.RemoveAt(0);
    }

    /// <summary>
    ///     Gets other players colours.
    /// </summary>
    /// <param name="_packet">Colour to read</param>
    public static void PlayerColour(Packet _packet)
    {
        var id = _packet.ReadInt();
        var colour = _packet.ReadVector3();

        //For all players in the lobby if the player is the same as the colour, set that players model to that colour.
        foreach (var player in GameManager.players)
            if (player.Key.Equals(id))
            {
                Color colorHsv = new(colour.x, colour.y, colour.z, 1);
                var renderer = player.Value.GetComponentInChildren(typeof(MeshRenderer)) as MeshRenderer;
                renderer.material.color = colorHsv;
            }
    }

    /// <summary>
    ///     If player has disconnected, then disconnect them from all clients as well.
    /// </summary>
    /// <param name="_packet">Id of player to disconnect.</param>
    public static void PlayerDisconnected(Packet _packet)
    {
        var id = _packet.ReadInt();

        Destroy(GameManager.players[id].gameObject);
        GameManager.players.Remove(id);
    }

    /// <summary>
    ///     If server has requested for clients to spawn a coin somewhere.
    /// </summary>
    /// <param name="_packet">Position to spawn collectable</param>
    public static void SpawnCollectable(Packet _packet)
    {
        var position = _packet.ReadVector3();

        //Deletes all previous collectables (as this function will be ran after this or another player has already,
        //collected it their side.
        var previousCollectables = GameObject.FindGameObjectsWithTag("Collectable");
        foreach (var collectable in previousCollectables) Destroy(collectable);
        GameManager.instance.SpawnCollectable(position);
    }

    /// <summary>
    ///     If server has requested for clients to spawn a coin somewhere.
    /// </summary>
    /// <param name="_packet">Position to spawn collectable</param>
    public static void UpdateScore(Packet _packet)
    {
        var id = _packet.ReadInt();
        var score = _packet.ReadInt();

        //Loops through all players and updates their score.
        foreach (var player in GameManager.players.Values.Where(player => player.Id.Equals(id)))
        {
            player.Score = score;
            if(player.gameObject.CompareTag("Player"))
            {
                continue;
            }

            player.gameObject.GetComponentInChildren<HoverText>().scoreText.text = player.Score.ToString();
        }

    }

    /// <summary>
    ///     Stops the game (as server has shut down).
    /// </summary>
    /// <param name="_packet">Message to read out to clients before closing.</param>
    public static void StopServer(Packet _packet)
    {
        var msg = _packet.ReadString();

        Debug.Log(msg);
#if UNITY_EDITOR
        GameManager.instance.StopTimer();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //EditorApplication.isPlaying = false;
#endif
        GameManager.instance.StopTimer();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Application.Quit();
    }
}