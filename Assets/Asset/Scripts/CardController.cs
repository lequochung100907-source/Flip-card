using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
    public Image icon;
    public GameObject frontGroup;
    public GameObject backGroup;
    public int id;
    public bool isFlipped = false;
    public bool isMatched = false;

    private Button currentBtn;
    private Animator animator;
    private CanvasGroup canvasGroup;
    private static readonly int HashIsFlip = Animator.StringToHash("isFlip");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        currentBtn = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (frontGroup == null)
        {
            Transform frontTf = transform.Find("VisualContainer/FrontGroup");
            if (frontTf != null) frontGroup = frontTf.gameObject;
        }
        if (backGroup == null)
        {
            Transform backTf = transform.Find("VisualContainer/BackGroup");
            if (backTf != null) backGroup = backTf.gameObject;
        }
        if (icon == null)
        {
            Transform logoTf = transform.Find("VisualContainer/FrontGroup/FrontLogo");
            if (logoTf != null) icon = logoTf.GetComponent<Image>();
        }

        if (currentBtn != null)
        {
            currentBtn.onClick.AddListener(OnCardClicked);
        }
    }

    private void Start()
    {
        if (transform.parent != null && transform.parent.childCount > 1)
        {
            transform.SetSiblingIndex(Random.Range(0, transform.parent.childCount));
        }
    }

    public void InitCard(int _id, Sprite iconSprite)
    {
        id = _id;
        if (icon != null)
        {
            icon.sprite = iconSprite;
        }
        isFlipped = false;
        isMatched = false;

        if (frontGroup != null) frontGroup.SetActive(false);
        if (backGroup != null) backGroup.SetActive(true);
    }

    public void OnCardClicked()
    {
        if (isFlipped || isMatched || GameManager.Instance == null || !GameManager.Instance.CanFlip())
        {
            return;
        }

        isFlipped = true;
        if (animator != null)
        {
            animator.SetBool(HashIsFlip, true);
        }
        GameManager.Instance.OnCardFlipped(this);
    }

    public void Unflip()
    {
        isFlipped = false;
        if (animator != null)
        {
            animator.SetBool(HashIsFlip, false);
        }
    }

    public void HideCard()
    {
        isMatched = true;
        if (currentBtn != null)
        {
            currentBtn.interactable = false;
        }

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
    }

    public void OnFlipShowBack()
    {
        if (frontGroup != null) frontGroup.SetActive(false);
        if (backGroup != null) backGroup.SetActive(true);
    }
}
