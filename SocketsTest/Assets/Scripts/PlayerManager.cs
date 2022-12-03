using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int Id { get; set; }
    public string Username { get; set; }
    public int Score { get; set; }

    [ItemCanBeNull]
    public List<PreviousPositions> Positions { get; set; } = new();

    private void Start()
    {
        Positions.Capacity = 2;
    }

}