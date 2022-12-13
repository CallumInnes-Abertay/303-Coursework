using System.Collections;
using TMPro;
using UnityEngine;

public class HoverText : MonoBehaviour
{
    private PlayerManager playerManager;
    public TMP_Text scoreText;
    [SerializeField] private GameObject toTrack;

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

    /// <summary>
    ///     Delays the start to wait until the local player spawns in so that it knows what to track.
    /// </summary>
    /// <returns>The same object as it loops.</returns>
    private IEnumerator DelayedStart()
    {
        //Waits until it can find the player (until it has spawned in)
        yield return new WaitUntil(() => GameObject.FindWithTag("Player") != null);
        //Finds the player to track.
        toTrack = GameObject.FindWithTag("Player");
    }

    private void Update()
    {
        if (toTrack == null) return;

        //Billboarding affect so the UI always faces the camera.
        transform.rotation = Quaternion.LookRotation(transform.position - toTrack.transform.position);
    }
}