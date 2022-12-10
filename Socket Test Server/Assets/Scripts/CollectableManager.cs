using System.Collections.Generic;
using UnityEngine;

public class CollectableManager : MonoBehaviour
{
    public static CollectableManager instance;

    [SerializeField] private List<GameObject> collectables = new();
    //The last postion of the thing.
    private Vector3 previousPosition;

    //Singleton
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
        previousPosition = Vector3.zero;
    }

    /// <summary>
    /// Spawns a collectable in all clients
    /// </summary>
    /// <param name="newPos">If its should be a new position (the old one has been collected) or an old one (for joining players)</param>
    public void SpawnCollectable(bool newPos = true)
    {
        /*
        If a new collectable is needed to be spawned choose from a random position 
        this could be because it's the first collectable of the cycle, or because the position is meant to be sent 
        a new joining player
        */
        if (newPos)
        {
            var randomPosition = collectables[Random.Range(0, collectables.Count)];

            if (randomPosition.transform.position == previousPosition)
            {
                 randomPosition = collectables[Random.Range(0, collectables.Count)];
            }

            previousPosition = randomPosition.transform.position;

            ServerSend.SpawnCollectable(randomPosition.transform.position);
        }
        //In the case a new one isn't meant to be spawned then send the previous position.
        else
        {
            ServerSend.SpawnCollectable(previousPosition);
        }
    }
}