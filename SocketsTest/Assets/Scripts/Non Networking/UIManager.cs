using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private Regex ipRegex;

    //Menu variables
    [Header("Menu")] 
    [SerializeField] private GameObject connectPanel;
    public TMP_InputField ipAddressField;
    public TMP_InputField usernameField;
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_Text errorText;
    //UI variables
    [Header("UI")] [SerializeField] 
    private GameObject hudPanel;
    public TMP_Text isPredictingText;
    public TMP_Text scoreText;
    public TMP_Text serverPosText;
    public TMP_Text timerText;




    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists");
            Destroy(this);
        }
    }

    private void Start()
    {
        //Regex to check if the IPv4 address entered is actually a valid IPv4 address
        //Regex from: https://stackoverflow.com/a/36760050
        ipRegex = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$",
            RegexOptions.Compiled
            | RegexOptions.IgnoreCase);

        if (!string.IsNullOrEmpty(StaticVariables.restartMessage))
        {
            errorText.gameObject.SetActive(true);
            errorText.text = StaticVariables.restartMessage;
        }
    }


    /// <summary>
    ///     Ran when connect to server button is pressed.
    /// </summary>
    public void ConnectToServer()
    {
        //Removes whitespace (vital for regex to work, and later connection to server)
        ipAddressField.text = ipAddressField.text.Trim();
        usernameField.text = usernameField.text.Trim();


        //If string empty then connect to local host
        if (string.IsNullOrEmpty(ipAddressField.text))
        {
            ipAddressField.text = "127.0.01";
            ConnectingText();
            Client.instance.ConnectToServer();
            return;
        }

        //If a valid Ip address then connect to that IP
        if (ipRegex.IsMatch(ipAddressField.text))
        {
            //And checks if the username is valid.
            if (string.IsNullOrEmpty(usernameField.text))
            {
                StartCoroutine(DisplayError("Please enter a valid username"));
                return;
            }


            //Start the process of connecting to the server.
            ConnectingText();
            Client.instance.ConnectToServer();
        }
        //In the case it's not a valid IP address then ask user to redo
        else
        {
            StartCoroutine(DisplayError("Please enter a valid IP address"));
        }
    }

    public void ReturntoMainMenu()
    {
        if (Client.instance.isConnected) GameManager.instance.EndGame();
    }

    public void Exit()
    {
        if (Client.instance.isConnected) Client.instance.Disconnect();
#if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
    }

    /// <summary>
    ///     Text that shows up when player is actively connecting,
    ///     will only really be seen if player takes a while to connect (timeout is 5 seconds).
    /// </summary>
    private void ConnectingText()
    {
        //Hides menu to go to loading screen.
        connectButton.interactable = false;
        usernameField.interactable = false;
        ipAddressField.interactable = false;
        connectPanel.SetActive(false);

        errorText.gameObject.SetActive(true);
        errorText.color = Color.black;
        errorText.text = $"Connecting to {ipAddressField.text}...";
    }


    /// <summary>
    ///     Toggles the menu
    /// </summary>
    /// <param name="_isMenuHidden">Should the main menu be shown or not</param>
    /// <param name="_errorMessage">Optional, error message shown</param>
    public void MenuToggle(bool _isMenuHidden = true, string _errorMessage = "")
    {
        //If menu is hidden then display HUD
        if (_isMenuHidden)
        {
            errorText.gameObject.SetActive(false);
            connectPanel.SetActive(false);
            hudPanel.SetActive(true);
        }
        //Show menu.
        else
        {
            errorText.gameObject.SetActive(true);
            errorText.text = _errorMessage;
            ipAddressField.text = "";
            connectPanel.SetActive(true);
            hudPanel.SetActive(false);

            connectButton.interactable = true;
            usernameField.interactable = true;
            ipAddressField.interactable = true;
            usernameField.gameObject.SetActive(true);
        }
    }

    /// <summary>
    ///     Displays Error message to user using a text field.
    /// </summary>
    /// <param name="errorMessage">The error message to display.</param>
    /// <returns>The same object until a set time has passed. </returns>
    public IEnumerator DisplayError(string errorMessage)
    {
        errorText.gameObject.SetActive(true);
        errorText.text = errorMessage;
        yield return new WaitForSeconds(3);
        errorText.gameObject.SetActive(false);
        errorText.text = "";
    }
}