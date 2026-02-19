using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ObjectManager : MonoBehaviour
{
    private static ObjectManager _instance;
    public static ObjectManager Instance { get { return _instance; } }
    
    [Header("Settings")]
    public string objectsFolderName = "ObjectsFolder";
    public GameObject objectPrefab;
    public Transform worldContainer;
    
    [Header("Spawn Area")]
    public float minX = -4f;
    public float maxX = 4f;
    public float minY = -2f;
    public float maxY = 2f;
    
    private List<WorldObject> spawnedObjects = new List<WorldObject>();
    private string objectsFolderPath;
    private FileSystemWatcher fileWatcher;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeFolderPath();
        LoadAllObjects();
        SetupFileWatcher();
    }
    
    void InitializeFolderPath()
    {
        // For Android, use persistentDataPath
        objectsFolderPath = Path.Combine(Application.persistentDataPath, objectsFolderName);
        
        // Create folder if it doesn't exist
        if (!Directory.Exists(objectsFolderPath))
        {
            Directory.CreateDirectory(objectsFolderPath);
            Debug.Log("Created objects folder: " + objectsFolderPath);
            
            // Copy example objects from StreamingAssets if available
            CopyExampleObjects();
        }
    }
    
    void CopyExampleObjects()
    {
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "ExampleObjects");
        if (Directory.Exists(streamingPath))
        {
            foreach (string file in Directory.GetFiles(streamingPath))
            {
                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(objectsFolderPath, fileName);
                File.Copy(file, destPath, true);
            }
            Debug.Log("Copied example objects to: " + objectsFolderPath);
        }
    }
    
    void SetupFileWatcher()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // FileWatcher doesn't work well on Android, use polling instead
            InvokeRepeating("CheckForNewObjects", 5f, 5f);
        }
        else
        {
            try
            {
                fileWatcher = new FileSystemWatcher(objectsFolderPath);
                fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                fileWatcher.Filter = "*.json";
                fileWatcher.Created += OnNewObjectFile;
                fileWatcher.Changed += OnObjectFileChanged;
                fileWatcher.EnableRaisingEvents = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("FileWatcher failed, using polling: " + e.Message);
                InvokeRepeating("CheckForNewObjects", 5f, 5f);
            }
        }
    }
    
    void CheckForNewObjects()
    {
        LoadAllObjects();
    }
    
    void OnNewObjectFile(object sender, FileSystemEventArgs e)
    {
        Debug.Log("New object file detected: " + e.FullPath);
        LoadObjectFromFile(e.FullPath);
    }
    
    void OnObjectFileChanged(object sender, FileSystemEventArgs e)
    {
        Debug.Log("Object file changed: " + e.FullPath);
        // Reload all objects to capture changes
        LoadAllObjects();
    }
    
    public void LoadAllObjects()
    {
        // Clear existing objects
        ClearObjects();
        
        if (!Directory.Exists(objectsFolderPath))
        {
            Debug.LogWarning("Objects folder not found: " + objectsFolderPath);
            return;
        }
        
        // Find all JSON files
        string[] jsonFiles = Directory.GetFiles(objectsFolderPath, "*.json");
        
        foreach (string jsonPath in jsonFiles)
        {
            LoadObjectFromFile(jsonPath);
        }
        
        Debug.Log("Loaded " + spawnedObjects.Count + " objects.");
    }
    
    void LoadObjectFromFile(string jsonPath)
    {
        try
        {
            string json = File.ReadAllText(jsonPath);
            WorldObjectData data = JsonUtility.FromJson<WorldObjectData>(json);
            
            if (data == null)
            {
                Debug.LogWarning("Failed to parse JSON: " + jsonPath);
                return;
            }
            
            // Look for corresponding PNG
            string pngPath = jsonPath.Replace(".json", ".png");
            if (!File.Exists(pngPath))
            {
                // Try with different case
                pngPath = jsonPath.Replace(".JSON", ".png");
                if (!File.Exists(pngPath))
                {
                    Debug.LogWarning("PNG not found for: " + jsonPath);
                    return;
                }
            }
            
            SpawnObject(data, pngPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading object from " + jsonPath + ": " + e.Message);
        }
    }
    
    void SpawnObject(WorldObjectData data, string pngPath)
    {
        // Load sprite
        byte[] imageData = File.ReadAllBytes(pngPath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
        
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            32f
        );
        
        // Determine position
        Vector3 position;
        if (data.spawnX != 0 || data.spawnY != 0)
        {
            position = new Vector3(data.spawnX, data.spawnY, 0);
        }
        else
        {
            position = GetRandomPosition();
            data.spawnX = position.x;
            data.spawnY = position.y;
        }
        
        // Create object
        GameObject obj = new GameObject(data.name);
        obj.transform.SetParent(worldContainer);
        obj.transform.position = position;
        
        // Add sprite renderer
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;
        
        // Add collider for interaction
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        
        // Add WorldObject component
        WorldObject worldObj = obj.AddComponent<WorldObject>();
        worldObj.Initialize(data, sprite);
        
        spawnedObjects.Add(worldObj);
        
        Debug.Log("Spawned object: " + data.name);
    }
    
    Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            0
        );
    }
    
    void ClearObjects()
    {
        foreach (WorldObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj.gameObject);
            }
        }
        spawnedObjects.Clear();
    }
    
    public WorldObject GetRandomObject()
    {
        if (spawnedObjects.Count == 0) return null;
        return spawnedObjects[Random.Range(0, spawnedObjects.Count)];
    }
    
    public int GetObjectCount()
    {
        return spawnedObjects.Count;
    }
    
    public List<WorldObject> GetAllObjects()
    {
        return spawnedObjects;
    }
    
    void OnDestroy()
    {
        if (fileWatcher != null)
        {
            fileWatcher.Dispose();
        }
    }
}
