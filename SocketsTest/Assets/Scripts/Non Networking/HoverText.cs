using System.Collections;
using TMPro;
using UnityEngine;

public class HoverText : MonoBehaviour
{
    [SerializeField] private GameObject toTrack;
    private PlayerManager playerManager; 
    public TMP_Text scoreText;

    [SerializeField] private TMP_Text usernameText;


    private void Awake()
    {
        playerManager = GetComponentInParent<PlayerManager>();
    }

    private void Start()
    {
        usernameText.text = playerManager.Username;
        scoreText.text = playerManager.Score.ToString();

        //Cant find the local player at start as they won't have spawned in yet, so have to delay it.
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return 1;
        
        //Finds the player to track.
        toTrack = GameObject.FindWithTag("Player");
    }

    private void Update()
    {
        if (toTrack == null)
        {
            return;
        }

        //Billboarding affect so the UI always faces the camera.
        transform.rotation = Quaternion.LookRotation(transform.position - toTrack.transform.position);
    }
}