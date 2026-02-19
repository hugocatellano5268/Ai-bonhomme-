using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [Header("Touch Settings")]
    public float touchDragThreshold = 0.5f;
    
    private Vector2 touchStartPos;
    private bool isDragging = false;
    private CharacterAI characterAI;
    
    void Start()
    {
        characterAI = GetComponent<CharacterAI>();
    }
    
    void Update()
    {
        HandleTouchInput();
    }
    
    void HandleTouchInput()
    {
        #if UNITY_ANDROID || UNITY_IOS
        // Mobile touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(touch.position);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touchPos;
                    isDragging = false;
                    
                    // Check if touching character
                    RaycastHit2D hit = Physics2D.Raycast(touchPos, Vector2.zero);
                    if (hit.collider != null && hit.collider.gameObject == gameObject)
                    {
                        characterAI.OnTouched();
                    }
                    break;
                    
                case TouchPhase.Moved:
                    if (Vector2.Distance(touchStartPos, touchPos) > touchDragThreshold)
                    {
                        isDragging = true;
                    }
                    break;
                    
                case TouchPhase.Ended:
                    if (!isDragging)
                    {
                        // Tap on empty space - character moves there
                        RaycastHit2D hitObj = Physics2D.Raycast(touchPos, Vector2.zero);
                        if (hitObj.collider == null)
                        {
                            // Move to tapped position
                            characterAI.SetTargetPosition(touchPos);
                        }
                    }
                    break;
            }
        }
        #else
        // Mouse input for testing
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Check if clicking character
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                characterAI.OnTouched();
            }
            else if (hit.collider == null)
            {
                // Move to clicked position
                characterAI.SetTargetPosition(mousePos);
            }
        }
        #endif
    }
}
