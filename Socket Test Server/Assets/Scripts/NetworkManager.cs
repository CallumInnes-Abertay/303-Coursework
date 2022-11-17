using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    [SerializeField] private GameObject playerPrefab;

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

        Server.Start(12, 50000);
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
    public Player InstantiatePlayer(string username)
    {
        playerPrefab.gameObject.name = $"Player {username}";
        return Instantiate(playerPrefab, new Vector3(0f, 0.5f, 0f), Quaternion.identity,null).GetComponent<Player>();
    }
}