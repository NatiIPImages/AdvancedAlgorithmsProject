using UnityEngine;

// This class takes care of playing the music.
// Note: We must use a seperate class because we want the music to work between scenes!
public class MusicPlayer : MonoBehaviour
{
    // Stores the instance of the music player
    private static MusicPlayer instance;

    // AudioSource for playing music
    public AudioSource musicAudioSource;

    void Awake()
    {
        // Singleton pattern to ensure only one instance persists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Prevent this object from being destroyed on scene load
        }
        else
        {
            Destroy(gameObject); // Destroy this instance if another one already exists
        }
    }

    // This function gets executed on game start
    void Start()
    {
        // If our music is not null (which means if it exists)
        if (musicAudioSource != null)
        {
            // Play the audio source (our music)
            musicAudioSource.Play();
        }
    }
}
