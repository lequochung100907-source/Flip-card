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
    public TMP_Text timerText;
    public TMP_Text livesText;
    public GameObject victoryUI;
    public GameObject gameOverUI;
    public float timeRemaining = 300f;
    public int lives = 5;

    [HideInInspector] public LevelConfig levelConfig;
    [HideInInspector] public CardController cardPrefab;

    private CardController firstCard;
    private CardController secondCard;
    private bool isChecking = false;
    private bool isGameEnded = false;
    private int score = 0;
    private int matchedPairs = 0;
    private int totalPairs = 0;
    private int lastSecond = -1;

    private WaitForSeconds waitCheck = new WaitForSeconds(0.75f);
    private WaitForSeconds waitUnflip = new WaitForSeconds(0.4f);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
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
        if (scoreText == null)
        {
            GameObject scoreObj = GameObject.Find("PairCountText");
            if (scoreObj != null) scoreText = scoreObj.GetComponent<TMP_Text>();
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
        if (victoryUI == null)
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                Transform resTf = canvasObj.transform.Find("GameResultObject");
                if (resTf != null) victoryUI = resTf.gameObject;
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
        timeRemaining = 300f;
        lives = 5;
        isGameEnded = false;
        lastSecond = -1;
        UpdateScoreUI();
        UpdateTimerUI();
        UpdateLivesUI();

        if (victoryUI != null)
        {
            victoryUI.SetActive(false);
        }

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isGameEnded)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isGameEnded = true;
            StopAllCoroutines();
            isChecking = false;
            UpdateTimerUI();

            if (gameOverUI != null)
            {
                gameOverUI.SetActive(true);
            }
            return;
        }

        UpdateTimerUI();
    }

    public bool CanFlip()
    {
        return !isGameEnded && !isChecking && secondCard == null;
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

            if (matchedPairs >= totalPairs)
            {
                isGameEnded = true;
                if (victoryUI != null)
                {
                    victoryUI.SetActive(true);
                }
            }
        }
        else
        {
            if (firstCard != null) firstCard.Unflip();
            if (secondCard != null) secondCard.Unflip();

            lives -= 1;
            if (lives < 0) lives = 0;
            UpdateLivesUI();

            yield return waitUnflip;

            if (lives <= 0)
            {
                isGameEnded = true;
                if (gameOverUI != null)
                {
                    gameOverUI.SetActive(true);
                }
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
            scoreText.text = "Score: " + score;
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int currentSecond = Mathf.FloorToInt(timeRemaining);
            if (currentSecond != lastSecond)
            {
                lastSecond = currentSecond;
                int minutes = currentSecond / 60;
                int seconds = currentSecond % 60;
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = lives.ToString();
        }
    }
}
