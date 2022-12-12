using System;
using System.Collections.Generic;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    //List of functions to run.
    private static readonly List<Action> executeOnMainThread = new();
    private static readonly List<Action> executeCopiedOnMainThread = new();

    private static bool actionToExecuteOnMainThread;

    private void Update()
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

        //Mutex to stop race conditions on list.
        lock (executeOnMainThread)
        {
            executeOnMainThread.Add(_action);
            actionToExecuteOnMainThread = true;
        }
    }

    /// <summary>Executes all code meant to run on the main thread.</summary>
    private static void UpdateMain()
    {
        if (!actionToExecuteOnMainThread)
            return;

        executeCopiedOnMainThread.Clear();
        //Mutex to stop race conditions on list.
        lock (executeOnMainThread)
        {
            executeCopiedOnMainThread.AddRange(executeOnMainThread);
            executeOnMainThread.Clear();
            actionToExecuteOnMainThread = false;
        }

        //Runs all handle functions now on the main thread.
        foreach (var functionToRun in executeCopiedOnMainThread)
            functionToRun();
    }

}