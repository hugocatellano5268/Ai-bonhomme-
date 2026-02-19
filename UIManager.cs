using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("Stats UI")]
    public GameObject statsPanel;
    public Image happinessBar;
    public Image energyBar;
    public Image hungerBar;
    public Text evolutionText;
    
    [Header("Settings")]
    public bool showStatsOnStart = false;
    public float updateInterval = 1f;
    
    private float lastUpdateTime;
    private CharacterAI characterAI;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        characterAI = CharacterAI.Instance;
        
        if (statsPanel != null)
        {
            statsPanel.SetActive(showStatsOnStart);
        }
        
        lastUpdateTime = Time.time;
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateStats();
            lastUpdateTime = Time.time;
        }
        
        // Toggle stats panel with triple tap (top right corner)
        #if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 3)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.position.x > Screen.width * 0.8f && touch.position.y > Screen.height * 0.8f)
            {
                ToggleStatsPanel();
            }
        }
        #else
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleStatsPanel();
        }
        #endif
    }
    
    void UpdateStats()
    {
        if (characterAI == null) return;
        
        if (happinessBar != null)
        {
            happinessBar.fillAmount = characterAI.GetHappiness() / 100f;
        }
        
        if (energyBar != null)
        {
            energyBar.fillAmount = characterAI.GetEnergy() / 100f;
        }
        
        if (hungerBar != null)
        {
            hungerBar.fillAmount = characterAI.GetHunger() / 100f;
        }
        
        if (evolutionText != null)
        {
            evolutionText.text = "Niv. " + characterAI.GetEvolutionLevel();
        }
    }
    
    public void ToggleStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(!statsPanel.activeSelf);
        }
    }
    
    public void ShowStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(true);
        }
    }
    
    public void HideStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }
}
