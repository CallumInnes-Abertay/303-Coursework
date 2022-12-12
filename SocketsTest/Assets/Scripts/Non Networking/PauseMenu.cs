using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private CameraController cameraScript;

    private bool isPaused;

    [SerializeField] private GameObject pausePnl;

    private void Update()
    {
        if (pausePnl != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isPaused = !isPaused;
                if (isPaused)
                {
                    cameraScript.enabled = false;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    cameraScript.enabled = true;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

            pausePnl.SetActive(isPaused);
        }
    }
}