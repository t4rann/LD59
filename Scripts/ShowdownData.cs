// ShowdownData.cs
using System.Collections.Generic;

// Классы данных для вскрытия
[System.Serializable]
public class ShowdownInfo
{
    public bool PlayerFolded;
    public bool PlayerRaised;
    public string PlayerHandDesc;
    public int PlayerHandValue;
    
    public List<NPCShowdownInfo> ActiveNPCInfos = new List<NPCShowdownInfo>();
    public List<string> FoldedNPCNames = new List<string>();
    
    public string WinnerName;
    public string WinnerHandDesc;
    public int PotAmount;
}

[System.Serializable]
public class NPCShowdownInfo
{
    public string Name;
    public bool Raised;
    public string HandDesc;
    public int HandValue;
}