using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private Vector3 fromPos = Vector3.zero;
    private float lastTime;
    private Vector3 toPos = Vector3.zero;
    public int Id { get; set; }
    public string Username { get; set; }
    public int Score { get; set; }
    public List<PreviousPositions> Positions { get; } = new(); 


    private void Start()
    {
        fromPos = transform.position;
        toPos = transform.position;
    }

    /// <summary>
    /// Positions to lerp to (note couldnt get to work)
    /// </summary>
    /// <param name="_position">position to lerp to</param>
    public void SetPosition(Vector3 _position)
    {
        fromPos = toPos;
        toPos = _position;
        lastTime = Time.time;
    }


    //Runs lerp 
    private void Update()
    {
        //if (Id != Client.instance.myId)
        //{
        //    transform.position = Vector3.Lerp(fromPos, toPos, (Time.time - lastTime) / (1.0f / 30));
        //}
    }
}