using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TabIndicator : MonoBehaviour,
    IPointerEnterHandler,
    ISelectHandler,
    IDeselectHandler
{
    public enum IndicatorStyle
    {
        OutsideBorder,
        InteriorGlow
    }

    public enum BorderSide
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Style")]
    [SerializeField]
    private IndicatorStyle style =
        IndicatorStyle.OutsideBorder;

    [Header("Outside Border Settings")]
    [SerializeField] private BorderSide side = BorderSide.Up;

    [Tooltip("The RectTransform matching the menu's outer border.")]
    [SerializeField] private RectTransform menuBorder;

    [Tooltip("Must be a direct child of Menu Border.")]
    [SerializeField] private RectTransform indicatorRoot;

    [Tooltip("Glow behind the outside icon.")]
    [SerializeField] private GameObject glowObject;

    [Tooltip("Gap between the border and the nearest edge of the icon root.")]
    [SerializeField, Min(0f)] private float borderGap = 12f;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float bobHeight = 6f;
    [SerializeField, Min(0f)] private float bobFrequency = 1.5f;

    [Header("Interior Settings")]
    [Tooltip("Glow around this button or inventory slot.")]
    [SerializeField] private GameObject interiorGlow;

    private TabController controller;
    private Button button;
    private RectTransform buttonRect;
    private int tabIndex;

    private bool initialized;
    private bool visible;
    private bool locked;
    private float animationTime;

    private void Awake()
    {
        EnsureInitialized();
    }

    // Called by TabController for page tabs.
    // Exit and inventory buttons work without this call.
    public void Initialize(TabController owner, int index)
    {
        EnsureInitialized();

        controller = owner;
        tabIndex = index;

        SetVisualState(false, false);
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        button = GetComponent<Button>();
        buttonRect = GetComponent<RectTransform>();

        // Our icon/glow replaces the standard colour transitions.
        button.transition = Selectable.Transition.None;

        DisableRaycasts(indicatorRoot != null
            ? indicatorRoot.gameObject
            : null);

        DisableRaycasts(interiorGlow);

        initialized = true;
        SetVisualState(false, false);
    }

    private void DisableRaycasts(GameObject visual)
    {
        if (visual == null)
        {
            return;
        }

        foreach (Graphic graphic in
                 visual.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
    }

    public void SetVisualState(bool show, bool pageIsActive)
    {
        EnsureInitialized();

        if (show != visible || pageIsActive != locked)
        {
            animationTime = 0f;
        }

        visible = show;
        locked = pageIsActive;

        bool outside = style == IndicatorStyle.OutsideBorder;

        if (indicatorRoot != null)
        {
            indicatorRoot.gameObject.SetActive(visible && outside);
        }

        if (glowObject != null)
        {
            glowObject.SetActive(visible && outside && locked);
        }

        if (interiorGlow != null)
        {
            interiorGlow.SetActive(visible && !outside);
        }

        if (visible && outside)
        {
            PositionOutsideIcon();
        }
    }

    private void LateUpdate()
    {
        if (!initialized || !visible ||
            style != IndicatorStyle.OutsideBorder)
        {
            return;
        }

        if (!locked)
        {
            animationTime += Time.unscaledDeltaTime;
        }

        PositionOutsideIcon();

        if (Time.frameCount % 30 == 0)
        {
            Debug.Log(
                $"{name}: Locked={locked}, " +
                $"Time={animationTime:F2}, " +
                $"Height={bobHeight}, Frequency={bobFrequency}, " +
                $"Root={(indicatorRoot != null ? indicatorRoot.name : "Missing")}, " +
                $"CorrectParent={(indicatorRoot != null && indicatorRoot.parent == menuBorder)}, " +
                $"Position={(indicatorRoot != null ? indicatorRoot.anchoredPosition.ToString() : "Missing")}",
                this
            );
        }
    }

    private void PositionOutsideIcon()
    {
        if (menuBorder == null || indicatorRoot == null)
        {
            return;
        }

        // Anchors below are relative to the menu border.
        if (indicatorRoot.parent != menuBorder)
        {
            return;
        }

        Vector3 buttonCentreWorld =
            buttonRect.TransformPoint(buttonRect.rect.center);

        Vector3 buttonCentreLocal =
            menuBorder.InverseTransformPoint(buttonCentreWorld);

        Vector2 borderCentre = menuBorder.rect.center;

        // Animation stays entirely outside the fixed minimum gap.
        float extraGap = locked
            ? 0f
            : (1f - Mathf.Cos(
                animationTime * bobFrequency * Mathf.PI * 2f
            )) * 0.5f * bobHeight;

        float gap = borderGap + extraGap;

        Vector2 anchor;
        Vector2 pivot;
        Vector2 position;

        switch (side)
        {
            case BorderSide.Down:
                anchor = new Vector2(0.5f, 0f);
                pivot = new Vector2(0.5f, 1f);
                position = new Vector2(
                    buttonCentreLocal.x - borderCentre.x,
                    -gap
                );
                break;

            case BorderSide.Left:
                anchor = new Vector2(0f, 0.5f);
                pivot = new Vector2(1f, 0.5f);
                position = new Vector2(
                    -gap,
                    buttonCentreLocal.y - borderCentre.y
                );
                break;

            case BorderSide.Right:
                anchor = new Vector2(1f, 0.5f);
                pivot = new Vector2(0f, 0.5f);
                position = new Vector2(
                    gap,
                    buttonCentreLocal.y - borderCentre.y
                );
                break;

            default:
                anchor = new Vector2(0.5f, 1f);
                pivot = new Vector2(0.5f, 0f);
                position = new Vector2(
                    buttonCentreLocal.x - borderCentre.x,
                    gap
                );
                break;
        }

        indicatorRoot.anchorMin = anchor;
        indicatorRoot.anchorMax = anchor;
        indicatorRoot.pivot = pivot;
        indicatorRoot.anchoredPosition = position;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnsureInitialized();

        if (!CanFocus())
        {
            return;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        ShowFocus();
    }

    public void OnSelect(BaseEventData eventData)
    {
        EnsureInitialized();

        if (CanFocus())
        {
            ShowFocus();
        }
    }

    private void ShowFocus()
    {
        if (controller != null)
        {
            controller.SetFocusedTab(tabIndex);
        }
        else
        {
            // Standalone buttons, such as Exit or inventory slots.
            SetVisualState(true, false);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (controller != null)
        {
            controller.ClearFocusedTab(tabIndex);
        }
        else
        {
            SetVisualState(false, false);
        }
    }

    private bool CanFocus()
    {
        return button.isActiveAndEnabled && button.IsInteractable();
    }

    private void OnDisable()
    {
        if (initialized)
        {
            SetVisualState(false, false);
        }
    }
}