
using UnityEngine;

/// <summary>
/// Class to store time and past position of all players.
/// </summary>
public class PreviousPositions
{
    public int Time { get; }
    public Vector3 Position { get; }

    public PreviousPositions(int _time, Vector3 _position)
    {
        Time = _time;
        Position = _position;
    }
}
