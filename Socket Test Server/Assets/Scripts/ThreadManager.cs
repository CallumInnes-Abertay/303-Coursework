using System;
using System.Collections.Generic;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    private static readonly List<Action> executeOnMainThread = new();
    private static readonly List<Action> executeCopiedOnMainThread = new();
    private static bool actionToExecuteOnMainThread;


    private void FixedUpdate()
    {
        UpdateMain();
    }


    /// <summary>Sets an action to be executed on the main thread.</summary>
    /// <param name="_action">The action to be executed on the main thread.</param>
    public static void ExecuteOnMainThread(Action _action)
    {
        if (_action == null)
        {
            Debug.Log("No action to execute on main thread!");
            return;
        }

        lock (executeOnMainThread)
        {
            executeOnMainThread.Add(_action);
            actionToExecuteOnMainThread = true;
        }
    }

    /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
    public static void UpdateMain()
    {
        if (!actionToExecuteOnMainThread)
            return;
        executeCopiedOnMainThread.Clear();
        lock (executeOnMainThread)
        {
            executeCopiedOnMainThread.AddRange(executeOnMainThread);
            executeOnMainThread.Clear();
            actionToExecuteOnMainThread = false;
        }

        foreach (var action in executeCopiedOnMainThread)
            action();
    }
}