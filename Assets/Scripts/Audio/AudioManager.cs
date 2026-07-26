using UnityEngine;

namespace T60.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private float musicVolume = 0.8f;

        [Header("SFX")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip[] sfxClips;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource != null)
            {
                musicSource.clip = musicClip;
                musicSource.loop = true;
                musicSource.volume = musicVolume;
                musicSource.playOnAwake = false;
                musicSource.Play();
            }
        }

        public void PlayRandomSfx()
        {
            if (sfxSource == null || sfxClips == null || sfxClips.Length == 0) return;

            AudioClip clip = sfxClips[Random.Range(0, sfxClips.Length)];
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
