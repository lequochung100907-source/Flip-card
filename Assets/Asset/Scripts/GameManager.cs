using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Cấu hình chính")]
    public Transform cardContainer;
    public GameObject cardPrefabs;
    public Sprite[] cardIcons;
    public TMP_Text scoreText;
    public GameObject victoryUI;

    [Header("Cấu hình Inspector nâng cao")]
    [SerializeField] private CardController cardPrefab;
    [SerializeField] private GridLayoutGroup cardGrid;
    [SerializeField] private LevelConfig levelConfig;
    [SerializeField] private float mismatchDelay = 0.5f;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private GameObject knowledgePanel;
    [SerializeField] private TMP_Text clubNameText;
    [SerializeField] private TMP_Text knowledgeDescText;
    [SerializeField] private float timeLimit = 240f;
    [SerializeField] private int maxLives = 5;
    [SerializeField] private GameObject knowledgeItemPrefab;

    private CardController firstCard;
    private CardController secondCard;
    private bool isChecking = false;
    private int score = 0;
    private int matchedPairs = 0;
    private int totalPairs = 0;
    private int currentLives = 5;
    private float remainingTime = 240f;
    private bool isGameOver = false;

    private WaitForSeconds checkWait;
    private static readonly WaitForSeconds FlipBackWait = new WaitForSeconds(0.4f);

    private int lastDisplayedSecond = -1;
    private Transform cachedKnowledgeContent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureEventSystem();
        checkWait = new WaitForSeconds(mismatchDelay > 0f ? mismatchDelay : 0.5f);
    }

    private void Start()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        AutoFindReferences();

        if (cardContainer != null)
        {
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardContainer.GetChild(i).gameObject);
            }
        }

        if (cachedKnowledgeContent != null)
        {
            for (int i = cachedKnowledgeContent.childCount - 1; i >= 0; i--)
            {
                Destroy(cachedKnowledgeContent.GetChild(i).gameObject);
            }
        }

        if (cardIcons != null && cardPrefabs != null && cardContainer != null)
        {
            totalPairs = cardIcons.Length;
            for (int i = 0; i < cardIcons.Length; i++)
            {
                GameObject cardObj1 = Instantiate(cardPrefabs, cardContainer);
                GameObject cardObj2 = Instantiate(cardPrefabs, cardContainer);

                CardController c1 = cardObj1.GetComponent<CardController>();
                CardController c2 = cardObj2.GetComponent<CardController>();

                CardPairData pairData = GetPairData(i);

                if (c1 != null) c1.InitCard(i, cardIcons[i], pairData);
                if (c2 != null) c2.InitCard(i, cardIcons[i], pairData);
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
        score = 0;
        matchedPairs = 0;
        currentLives = maxLives;
        remainingTime = timeLimit;
        isGameOver = false;
        lastDisplayedSecond = -1;

        UpdateScoreUI();
        UpdateLivesUI();
        UpdateTimerUI(true);

        if (victoryUI != null)
        {
            victoryUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        if (timerText != null && remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                isGameOver = true;
            }
            UpdateTimerUI(false);
        }
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
        yield return checkWait;

        if (firstCard != null && secondCard != null && firstCard.id == secondCard.id)
        {
            firstCard.HideCard();
            secondCard.HideCard();

            score += 1;
            matchedPairs += 1;
            UpdateScoreUI();

            CardPairData pairData = firstCard.Data ?? GetPairData(firstCard.id);
            if (pairData != null)
            {
                ShowKnowledge(pairData);
            }

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

            currentLives--;
            if (currentLives < 0) currentLives = 0;
            UpdateLivesUI();

            yield return FlipBackWait;

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
                scoreText.text = $"Số cặp đã hoàn thành: {matchedPairs}/{totalPairs}";
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
            livesText.text = $"Lives: {currentLives}";
        }
    }

    private void UpdateTimerUI(bool forceUpdate)
    {
        if (timerText == null) return;

        int currentSec = Mathf.CeilToInt(remainingTime);
        if (!forceUpdate && currentSec == lastDisplayedSecond) return;
        lastDisplayedSecond = currentSec;

        int mins = currentSec / 60;
        int secs = currentSec % 60;
        timerText.text = $"{mins:00}:{secs:00}";
    }

    public CardPairData GetPairData(int index)
    {
        if (levelConfig != null && levelConfig.pairsInLevel != null && index >= 0 && index < levelConfig.pairsInLevel.Count)
        {
            return levelConfig.pairsInLevel[index];
        }
        return null;
    }

    private void ShowKnowledge(CardPairData data)
    {
        if (data == null) return;

        if (knowledgePanel != null) knowledgePanel.SetActive(true);
        if (clubNameText != null) clubNameText.text = data.clubName;
        if (knowledgeDescText != null) knowledgeDescText.text = data.knowledgeDescription;

        if (cachedKnowledgeContent != null)
        {
            if (knowledgeItemPrefab != null)
            {
                GameObject item = Instantiate(knowledgeItemPrefab, cachedKnowledgeContent);
                Image logo = item.transform.Find("LogoImage")?.GetComponent<Image>();
                if (logo != null && data.cardSprite != null) logo.sprite = data.cardSprite;

                TMP_Text desc = item.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
                if (desc != null)
                {
                    desc.text = $"<b>{data.clubName}</b>\n{data.knowledgeDescription}";
                }
            }
            else
            {
                GameObject lineObj = new GameObject("KnowledgeEntry_" + data.clubName);
                lineObj.transform.SetParent(cachedKnowledgeContent, false);
                TMP_Text entryText = lineObj.AddComponent<TextMeshProUGUI>();
                entryText.fontSize = 20;
                entryText.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                entryText.text = $"<b>{data.clubName}</b>: {data.knowledgeDescription}";
            }
        }
    }

    private void AutoFindReferences()
    {
        if (cardContainer == null)
        {
            if (cardGrid != null) cardContainer = cardGrid.transform;
            else
            {
                GameObject cgObj = GameObject.Find("CardGrid");
                if (cgObj != null) cardContainer = cgObj.transform;
            }
        }

        if (cardContainer != null)
        {
            GridLayoutGroup grid = cardContainer.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = cardContainer.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(128f, 161f);
                grid.spacing = new Vector2(15f, 15f);
                grid.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        if (cardPrefabs == null)
        {
            if (cardPrefab != null)
            {
                cardPrefabs = cardPrefab.gameObject;
            }
            else
            {
                CardController existing = FindFirstObjectByType<CardController>(FindObjectsInactive.Include);
                if (existing != null && existing.gameObject.scene.name == null)
                {
                    cardPrefabs = existing.gameObject;
                }
                else
                {
                    CardController[] allCards = Resources.FindObjectsOfTypeAll<CardController>();
                    foreach (CardController c in allCards)
                    {
                        if (c != null && c.gameObject.scene.name == null)
                        {
                            cardPrefabs = c.gameObject;
                            break;
                        }
                    }
                }
            }
        }

        if (levelConfig == null)
        {
            LevelConfig[] configs = Resources.FindObjectsOfTypeAll<LevelConfig>();
            if (configs != null && configs.Length > 0)
            {
                levelConfig = configs[0];
            }
        }

        if ((cardIcons == null || cardIcons.Length == 0) && levelConfig != null && levelConfig.pairsInLevel != null)
        {
            cardIcons = new Sprite[levelConfig.pairsInLevel.Count];
            for (int i = 0; i < levelConfig.pairsInLevel.Count; i++)
            {
                if (levelConfig.pairsInLevel[i] != null)
                {
                    cardIcons[i] = levelConfig.pairsInLevel[i].cardSprite;
                }
            }
            timeLimit = levelConfig.timeLimitSeconds;
            maxLives = levelConfig.maxLives;
        }

        if (knowledgeItemPrefab == null)
        {
            GameObject[] allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjs)
            {
                if (obj != null && obj.name == "KnowledgeItem" && obj.scene.name == null)
                {
                    knowledgeItemPrefab = obj;
                    break;
                }
            }
        }

        if (scoreText == null) scoreText = GameObject.Find("PairCountText")?.GetComponent<TMP_Text>();
        if (timerText == null) timerText = GameObject.Find("TimerText")?.GetComponent<TMP_Text>();
        if (livesText == null) livesText = (GameObject.Find("HeartText") ?? GameObject.Find("LivesText"))?.GetComponent<TMP_Text>();

        if (victoryUI == null)
        {
            victoryUI = GameObject.Find("GameResultObject");
            if (victoryUI == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform res = canvas.transform.Find("GameResultObject");
                    if (res != null) victoryUI = res.gameObject;
                }
            }
        }

        if (knowledgePanel == null) knowledgePanel = GameObject.Find("KnowledgeSection");

        if (cachedKnowledgeContent == null)
        {
            if (knowledgePanel != null)
            {
                cachedKnowledgeContent = knowledgePanel.transform.Find("PaperSheet/KnowlegdeScrollView/Viewport/Content")
                                      ?? knowledgePanel.transform.Find("KnowlegdeScrollView/Viewport/Content")
                                      ?? knowledgePanel.transform.Find("Viewport/Content");
            }
            if (cachedKnowledgeContent == null)
            {
                GameObject contentObj = GameObject.Find("Content");
                if (contentObj != null) cachedKnowledgeContent = contentObj.transform;
            }
        }

        if (clubNameText == null) clubNameText = GameObject.Find("ClubNameText")?.GetComponent<TMP_Text>();
        if (knowledgeDescText == null) knowledgeDescText = GameObject.Find("KnowledgeDescText")?.GetComponent<TMP_Text>();

        if ((clubNameText == null || knowledgeDescText == null) && knowledgePanel != null)
        {
            TMP_Text[] allTexts = knowledgePanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in allTexts)
            {
                string n = t.gameObject.name.ToLower();
                if (clubNameText == null && (n.Contains("club") || n.Contains("name"))) clubNameText = t;
                else if (knowledgeDescText == null && (n.Contains("desc") || n.Contains("info") || n.Contains("knowledge"))) knowledgeDescText = t;
            }
        }
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var existing = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existing == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }

    public void RestartGame()
    {
        InitializeGame();
    }
}
