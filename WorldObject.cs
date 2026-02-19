using UnityEngine;

public class WorldObject : MonoBehaviour
{
    public WorldObjectData data;
    public Sprite objectSprite;
    
    private SpriteRenderer sr;
    private bool isPlayerNearby = false;
    
    public void Initialize(WorldObjectData data, Sprite sprite)
    {
        this.data = data;
        this.objectSprite = sprite;
        
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
        sr.sprite = sprite;
    }
    
    void OnMouseDown()
    {
        // Player clicked on object
        if (CharacterAI.Instance != null)
        {
            CharacterAI.Instance.InteractWithObject(data, transform.position);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            
            // Show proximity reaction
            if (!string.IsNullOrEmpty(data.interactions.proximity))
            {
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ShowSpeechBubble(
                        data.interactions.proximity,
                        transform.position + Vector3.up * 0.5f
                    );
                }
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
    
    void OnMouseEnter()
    {
        // Highlight effect
        if (sr != null)
        {
            sr.color = new Color(1.2f, 1.2f, 1.2f);
        }
    }
    
    void OnMouseExit()
    {
        // Remove highlight
        if (sr != null)
        {
            sr.color = Color.white;
        }
    }
}
