using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Data/Game Data")]
public class GameDataSO : ScriptableObject
{
    public int lifeCountLeft = 5;
    public int highScore;
    public int currentStage;

}