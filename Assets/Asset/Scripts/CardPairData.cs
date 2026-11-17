using UnityEngine;

[CreateAssetMenu(fileName = "NewCardPair", menuName = "FlipCard/Card Pair Data")]
public class CardPairData : ScriptableObject
{
    [Header("Thông tin cặp thẻ")]
    public string pairId;
    public string clubName;
    public Sprite cardSprite;

    [Header("Thông tin hiển thị ở bảng Kiến Thức")]
    [TextArea(3, 8)]
    public string knowledgeDescription;
}