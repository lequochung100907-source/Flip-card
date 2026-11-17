using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Transform cardContainer;
    public GameObject cardPrefabs;
    public Sprite[] cardIcons;
    public TMP_Text scoreText;
    public GameObject victoryUI;

    public LevelConfig levelConfig;
    public TMP_Text timerText;
    public TMP_Text livesText;
    public Transform knowledgeContent;
    public GameObject knowledgeItemPrefab;

    [HideInInspector] public CardController cardPrefab;

    private CardController firstCard;
    private CardController secondCard;
    private bool isChecking = false;
    private bool isGameOver = false;
    private int score = 0;
    private int matchedPairs = 0;
    private int totalPairs = 0;
    private int currentLives = 5;
    private float remainingTime = 240f;
    private int lastSecond = -1;

    private WaitForSeconds waitCheck = new WaitForSeconds(0.75f);
    private WaitForSeconds waitUnflip = new WaitForSeconds(0.4f);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        FindMissingReferences();
        RestartGame();
    }

    private void FindMissingReferences()
    {
        if (cardContainer == null)
        {
            GameObject gridObj = GameObject.Find("CardGrid");
            if (gridObj != null) cardContainer = gridObj.transform;
        }
        if (cardPrefabs == null && cardPrefab != null)
        {
            cardPrefabs = cardPrefab.gameObject;
        }
        if (scoreText == null)
        {
            GameObject scoreObj = GameObject.Find("PairCountText");
            if (scoreObj != null) scoreText = scoreObj.GetComponent<TMP_Text>();
        }
        if (victoryUI == null)
        {
            victoryUI = GameObject.Find("GameResultObject");
        }
        if (timerText == null)
        {
            GameObject timerObj = GameObject.Find("TimerText");
            if (timerObj != null) timerText = timerObj.GetComponent<TMP_Text>();
        }
        if (livesText == null)
        {
            GameObject livesObj = GameObject.Find("HeartText");
            if (livesObj != null) livesText = livesObj.GetComponent<TMP_Text>();
        }
        if (knowledgeContent == null)
        {
            GameObject contentObj = GameObject.Find("Content");
            if (contentObj != null) knowledgeContent = contentObj.transform;
        }
    }

    public void RestartGame()
    {
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

        ClearChildren(cardContainer);
        ClearChildren(knowledgeContent);

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

        firstCard = null;
        secondCard = null;
        isChecking = false;
        isGameOver = false;
        score = 0;
        matchedPairs = 0;
        lastSecond = -1;

        if (levelConfig != null)
        {
            currentLives = levelConfig.maxLives;
            remainingTime = levelConfig.timeLimitSeconds;
        }
        else
        {
            currentLives = 5;
            remainingTime = 240f;
        }

        UpdateScoreUI();
        UpdateLivesUI();
        UpdateTimerUI();

        if (victoryUI != null)
        {
            victoryUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isGameOver || remainingTime <= 0f)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isGameOver = true;
        }

        UpdateTimerUI();
    }

    public bool CanFlip()
    {
        return !isChecking && secondCard == null && !isGameOver;
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
            AddKnowledgeEntry(firstCard.id);

            if (matchedPairs >= totalPairs && victoryUI != null)
            {
                isGameOver = true;
                victoryUI.SetActive(true);
            }
        }
        else
        {
            if (firstCard != null) firstCard.Unflip();
            if (secondCard != null) secondCard.Unflip();

            currentLives -= 1;
            if (currentLives < 0) currentLives = 0;
            UpdateLivesUI();

            yield return waitUnflip;

            if (currentLives <= 0)
            {
                isGameOver = true;
            }
        }

        firstCard = null;
        secondCard = null;
        isChecking = false;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            if (scoreText.gameObject.name.Contains("Pair"))
            {
                scoreText.text = "Số cặp đã hoàn thành: " + matchedPairs + "/" + totalPairs;
            }
            else
            {
                scoreText.text = "Score: " + score;
            }
        }
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = currentLives.ToString();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
        {
            return;
        }

        int currentSecond = Mathf.CeilToInt(remainingTime);
        if (currentSecond == lastSecond)
        {
            return;
        }

        lastSecond = currentSecond;
        int minutes = currentSecond / 60;
        int seconds = currentSecond % 60;
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void AddKnowledgeEntry(int index)
    {
        if (knowledgeContent == null || levelConfig == null || levelConfig.pairsInLevel == null)
        {
            return;
        }
        if (index < 0 || index >= levelConfig.pairsInLevel.Count)
        {
            return;
        }

        CardPairData data = levelConfig.pairsInLevel[index];
        if (data == null)
        {
            return;
        }

        if (knowledgeItemPrefab != null)
        {
            GameObject item = Instantiate(knowledgeItemPrefab, knowledgeContent);
            Transform logoTf = item.transform.Find("LogoImage");
            if (logoTf != null)
            {
                Image logo = logoTf.GetComponent<Image>();
                if (logo != null) logo.sprite = data.cardSprite;
            }

            Transform descTf = item.transform.Find("DescriptionText");
            if (descTf != null)
            {
                TMP_Text desc = descTf.GetComponent<TMP_Text>();
                if (desc != null) desc.text = "<b>" + data.clubName + "</b>\n" + data.knowledgeDescription;
            }
        }
        else
        {
            GameObject entryObj = new GameObject("Entry");
            entryObj.transform.SetParent(knowledgeContent, false);
            TMP_Text entry = entryObj.AddComponent<TextMeshProUGUI>();
            entry.fontSize = 20;
            entry.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            entry.text = "<b>" + data.clubName + "</b>: " + data.knowledgeDescription;
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
