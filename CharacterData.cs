using System;
using System.Collections.Generic;

[Serializable]
public class CharacterData
{
    public string characterName = "Pixel";
    public int evolutionLevel = 1;
    public float happiness = 50f;
    public float energy = 100f;
    public float hunger = 0f;
    public int totalInteractions = 0;
    public int daysAlive = 0;
    public string lastSaveTime = "";
    
    // Learning and memory
    public List<string> learnedWords = new List<string>();
    public List<MemoryEntry> memories = new List<MemoryEntry>();
    public Dictionary<string, int> objectInteractions = new Dictionary<string, int>();
    
    // Personality traits (evolve over time)
    public float curiosity = 0.5f;
    public float playfulness = 0.5f;
    public float sociability = 0.5f;
    
    // Position
    public float posX = 0f;
    public float posY = 0f;
}

[Serializable]
public class MemoryEntry
{
    public string timestamp;
    public string action;
    public string objectName;
    public string reaction;
    
    public MemoryEntry(string action, string objectName, string reaction)
    {
        this.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        this.action = action;
        this.objectName = objectName;
        this.reaction = reaction;
    }
}
