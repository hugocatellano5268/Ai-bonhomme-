using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    private static DialogueManager _instance;
    public static DialogueManager Instance { get { return _instance; } }
    
    [Header("UI References")]
    public GameObject speechBubblePrefab;
    public Transform canvasTransform;
    public Font pixelFont;
    
    [Header("Settings")]
    public float displayDuration = 2.5f;
    public float fadeDuration = 0.3f;
    public float bubbleOffset = 1.2f;
    
    private Queue<SpeechBubble> bubblePool = new Queue<SpeechBubble>();
    private List<SpeechBubble> activeBubbles = new List<SpeechBubble>();
    
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
        // Create canvas if not assigned
        if (canvasTransform == null)
        {
            CreateCanvas();
        }
        
        // Initialize bubble pool
        InitializeBubblePool();
    }
    
    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("DialogueCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        canvasTransform = canvasObj.transform;
        DontDestroyOnLoad(canvasObj);
    }
    
    void InitializeBubblePool()
    {
        // Create prefab if not assigned
        if (speechBubblePrefab == null)
        {
            speechBubblePrefab = CreateBubblePrefab();
        }
        
        // Pre-instantiate some bubbles
        for (int i = 0; i < 3; i++)
        {
            CreatePooledBubble();
        }
    }
    
    GameObject CreateBubblePrefab()
    {
        GameObject prefab = new GameObject("SpeechBubblePrefab");
        
        // Add RectTransform
        RectTransform rt = prefab.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 80);
        
        // Add Image (bubble background)
        Image bg = prefab.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.9f);
        
        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(prefab.transform);
        
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 10);
        textRt.offsetMax = new Vector2(-10, -10);
        
        Text text = textObj.AddComponent<Text>();
        text.font = pixelFont != null ? pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = 28;
        
        prefab.SetActive(false);
        return prefab;
    }
    
    void CreatePooledBubble()
    {
        GameObject bubbleObj = Instantiate(speechBubblePrefab, canvasTransform);
        SpeechBubble bubble = bubbleObj.AddComponent<SpeechBubble>();
        bubble.Initialize(this);
        bubbleObj.SetActive(false);
        bubblePool.Enqueue(bubble);
    }
    
    public void ShowSpeechBubble(string text, Vector3 worldPosition)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        SpeechBubble bubble = GetBubbleFromPool();
        if (bubble != null)
        {
            bubble.Show(text, worldPosition, displayDuration);
            activeBubbles.Add(bubble);
        }
    }
    
    SpeechBubble GetBubbleFromPool()
    {
        if (bubblePool.Count == 0)
        {
            CreatePooledBubble();
        }
        
        return bubblePool.Dequeue();
    }
    
    public void ReturnBubbleToPool(SpeechBubble bubble)
    {
        if (activeBubbles.Contains(bubble))
        {
            activeBubbles.Remove(bubble);
        }
        bubblePool.Enqueue(bubble);
    }
    
    void Update()
    {
        // Update positions of active bubbles
        foreach (SpeechBubble bubble in activeBubbles)
        {
            if (bubble != null && bubble.gameObject.activeSelf)
            {
                bubble.UpdatePosition();
            }
        }
    }
}

public class SpeechBubble : MonoBehaviour
{
    private DialogueManager manager;
    private Text textComponent;
    private Image backgroundImage;
    private RectTransform rectTransform;
    private Transform targetTransform;
    private Vector3 worldOffset;
    private Coroutine hideCoroutine;
    
    public void Initialize(DialogueManager manager)
    {
        this.manager = manager;
        rectTransform = GetComponent<RectTransform>();
        backgroundImage = GetComponent<Image>();
        textComponent = GetComponentInChildren<Text>();
    }
    
    public void Show(string text, Vector3 worldPosition, float duration)
    {
        // Set text
        if (textComponent != null)
        {
            textComponent.text = text;
        }
        
        // Store world position
        worldOffset = new Vector3(0, manager.bubbleOffset, 0);
        
        // Activate
        gameObject.SetActive(true);
        
        // Set initial position
        UpdatePosition(worldPosition + worldOffset);
        
        // Start fade in
        StartCoroutine(FadeIn());
        
        // Schedule hide
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }
    
    void UpdatePosition(Vector3 worldPos)
    {
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            rectTransform.position = screenPos;
        }
    }
    
    public void UpdatePosition()
    {
        // This can be called if following a moving target
    }
    
    IEnumerator FadeIn()
    {
        float elapsed = 0;
        float fadeDuration = manager.fadeDuration;
        
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = 0;
            backgroundImage.color = bgColor;
        }
        
        if (textComponent != null)
        {
            Color textColor = textComponent.color;
            textColor.a = 0;
            textComponent.color = textColor;
        }
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / fadeDuration;
            
            if (backgroundImage != null)
            {
                Color bgColor = backgroundImage.color;
                bgColor.a = Mathf.Lerp(0, 0.9f, alpha);
                backgroundImage.color = bgColor;
            }
            
            if (textComponent != null)
            {
                Color textColor = textComponent.color;
                textColor.a = Mathf.Lerp(0, 1, alpha);
                textComponent.color = textColor;
            }
            
            yield return null;
        }
    }
    
    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Fade out
        float elapsed = 0;
        float fadeDuration = manager.fadeDuration;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1 - (elapsed / fadeDuration);
            
            if (backgroundImage != null)
            {
                Color bgColor = backgroundImage.color;
                bgColor.a = Mathf.Lerp(0, 0.9f, alpha);
                backgroundImage.color = bgColor;
            }
            
            if (textComponent != null)
            {
                Color textColor = textComponent.color;
                textColor.a = alpha;
                textComponent.color = textColor;
            }
            
            yield return null;
        }
        
        Hide();
    }
    
    void Hide()
    {
        gameObject.SetActive(false);
        manager.ReturnBubbleToPool(this);
    }
}
