using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    /// <summary>
    /// Confirms the server has us in the server.
    /// </summary>
    /// <param name="_packet">Confirmation message from the server.</param>
    public static void Welcome(Packet _packet)
    {
        var msg = _packet.ReadString();
        var myId = _packet.ReadInt();

        Debug.Log($"Message from server: {msg}");
        Client.instance.myId = myId;
        //Tell the server the we've received the confirmation.
        ClientSend.WelcomeReceived();

        // Now that we have the client's id, we can by connect UDP (cant be same port).
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    /// <summary>
    /// //Starts the client side timer synced up with the server (adjusted for ping).
    /// </summary>
    /// <param name="_packet"></param>
    public static void StartTimer(Packet _packet)
    {
        var time = _packet.ReadInt();

        GameManager.instance.StartTimer(time);
    }

    /// <summary>
    /// Spawns the player into the game.
    /// </summary>
    /// <param name="_packet">The id, username, position,rotation and colour of the player.</param>
    public static void SpawnPlayer(Packet _packet)
    {
        var id = _packet.ReadInt();
        var username = _packet.ReadString();
        var position = _packet.ReadVector3();
        var rotation = _packet.ReadQuaternion();
        var colour = _packet.ReadVector3();

        Debug.Log($"Spawning player {id}: {username}");
        //Spawns the player the same as the server.
        GameManager.instance.SpawnPlayer(id, username, position, rotation, colour);
    }

    /// <summary>
    /// The players position calculated by the inputs and the tick it was calculated at.
    /// </summary>
    /// <param name="_packet"></param>
    public static void PlayerPosition(Packet _packet)
    {
        var id = _packet.ReadInt();
        var tick = _packet.ReadInt();
        var newPosition = _packet.ReadVector3();

        //Lets us see by how much the server or client are trailing by.
        UIManager.instance.serverPosText.text = $"Server Tick: {tick} has position at {newPosition}";

        if (GameManager.players.TryGetValue(id, out var _player))
        {
            //If local player.
            if (id.Equals(Client.instance.myId))
            {
                if (!_player.ServerCorrection(newPosition, tick))
                {
                    //Debug.Log("Correction failed");
                }
            }
            //If external player.
            else
            {
                _player.transform.position = newPosition;
                //_player.SetPosition(newPosition);
                _player.Positions.Add(new PreviousPositions(tick, newPosition));
                if (_player.Positions.Count >= 10) _player.Positions.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Updates the external players rotation.
    /// </summary>
    /// <param name="_packet">The rotation to read</param>
    public static void PlayerRotation(Packet _packet)
    {
        var id = _packet.ReadInt();
        var rotation = _packet.ReadQuaternion();

        if (GameManager.players.TryGetValue(id, out var _player)) _player.transform.rotation = rotation;
    }

    /// <summary>
    /// Gets other players colours.
    /// </summary>
    /// <param name="_packet">Colour to read</param>
    public static void PlayerColour(Packet _packet)
    {
        var id = _packet.ReadInt();
        var colour = _packet.ReadVector3();

        //For all players in the lobby if the player has the same id as the incoming id,set that players model to the incoming colour.
        foreach (var player in GameManager.players)
            if (player.Key.Equals(id))
            {
                Color colorHsv = new(colour.x, colour.y, colour.z, 1);
                var meshRenderer = player.Value.GetComponentInChildren<MeshRenderer>();
                meshRenderer.material.color = colorHsv;
            }
    }

    /// <summary>
    /// If player has disconnected, then disconnect them from all clients as well.
    /// </summary>
    /// <param name="_packet">Id of player to disconnect.</param>
    public static void PlayerDisconnected(Packet _packet)
    {
        var id = _packet.ReadInt();

        Destroy(GameManager.players[id].gameObject);
        GameManager.players.Remove(id);
    }

    /// <summary>
    /// If server has requested for clients to spawn a coin somewhere.
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
    /// If server has requested for clients to spawn a coin somewhere.
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
            //Local player does not have "hovertext" so can continue early.
            if (player.gameObject.CompareTag("Player")) continue;

            //Updates the score to the UI above player
            player.gameObject.GetComponentInChildren<HoverText>().scoreText.text = player.Score.ToString();
        }
    }

    /// <summary>
    /// Stops the game (as server has shut down).
    /// </summary>
    /// <param name="_packet">Message to read out to clients before closing.</param>
    public static void StopServer(Packet _packet)
    {
        var msg = _packet.ReadString();

        Debug.Log(msg);

#if UNITY_EDITOR
        GameManager.instance.StopTimer();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        EditorApplication.isPlaying = false;
#endif
        GameManager.instance.StopTimer();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Application.Quit();
    }
}