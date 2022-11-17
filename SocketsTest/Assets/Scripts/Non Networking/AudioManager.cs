using System.Collections;
using UnityEngine;

/// <summary>
///     Used to handle audio across scenes.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    private bool firstMusicSourceIsPlaying;

    private AudioSource musicSource;
    private AudioSource musicSource2;
    private AudioSource sfxSource;

    //Makes use of the singleton pattern to create an instance that can be accessed from anywhere in the level.
    public static AudioManager Instance

    {
        get
        {
            // If doesnt have an instance of AudioManager get one
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();
                //If still doesn't have a AudioManager then it spawns one of its own.
                if (instance == null)
                    instance = new GameObject("Spawned AudioManager", typeof(AudioManager))
                        .GetComponent<AudioManager>();
            }

            //Returns the instance of type audiomanager
            return instance;
        }

        //Private setter as the AudioManager doesn't need to changed anywhere else other than this AudioManager.
        private set => instance = value;
    }

    private void Awake()
    {
        //Adds 2 audio sources for music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource2 = gameObject.AddComponent<AudioSource>();
        //Adds an audio source for sfx
        sfxSource = gameObject.GetComponent<AudioSource>();

        //Makes the Audiomanager persist throughout on scene change.
        DontDestroyOnLoad(gameObject);

        //Loops the music tracks, just incase a new one doesn't play in time.
        musicSource.loop = true;
        musicSource2.loop = true;
    }

    /// <summary>
    ///     Used to play a song with an Audio clip.
    /// </summary>
    /// <param name="musicClip">The song you want to play.</param>
    public void PlayMusic(AudioClip musicClip)
    {
        //A ternary operator, essentially a if statement, if firstMusicSourceIsPlaying is true.
        var activeSource = firstMusicSourceIsPlaying ? musicSource : musicSource2;
        //Sets the active audio source to the music clip.
        activeSource.clip = musicClip;
        //And plays the song
        activeSource.Play();
    }

    /// <summary>
    ///     Plays the audio clip with a noticeable fade in and fade out.
    /// </summary>
    /// <param name="newclip">The song that you want to play.</param>
    /// <param name="transitionTime">How long to transition in and out of it.</param>
    public void PlayMusicWithFade(AudioClip newclip, float transitionTime = 1.0f)
    {
        //A ternary operator, essentially a if statement, if firstMusicSourceIsPlaying is true. 
        var activeSource = firstMusicSourceIsPlaying ? musicSource : musicSource2;
        //Starts the coroutine and passes through the parameters.
        StartCoroutine(UpdateMusicWithFade(activeSource, newclip, transitionTime));
    }

    private IEnumerator UpdateMusicWithFade(AudioSource activeSource, AudioClip newClip, float transitionTime)
    {
        //If not playing something, play it.
        if (!activeSource.isPlaying) activeSource.Play();

        float t;
        // Fade out
        for (t = 0; t < transitionTime; t += Time.deltaTime)
        {
            activeSource.volume = 1 - t / transitionTime;
            yield return null;
        }

        //Stops playing the song.
        activeSource.Stop();
        //Sets the audiosource to a new song.
        activeSource.clip = newClip;
        //Play that song.
        activeSource.Play();

        // Fade in.
        for (t = 0; t < transitionTime; t += Time.deltaTime)
        {
            //Fades the volume in.
            activeSource.volume = t / transitionTime;
            //No wait time.
            yield return null;
        }
    }

    /// <summary>
    ///     Plays a sound effect with an Audio clip.
    /// </summary>
    /// <param name="sfxClip">The sound effect to be played.</param>
    public void PlaySound(AudioClip sfxClip)
    {
        sfxSource.clip = sfxClip;
        //Oneshot used so sounds don't overlap
        sfxSource.PlayOneShot(sfxClip);
    }

    /// <summary>
    ///     Overloaded Playsound method with volume control.
    /// </summary>
    /// <param name="sfxClip">The sound effect to be played.</param>
    /// <param name="volume">The volume of the sound effect.</param>
    public void PlaySound(AudioClip sfxClip, int volume)
    {
        //Sets the audiosource to the clip.
        sfxSource.clip = sfxClip;
        //Oneshot used so sounds don't overlap.
        sfxSource.PlayOneShot(sfxClip);
    }
}