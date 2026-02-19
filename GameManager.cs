using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } }
    
    [Header("Game Settings")]
    public bool autoSave = true;
    public float autoSaveInterval = 60f;
    
    [Header("References")]
    public Camera mainCamera;
    public Transform characterSpawnPoint;
    
    private float lastSaveTime;
    private bool isPaused = false;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeGame();
    }
    
    void Update()
    {
        // Auto save
        if (autoSave && Time.time - lastSaveTime > autoSaveInterval)
        {
            SaveGame();
            lastSaveTime = Time.time;
        }
        
        // Handle back button on Android
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackButtonPressed();
        }
    }
    
    void InitializeGame()
    {
        // Ensure we have a main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // Setup camera for pixel art
        SetupPixelPerfectCamera();
        
        // Load game
        LoadGame();
        
        lastSaveTime = Time.time;
        
        Debug.Log("Game initialized!");
    }
    
    void SetupPixelPerfectCamera()
    {
        if (mainCamera != null)
        {
            // Set orthographic size for pixel art
            // 1080p / 32 pixels per unit / 2 = ~16.875
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 8f;
            
            // Set background color
            mainCamera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        }
    }
    
    public void SaveGame()
    {
        if (CharacterAI.Instance != null && SaveManager.Instance != null)
        {
            CharacterData data = CharacterAI.Instance.GetCharacterData();
            SaveManager.Instance.SaveCharacter(data);
            Debug.Log("Game auto-saved!");
        }
    }
    
    public void LoadGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadCharacter();
        }
    }
    
    public void NewGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    void OnBackButtonPressed()
    {
        // Show pause menu or save and exit
        SaveGame();
        
        #if UNITY_ANDROID
        // On Android, minimize app
        AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
            .GetStatic<AndroidJavaObject>("currentActivity");
        activity.Call<bool>("moveTaskToBack", true);
        #endif
    }
    
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }
    
    public bool IsPaused()
    {
        return isPaused;
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }
    
    void OnApplicationQuit()
    {
        SaveGame();
    }
}
