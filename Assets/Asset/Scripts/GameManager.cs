using System.Collections;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Transform cardContainer;
    public GameObject cardPrefabs;
    public Sprite[] cardIcons;
    public TMP_Text scoreText;
    public GameObject victoryUI;

    [HideInInspector] public LevelConfig levelConfig;
    [HideInInspector] public CardController cardPrefab;

    private CardController firstCard;
    private CardController secondCard;
    private bool isChecking = false;
    private int score = 0;
    private int matchedPairs = 0;
    private int totalPairs = 0;

    private WaitForSeconds waitCheck = new WaitForSeconds(0.75f);
    private WaitForSeconds waitUnflip = new WaitForSeconds(0.4f);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (cardPrefabs == null && cardPrefab != null)
        {
            cardPrefabs = cardPrefab.gameObject;
        }

        if ((cardIcons == null || cardIcons.Length == 0) && levelConfig != null && levelConfig.pairsInLevel != null)
        {
            cardIcons = new Sprite[levelConfig.pairsInLevel.Count];
            for (int i = 0; i < cardIcons.Length; i++)
            {
                if (levelConfig.pairsInLevel[i] != null)
                {
                    cardIcons[i] = levelConfig.pairsInLevel[i].cardSprite;
                }
            }
        }

        if (cardContainer != null)
        {
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardContainer.GetChild(i).gameObject);
            }
        }

        if (cardIcons != null && cardPrefabs != null && cardContainer != null)
        {
            totalPairs = cardIcons.Length;
            for (int i = 0; i < cardIcons.Length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    GameObject cardObj = Instantiate(cardPrefabs, cardContainer);
                    CardController card = cardObj.GetComponent<CardController>();
                    if (card != null)
                    {
                        card.InitCard(i, cardIcons[i]);
                    }
                }
            }

            int count = cardContainer.childCount;
            for (int i = count - 1; i > 0; i--)
            {
                int rand = Random.Range(0, i + 1);
                cardContainer.GetChild(rand).SetSiblingIndex(i);
            }
        }

        score = 0;
        matchedPairs = 0;
        UpdateScoreUI();

        if (victoryUI != null)
        {
            victoryUI.SetActive(false);
        }
    }

    public bool CanFlip()
    {
        return !isChecking && secondCard == null;
    }

    public void OnCardFlipped(CardController card)
    {
        if (firstCard == null)
        {
            firstCard = card;
        }
        else if (secondCard == null && card != firstCard)
        {
            secondCard = card;
            StartCoroutine(CheckMatchRoutine());
        }
    }

    private IEnumerator CheckMatchRoutine()
    {
        isChecking = true;
        yield return waitCheck;

        if (firstCard != null && secondCard != null && firstCard.id == secondCard.id)
        {
            firstCard.HideCard();
            secondCard.HideCard();

            score += 1;
            matchedPairs += 1;
            UpdateScoreUI();

            if (matchedPairs >= totalPairs && victoryUI != null)
            {
                victoryUI.SetActive(true);
            }
        }
        else
        {
            if (firstCard != null) firstCard.Unflip();
            if (secondCard != null) secondCard.Unflip();
            yield return waitUnflip;
        }

        firstCard = null;
        secondCard = null;
        isChecking = false;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}
