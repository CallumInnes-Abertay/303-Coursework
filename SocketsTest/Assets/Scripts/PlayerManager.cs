using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int Id { get; set; }
    public string Username { get; set; }
    public int Score { get; set; }
    [ItemCanBeNull]
    public List<PreviousPositions> Positions { get; } = new();
    [SerializeField] [Range(0.1f, 5)] private float correctionThreshold = 1;



    private void Start()
    {
        fromPos = transform.position;
        toPos = transform.position;
    }

    private Vector3 fromPos = Vector3.zero;
    private Vector3 toPos = Vector3.zero;
    private float lastTime;

    public void SetPosition(Vector3 position)
    {
        fromPos = toPos;
        toPos = position;
        lastTime = Time.time;
    }

    /// <summary>
    /// Snaps the player back to the correct position if the clients prediction is too far off.
    /// </summary>
    /// <param name="_position">The position to check</param>
    /// <param name="_tick">The tick this value relates to</param>
    /// <returns>If they could sync the client to the server or not.</returns>
    public bool ServerCorrection(Vector3 _position, int _tick)
    {
        var currentTime = Positions.Where(x => x.Tick == _tick).FirstOrDefault();

        if (currentTime == null)
            return false;
        Debug.Log($"Correcting position at Server time: {_tick} Client time: {currentTime.Tick}");

        //Checks the clients predicted path to the servers actual position for tha 
        var distance =
            Vector3.Distance(new Vector3(currentTime.Position.x, currentTime.Position.y, currentTime.Position.z),
                _position);
        if (distance > correctionThreshold)
        {
            //And in the case that it has strayed too far off.
            Debug.Log($"Threshold exceeded by {distance}, fixing time");
            //Snap the player back
            transform.position = _position;
            //SetPosition(_position);
        }

        //If it's not then we can accept the clients predicted movement was good enough
        //Then remove the position
        Positions.RemoveAll(x => x.Tick <= _tick);
        return true;
    }

    private void Update()
    {
        if (Id != Client.instance.myId)
        {
            transform.position = Vector3.Lerp(fromPos, toPos, (Time.time - lastTime) / (1.0f / 30));
        }
    }



}