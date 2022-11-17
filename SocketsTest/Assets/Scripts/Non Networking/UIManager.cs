using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Menu")] public TMP_InputField AddressField;

    [SerializeField] private Button connectButton;
    [SerializeField] public TMP_Text errorText;

    [Header("UI")] [SerializeField] private GameObject hudPanel;

    private Regex ipRegex;
    public TMP_Text scoreText;
    [SerializeField] private GameObject startMenuPanel;
    public TMP_InputField usernameField;


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
    }

    /// <summary>
    ///     Displays Error message to user using a text field.
    /// </summary>
    /// <param name="errorMessage">The error message to display.</param>
    /// <returns>Nothing.</returns>
    private IEnumerator DisplayError(string errorMessage)
    {
        errorText.gameObject.SetActive(true);
        errorText.text = errorMessage;
        yield return new WaitForSeconds(3);
        errorText.gameObject.SetActive(false);
        errorText.text = "";
    }


    /// <summary>
    ///     Ran when connect to server button is pressed.
    /// </summary>
    public void ConnectToServer()
    {
        //Removes whitespace (vital for regex to work, and later connection to thus ip)
        AddressField.text = AddressField.text.Trim();
        usernameField.text = usernameField.text.Trim();


        //If string empty then connect to local host
        if (string.IsNullOrEmpty(AddressField.text))
        {
            AddressField.text = "127.0.01";
            ConnectingText();
            Client.instance.ConnectToServer();
            return;
        }

        //If a valid Ip address then connect to that IP
        if (ipRegex.IsMatch(AddressField.text))
        {
            //And checks if the username is valid.
            if (string.IsNullOrEmpty(usernameField.text))
            {
                StartCoroutine(DisplayError("Please enter a valid username"));
                return;
            }

            ConnectingText();

            Client.instance.ConnectToServer();
        }
        //In the case it's not a valid IP address then ask user to redo
        else
        {
            StartCoroutine(DisplayError("Please enter a valid IP address"));
        }
    }

    /// <summary>
    ///     Text that shows up when player is actively connecting,
    ///     will only really be seen if player takes a while to connect (timeout is 5 seconds).
    /// </summary>
    private void ConnectingText()
    {
        connectButton.interactable = false;
        usernameField.interactable = false;
        AddressField.interactable = false;
        connectButton.gameObject.SetActive(false);
        usernameField.gameObject.SetActive(false);
        AddressField.gameObject.SetActive(false);

        errorText.gameObject.SetActive(true);
        errorText.color = Color.black;
        errorText.text = "Connecting";
    }

    public void HideMenu()
    {
        errorText.gameObject.SetActive(false);
        startMenuPanel.SetActive(false);
        hudPanel.SetActive(true);
    }
}