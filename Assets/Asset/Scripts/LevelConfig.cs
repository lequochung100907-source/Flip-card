using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Scriptable Objects/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    public int columns = 6;
    public float timeLimitSeconds = 240f;
    public int maxLives = 5;
    public List<CardPairData> pairsInLevel = new List<CardPairData>();
}
