using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance { get { return _instance; } }
    
    private string savePath;
    private CharacterData currentData;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSavePath();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeSavePath()
    {
        // Use persistentDataPath for Android compatibility
        savePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
        Debug.Log("Save path: " + savePath);
    }
    
    public void SaveCharacter(CharacterData data)
    {
        data.lastSaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string json = JsonUtility.ToJson(data, true);
        
        try
        {
            File.WriteAllText(savePath, json);
            Debug.Log("Character saved successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save: " + e.Message);
        }
    }
    
    public CharacterData LoadCharacter()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                currentData = JsonUtility.FromJson<CharacterData>(json);
                
                if (currentData == null)
                {
                    currentData = new CharacterData();
                }
                
                Debug.Log("Character loaded! Evolution level: " + currentData.evolutionLevel);
                return currentData;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load: " + e.Message);
                return new CharacterData();
            }
        }
        else
        {
            Debug.Log("No save file found. Creating new character.");
            return new CharacterData();
        }
    }
    
    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save deleted.");
        }
    }
    
    public bool HasSaveFile()
    {
        return File.Exists(savePath);
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && CharacterAI.Instance != null)
        {
            SaveCharacter(CharacterAI.Instance.GetCharacterData());
        }
    }
    
    void OnApplicationQuit()
    {
        if (CharacterAI.Instance != null)
        {
            SaveCharacter(CharacterAI.Instance.GetCharacterData());
        }
    }
}
