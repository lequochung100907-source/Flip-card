using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class CardController : MonoBehaviour, IPointerClickHandler
{
    [Header("Giao diện thẻ bài")]
    public Image icon;
    public int id;
    public bool isFlipped = false;
    public bool isMatched = false;

    public CardPairData Data { get; private set; }

    private static readonly int HashIsFlip = Animator.StringToHash("isFlip");
    private static readonly int HashCardIdle = Animator.StringToHash("CardIdle");
    private static readonly int HashCardFlipUp = Animator.StringToHash("CardFlipUp");
    private static readonly int HashCardFlipDown = Animator.StringToHash("CardFlipDown");

    private static readonly WaitForSeconds HalfFlipWait = new WaitForSeconds(0.13f);

    [SerializeField] private GameObject frontGroup;
    [SerializeField] private GameObject backGroup;

    private Sprite cardSprite;
    private Button currentBtn;
    private Animator animator;
    private CanvasGroup canvasGroup;
    private Coroutine swapCoroutine;
    private bool isInitialized = false;

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (transform.parent != null && transform.parent.childCount > 1)
        {
            transform.SetSiblingIndex(Random.Range(0, transform.parent.childCount));
        }
    }

    private void InitializeComponents()
    {
        if (isInitialized) return;

        animator = GetComponent<Animator>();
        currentBtn = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (currentBtn != null)
        {
            currentBtn.onClick.RemoveListener(OnCardClicked);
            currentBtn.onClick.AddListener(OnCardClicked);
        }

        Image rootImg = GetComponent<Image>();
        if (rootImg != null) rootImg.raycastTarget = true;

        if (frontGroup == null)
        {
            Transform fg = transform.Find("VisualContainer/FrontGroup") ?? transform.Find("FrontGroup");
            if (fg != null) frontGroup = fg.gameObject;
        }

        if (backGroup == null)
        {
            Transform bg = transform.Find("VisualContainer/BackGroup")
                        ?? transform.Find("VisualContainer/BackCard")
                        ?? transform.Find("BackGroup")
                        ?? transform.Find("BackCard");
            if (bg != null) backGroup = bg.gameObject;
        }

        if (icon == null)
        {
            Transform logo = transform.Find("VisualContainer/FrontGroup/FrontLogo")
                          ?? transform.Find("FrontGroup/FrontLogo")
                          ?? transform.Find("FrontLogo");
            if (logo != null) icon = logo.GetComponent<Image>();
        }

        isInitialized = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardClicked();
    }

    public void InitCard(int _id, Sprite iconSprite)
    {
        InitCard(_id, iconSprite, null);
    }

    public void InitCard(int _id, Sprite iconSprite, CardPairData pairData)
    {
        InitializeComponents();

        id = _id;
        cardSprite = iconSprite;

        if (pairData != null)
        {
            Data = pairData;
            if (cardSprite == null) cardSprite = pairData.cardSprite;
        }
        else if (GameManager.Instance != null)
        {
            Data = GameManager.Instance.GetPairData(_id);
            if (cardSprite == null && Data != null) cardSprite = Data.cardSprite;
        }

        if (icon != null && cardSprite != null)
        {
            icon.sprite = cardSprite;
        }

        isFlipped = false;
        isMatched = false;

        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        if (currentBtn != null)
        {
            currentBtn.interactable = true;
        }

        if (frontGroup != null) frontGroup.SetActive(false);
        if (backGroup != null) backGroup.SetActive(true);

        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(HashIsFlip, false);
            if (animator.HasState(0, HashCardIdle))
            {
                animator.Play(HashCardIdle, 0, 0f);
            }
        }
    }

    public void InitCard(string pairId, Sprite iconSprite, CardPairData pairData = null)
    {
        int numericId = !string.IsNullOrEmpty(pairId) ? Mathf.Abs(pairId.GetHashCode()) : 0;
        InitCard(numericId, iconSprite, pairData);
    }

    public void Setup(CardPairData pairData, GameManager manager)
    {
        if (pairData != null)
        {
            int numericId = !string.IsNullOrEmpty(pairData.pairId) ? Mathf.Abs(pairData.pairId.GetHashCode()) : 0;
            InitCard(numericId, pairData.cardSprite, pairData);
        }
    }

    public void OnCardClicked()
    {
        if (isFlipped || isMatched || GameManager.Instance == null || !GameManager.Instance.CanFlip())
        {
            return;
        }

        isFlipped = true;
        PlayFlipAnimation(true);

        if (swapCoroutine != null) StopCoroutine(swapCoroutine);
        swapCoroutine = StartCoroutine(SwapFaceDelayed(true));

        GameManager.Instance.OnCardFlipped(this);
    }

    public void Unflip()
    {
        isFlipped = false;
        PlayFlipAnimation(false);

        if (swapCoroutine != null) StopCoroutine(swapCoroutine);
        swapCoroutine = StartCoroutine(SwapFaceDelayed(false));
    }

    private IEnumerator SwapFaceDelayed(bool showFront)
    {
        yield return HalfFlipWait;
        if (showFront) OnFlipShowFront();
        else OnFlipShowBack();
        swapCoroutine = null;
    }

    private void PlayFlipAnimation(bool flipUp)
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;

        animator.SetBool(HashIsFlip, flipUp);
        int targetState = flipUp ? HashCardFlipUp : HashCardFlipDown;
        if (animator.HasState(0, targetState))
        {
            animator.Play(targetState, 0, 0f);
        }
    }

    public void HideCard()
    {
        isMatched = true;
        if (currentBtn != null) currentBtn.interactable = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnFlipShowFront()
    {
        if (frontGroup != null) frontGroup.SetActive(true);
        if (backGroup != null) backGroup.SetActive(false);

        if (icon != null)
        {
            if (cardSprite != null) icon.sprite = cardSprite;
            else if (Data != null && Data.cardSprite != null) icon.sprite = Data.cardSprite;
        }
    }

    public void OnFlipShowBack()
    {
        if (frontGroup != null) frontGroup.SetActive(false);
        if (backGroup != null) backGroup.SetActive(true);
    }

    public void OnAnimFinished() { }
}
