using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CharacterAI : MonoBehaviour
{
    private static CharacterAI _instance;
    public static CharacterAI Instance { get { return _instance; } }
    
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float idleTime = 2f;
    public float moveRadius = 3f;
    
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    
    private CharacterData data;
    private Vector3 targetPosition;
    private Vector3 homePosition;
    private bool isMoving = false;
    private bool isSleeping = false;
    
    // AI States
    private enum AIState { Idle, Moving, Interacting, Sleeping, Playing }
    private AIState currentState = AIState.Idle;
    
    // Learning vocabulary (French)
    private List<string> basicWords = new List<string> { "Bonjour", "Oh", "Wow", "Miam", "Zzz" };
    private List<string> mediumWords = new List<string> { "J'aime", "Beau", "Amusant", "Dormir", "Manger" };
    private List<string> advancedWords = new List<string> { "C'est intéressant", "Je suis content", "Encore", "Merci", "Super" };
    
    // Sentence templates for evolution levels
    private string[] level1Templates = { "{0}!", "{0}...", "{0}?" };
    private string[] level2Templates = { "{0} {1}!", "{0}... {1}?", "Oh, {0} {1}!" };
    private string[] level3Templates = { "{0} {1} {2}!", "Je vois {0} {1}.", "C'est {0} et {1}!" };
    
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
        homePosition = transform.position;
        
        // Load saved data
        if (SaveManager.Instance != null)
        {
            data = SaveManager.Instance.LoadCharacter();
        }
        else
        {
            data = new CharacterData();
        }
        
        // Restore position
        if (data.posX != 0 || data.posY != 0)
        {
            transform.position = new Vector3(data.posX, data.posY, 0);
        }
        
        // Start AI behavior
        StartCoroutine(AIBehaviorLoop());
        StartCoroutine(EvolutionLoop());
        StartCoroutine(NeedsDecayLoop());
    }
    
    void Update()
    {
        if (isMoving && !isSleeping)
        {
            MoveToTarget();
        }
        
        // Save position
        data.posX = transform.position.x;
        data.posY = transform.position.y;
    }
    
    IEnumerator AIBehaviorLoop()
    {
        while (true)
        {
            if (!isSleeping && currentState != AIState.Interacting)
            {
                float decision = Random.value;
                
                if (decision < 0.4f)
                {
                    // Move to random position
                    SetRandomTarget();
                }
                else if (decision < 0.6f && data.energy < 30f)
                {
                    // Sleep if tired
                    GoToSleep();
                }
                else if (decision < 0.8f)
                {
                    // Look around and comment
                    LookAround();
                }
                else
                {
                    // Just idle
                    currentState = AIState.Idle;
                    if (animator != null) animator.SetBool("IsMoving", false);
                }
            }
            
            yield return new WaitForSeconds(idleTime + Random.Range(-1f, 2f));
        }
    }
    
    IEnumerator EvolutionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f); // Check every minute
            
            // Evolution based on interactions
            if (data.totalInteractions > data.evolutionLevel * 10)
            {
                Evolve();
            }
            
            // Learn new words
            LearnNewWords();
        }
    }
    
    IEnumerator NeedsDecayLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);
            
            // Decay needs
            data.energy = Mathf.Max(0, data.energy - 2f);
            data.hunger = Mathf.Min(100, data.hunger + 1f);
            
            // Happiness based on needs
            if (data.hunger > 70f) data.happiness -= 5f;
            if (data.energy < 20f) data.happiness -= 2f;
            
            data.happiness = Mathf.Clamp(data.happiness, 0, 100);
        }
    }
    
    void SetRandomTarget()
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        float distance = Random.Range(0.5f, moveRadius);
        
        targetPosition = homePosition + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0
        );
        
        isMoving = true;
        currentState = AIState.Moving;
        
        if (animator != null) animator.SetBool("IsMoving", true);
        
        // Face direction
        if (targetPosition.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }
    
    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
        
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isMoving = false;
            currentState = AIState.Idle;
            if (animator != null) animator.SetBool("IsMoving", false);
        }
    }
    
    public void InteractWithObject(WorldObjectData objData, Vector3 objectPosition)
    {
        if (isSleeping) return;
        
        currentState = AIState.Interacting;
        
        // Move to object
        targetPosition = objectPosition + new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(-0.5f, 0.5f),
            0
        );
        isMoving = true;
        
        StartCoroutine(InteractCoroutine(objData));
    }
    
    IEnumerator InteractCoroutine(WorldObjectData objData)
    {
        // Wait to arrive
        while (isMoving)
        {
            yield return null;
        }
        
        // Perform interaction
        string interaction = GetInteractionType(objData);
        string reaction = GetReaction(objData, interaction);
        
        // Show dialogue
        if (DialogueManager.Instance != null)
        {
            string message = GenerateMessage(reaction, objData.name);
            DialogueManager.Instance.ShowSpeechBubble(message, transform.position);
        }
        
        // Update stats
        data.totalInteractions++;
        data.happiness = Mathf.Min(100, data.happiness + 5f);
        
        // Remember interaction
        data.memories.Add(new MemoryEntry(interaction, objData.name, reaction));
        if (data.memories.Count > 50) data.memories.RemoveAt(0);
        
        // Track object interactions
        if (data.objectInteractions.ContainsKey(objData.name))
        {
            data.objectInteractions[objData.name]++;
        }
        else
        {
            data.objectInteractions[objData.name] = 1;
        }
        
        yield return new WaitForSeconds(2f);
        
        currentState = AIState.Idle;
    }
    
    string GetInteractionType(WorldObjectData objData)
    {
        if (objData.type == "food") return "eat";
        if (objData.type == "toy") return "play";
        return "touch";
    }
    
    string GetReaction(WorldObjectData objData, string interaction)
    {
        switch (interaction)
        {
            case "eat":
                data.hunger = Mathf.Max(0, data.hunger - 30f);
                data.energy = Mathf.Min(100, data.energy + 10f);
                return objData.interactions.eat ?? "Miam!";
            case "play":
                data.energy = Mathf.Max(0, data.energy - 10f);
                data.happiness = Mathf.Min(100, data.happiness + 10f);
                return objData.interactions.play ?? "Amusant!";
            default:
                return objData.interactions.touch ?? "Oh!";
        }
    }
    
    void LookAround()
    {
        if (ObjectManager.Instance != null && ObjectManager.Instance.GetObjectCount() > 0)
        {
            var obj = ObjectManager.Instance.GetRandomObject();
            if (obj != null)
            {
                string message = GenerateMessage("Je vois", obj.data.name);
                DialogueManager.Instance.ShowSpeechBubble(message, transform.position);
            }
        }
    }
    
    void GoToSleep()
    {
        isSleeping = true;
        currentState = AIState.Sleeping;
        if (animator != null) animator.SetBool("IsSleeping", true);
        
        StartCoroutine(SleepCoroutine());
    }
    
    IEnumerator SleepCoroutine()
    {
        DialogueManager.Instance.ShowSpeechBubble("Zzz...", transform.position);
        
        while (data.energy < 80f)
        {
            data.energy = Mathf.Min(100, data.energy + 5f);
            yield return new WaitForSeconds(1f);
        }
        
        isSleeping = false;
        if (animator != null) animator.SetBool("IsSleeping", false);
        currentState = AIState.Idle;
        
        DialogueManager.Instance.ShowSpeechBubble("Bonjour!", transform.position);
    }
    
    void Evolve()
    {
        data.evolutionLevel++;
        
        // Visual evolution
        if (animator != null)
        {
            animator.SetTrigger("Evolve");
        }
        
        // Announce evolution
        string[] evolveMessages = { "J'ai grandi!", "Je suis plus fort!", "Woooow!" };
        string message = evolveMessages[Random.Range(0, evolveMessages.Length)];
        DialogueManager.Instance.ShowSpeechBubble(message, transform.position);
        
        Debug.Log("Character evolved to level " + data.evolutionLevel);
    }
    
    void LearnNewWords()
    {
        List<string> wordPool = new List<string>();
        
        if (data.evolutionLevel == 1)
            wordPool = basicWords;
        else if (data.evolutionLevel == 2)
            wordPool = mediumWords;
        else
            wordPool = advancedWords;
        
        foreach (string word in wordPool)
        {
            if (!data.learnedWords.Contains(word) && Random.value < 0.3f)
            {
                data.learnedWords.Add(word);
                Debug.Log("Learned word: " + word);
            }
        }
    }
    
    string GenerateMessage(string context, string objectName = "")
    {
        List<string> availableWords = new List<string>(data.learnedWords);
        
        // Add default words if vocabulary is small
        if (availableWords.Count < 3)
        {
            availableWords.AddRange(basicWords);
        }
        
        string[] templates;
        if (data.evolutionLevel == 1)
            templates = level1Templates;
        else if (data.evolutionLevel == 2)
            templates = level2Templates;
        else
            templates = level3Templates;
        
        string template = templates[Random.Range(0, templates.Length)];
        
        // Fill template
        string word1 = availableWords[Random.Range(0, availableWords.Count)];
        string word2 = availableWords[Random.Range(0, availableWords.Count)];
        string word3 = !string.IsNullOrEmpty(objectName) ? objectName : availableWords[Random.Range(0, availableWords.Count)];
        
        return string.Format(template, word1, word2, word3);
    }
    
    public void OnTouched()
    {
        if (isSleeping)
        {
            isSleeping = false;
            if (animator != null) animator.SetBool("IsSleeping", false);
        }
        
        data.happiness = Mathf.Min(100, data.happiness + 10f);
        data.totalInteractions++;
        
        string[] petResponses = { "Coucou!", "Content!", "Hihi!" };
        string message = petResponses[Random.Range(0, petResponses.Length)];
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowSpeechBubble(message, transform.position);
        }
        
        // Small jump animation
        StartCoroutine(SmallJump());
    }
    
    IEnumerator SmallJump()
    {
        Vector3 originalPos = transform.position;
        float jumpHeight = 0.3f;
        float duration = 0.3f;
        
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float y = Mathf.Sin((elapsed / duration) * Mathf.PI) * jumpHeight;
            transform.position = originalPos + new Vector3(0, y, 0);
            yield return null;
        }
        
        transform.position = originalPos;
    }
    
    public CharacterData GetCharacterData()
    {
        return data;
    }
    
    public int GetEvolutionLevel()
    {
        return data.evolutionLevel;
    }
    
    public float GetHappiness()
    {
        return data.happiness;
    }
    
    public float GetEnergy()
    {
        return data.energy;
    }
    
    public float GetHunger()
    {
        return data.hunger;
    }
    
    public void SetTargetPosition(Vector2 position)
    {
        if (isSleeping) return;
        
        targetPosition = new Vector3(position.x, position.y, 0);
        isMoving = true;
        currentState = AIState.Moving;
        
        if (animator != null) animator.SetBool("IsMoving", true);
        
        // Face direction
        if (targetPosition.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }
}
