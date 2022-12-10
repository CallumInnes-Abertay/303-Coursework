
using UnityEngine;

/// <summary>
/// Class to store time and past position of all players.
/// </summary>
public class PreviousPositions
{
    public int Tick { get; }
    public Vector3 Position { get; }

    //Constructor
    public PreviousPositions(int _tick, Vector3 _position)
    {
        Tick = _tick;
        Position = _position;
    }
}
