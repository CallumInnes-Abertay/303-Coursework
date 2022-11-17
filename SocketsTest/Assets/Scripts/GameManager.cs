using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public static Dictionary<int, PlayerManager> players = new();
    [SerializeField] private GameObject collectablePrefab;
    [SerializeField] private GameObject externalPlayerPrefab;

    [SerializeField] private GameObject localPlayerPrefab;

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

    /// <summary>Spawns a player.</summary>
    /// <param name="_id">The player's ID.</param>
    /// <param name="_username"></param>
    /// <param name="_position">The player's starting position.</param>
    /// <param name="_rotation">The player's starting rotation.</param>
    /// <param name="_colour">The colour of the player.</param>
    public void SpawnPlayer(int _id, string _username, Vector3 _position, Quaternion _rotation, Vector3 _colour)
    {
        GameObject _player;

        //If the id is the same as the current user id, then spawn the player the user will be playing as.
        if (_id == Client.instance.myId)
        {
            _player = Instantiate(localPlayerPrefab, _position, _rotation);
        }
        //If not then it's a seperate player, so spawn the external prefab the connecting player will play as.
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
    ///     Spawns collectable
    /// </summary>
    /// <param name="collectablePos">Position to spawn collectable.</param>
    public void SpawnCollectable(Vector3 collectablePos)
    {
        Instantiate(collectablePrefab, collectablePos, Quaternion.identity);
    }
}