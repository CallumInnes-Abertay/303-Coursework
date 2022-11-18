using System.Collections.Generic;
using UnityEngine;

public class CollectableManager : MonoBehaviour
{
    public static CollectableManager instance;

    [SerializeField] private List<GameObject> collectables = new();
    private Transform previousPosition;

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


            previousPosition = randomPosition.transform;

            ServerSend.SpawnCollectable(randomPosition.transform.position);
        }
        //In the case a new one isn't meant to be spawned then send the previous position.
        else
        {
            ServerSend.SpawnCollectable(previousPosition.position);
        }
    }
}