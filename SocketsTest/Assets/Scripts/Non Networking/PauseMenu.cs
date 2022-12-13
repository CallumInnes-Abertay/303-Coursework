using System.Collections;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    private CameraController cameraScript;

    private bool isPaused;

    [SerializeField] private GameObject pausePnl;

    private GameObject playerObject;

    private void Start()
    {
        StartCoroutine(DelayedStart());
    }

    /// <summary>
    ///     Delays the start of pause menu to wait for the player to spawn first, then get it's camera.
    /// </summary>
    /// <returns>The same object as it loops.</returns>
    private IEnumerator DelayedStart()
    {
        //
        yield return new WaitUntil(() => GameObject.FindWithTag("Player") != null);
        //Finds the player to track.
        playerObject = GameObject.FindWithTag("Player");
        cameraScript = playerObject.GetComponentInChildren<CameraController>();
    }

    private void Update()
    {
        if (Client.instance.isConnected) TogglePauseMenu();
    }

    /// <summary>
    ///     Sets the sensitivity of the players camera.
    /// </summary>
    /// <param name="_sensitivity">The value of the volume.</param>
    public void SetSensitivity(float _sensitivity)
    {
        print($"The sensitivity has changed to {_sensitivity}");
        //Saves this sensitivity so if the player reloads it'll keep their sensitivity.
        PlayerPrefs.SetFloat("sensitivity", _sensitivity);
        cameraScript.Sensitivity = _sensitivity;
    }

    /// <summary>
    ///     Allows toggling of pause Menu.
    /// </summary>
    private void TogglePauseMenu()
    {
        if (pausePnl != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isPaused = !isPaused;
                if (isPaused)
                {
                    cameraScript.enabled = false;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    cameraScript.enabled = true;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            pausePnl.SetActive(isPaused);
        }
    }
}