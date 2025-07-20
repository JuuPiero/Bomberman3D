using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    [Header("SFX Settings")]
    public List<Sound> sfxList = new List<Sound>();
    public int maxPoolSize = 10; // Số lượng AudioSource tối đa
    public int maxSameSFX = 3; // Số lượng SFX giống nhau được phát cùng lúc

    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, int> activeSFXCount = new Dictionary<string, int>();
    private List<AudioSource> audioSourcePool = new List<AudioSource>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Khởi tạo Dictionary từ sfxList để tra cứu nhanh bằng tên
        foreach (Sound sound in sfxList)
        {
            if (sound.clip != null)
            {
                sound.clip.LoadAudioData(); // Tải trước dữ liệu âm thanh
            }
            if (!sfxDictionary.ContainsKey(sound.name))
            {
                sfxDictionary.Add(sound.name, sound.clip);
            }
        }

        // Khởi tạo AudioSource Pool
        for (int i = 0; i < maxPoolSize; i++)
        {
            CreateAudioSource();
        }
    }

    private AudioSource CreateAudioSource()
    {
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.ignoreListenerPause = true; // Phát ngay cả khi game pause
        audioSourcePool.Add(newSource);
        return newSource;
    }

    // Hàm chính phát âm thanh theo tên
    public void PlaySFX(string soundName, float volume = 1.0f)
    {
        if (!sfxDictionary.TryGetValue(soundName, out AudioClip clip)) 
        {
            Debug.LogWarning($"SFX '{soundName}' không tồn tại!");
            return;
        }

        // Kiểm tra giới hạn SFX
        int currentCount = activeSFXCount.GetValueOrDefault(soundName, 0);
        if (currentCount >= maxSameSFX) return;
        activeSFXCount[soundName] = currentCount + 1;

        // Tìm AudioSource rảnh
        AudioSource freeSource = audioSourcePool.FirstOrDefault(s => !s.isPlaying);
        if (freeSource == null)
        {
            if (audioSourcePool.Count < maxPoolSize)
            {
                freeSource = CreateAudioSource();
            }
            else
            {
                Debug.LogWarning("Đã đạt giới hạn AudioSource Pool!");
                return;
            }
        }

        // Phát âm thanh tối ưu
        freeSource.PlayOneShot(clip, volume);
        StartCoroutine(ReleaseSFXCounter(soundName));
    }

    private IEnumerator ReleaseSFXCounter(string soundName)
    {
        yield return new WaitForSeconds(sfxDictionary[soundName].length);
        
        if (activeSFXCount.ContainsKey(soundName))
        {
            activeSFXCount[soundName]--;
            if (activeSFXCount[soundName] <= 0)
                activeSFXCount.Remove(soundName);
        }
    }
}