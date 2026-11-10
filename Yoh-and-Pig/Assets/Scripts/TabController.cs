using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [Header("Keep all arrays in matching order")]
    public Image[] tabImages;
    public Button[] tabButtons;
    public GameObject[] pages;

    [Header("Page Colours")]
    [SerializeField] private Color activeTabColour = Color.white;
    [SerializeField] private Color inactiveTabColour = Color.grey;

    private TabIndicator[] indicators;

    private int activeTab;
    private int focusedTab = -1;
    private bool configured;

    private void Awake()
    {
        if (pages == null || tabImages == null ||
            tabButtons == null || pages.Length == 0 ||
            pages.Length != tabImages.Length ||
            pages.Length != tabButtons.Length)
        {
            Debug.LogError(
                "Pages, Tab Images and Tab Buttons must contain " +
                "the same number of entries.",
                this
            );
            return;
        }

        indicators = new TabIndicator[tabButtons.Length];

        // Validate every reference before connecting the buttons.
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null ||
                tabImages[i] == null ||
                tabButtons[i] == null)
            {
                Debug.LogError(
                    $"Missing tab reference at index {i}.",
                    this
                );
                return;
            }

            indicators[i] =
                tabButtons[i].GetComponent<TabIndicator>();

            if (indicators[i] == null)
            {
                Debug.LogError(
                    $"Add TabIndicator to {tabButtons[i].name}.",
                    tabButtons[i]
                );
                return;
            }
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tabIndex = i;

            indicators[i].Initialize(this, tabIndex);

            // The icon provides navigation feedback.
            tabButtons[i].transition = Selectable.Transition.None;

            tabButtons[i].onClick.AddListener(
                () => ActivateTab(tabIndex)
            );
        }

        configured = true;
        ActivateTab(0);
    }

    public void ActivateTab(int tabNo)
    {
        if (!configured || tabNo < 0 || tabNo >= pages.Length)
        {
            return;
        }

        activeTab = tabNo;

        for (int i = 0; i < pages.Length; i++)
        {
            bool active = i == activeTab;

            pages[i].SetActive(active);

            tabImages[i].color = active
                ? activeTabColour
                : inactiveTabColour;
        }

        RefreshIndicators();
    }

    public void SetFocusedTab(int tabNo)
    {
        if (!configured || tabNo < 0 || tabNo >= tabButtons.Length)
        {
            return;
        }

        focusedTab = tabNo;
        RefreshIndicators();
    }

    public void ClearFocusedTab(int tabNo)
    {
        if (!configured || focusedTab != tabNo)
        {
            return;
        }

        focusedTab = -1;
        RefreshIndicators();
    }

    private void RefreshIndicators()
    {
        for (int i = 0; i < indicators.Length; i++)
        {
            indicators[i].SetVisualState(
                i == focusedTab,
                i == activeTab
            );
        }
    }

    public void FocusActiveTab()
    {
        if (!configured || EventSystem.current == null)
        {
            return;
        }

        Button button = tabButtons[activeTab];

        if (!button.isActiveAndEnabled || !button.IsInteractable())
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);

        SetFocusedTab(activeTab);
    }
}