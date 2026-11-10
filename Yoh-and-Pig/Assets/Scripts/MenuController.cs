using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [Header("Menu")]
    public GameObject menuCanvas;
    [SerializeField] private TabController tabController;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private Animator playerAnimator;

    private bool menuOpen;
    private bool movementWasEnabled;
    private bool inputWasActive;

    private Coroutine focusRoutine;

    private void Start()
    {
        if (menuCanvas == null || tabController == null ||
            playerMovement == null || playerInput == null ||
            playerBody == null)
        {
            Debug.LogError(
                "Assign the menu, tab controller, player movement, " +
                "player input and player Rigidbody2D.",
                this
            );

            enabled = false;
            return;
        }

        menuCanvas.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        bool togglePressed =
            (keyboard != null && keyboard.tabKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.startButton.wasPressedThisFrame);

        if (togglePressed)
        {
            ToggleMenu();
            return;
        }

        bool closePressed =
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

        if (menuOpen && closePressed)
        {
            CloseMenu();
        }
    }

    public void ToggleMenu()
    {
        if (menuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (menuOpen)
        {
            return;
        }

        // Do not open during a map transition or another input lock.
        if (!playerMovement.isActiveAndEnabled ||
            !playerInput.inputIsActive)
        {
            return;
        }

        movementWasEnabled = playerMovement.enabled;
        inputWasActive = playerInput.inputIsActive;

        // Cancels movement input before disabling the movement script.
        playerInput.DeactivateInput();
        playerMovement.enabled = false;
        playerBody.linearVelocity = Vector2.zero;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isWalking", false);
        }

        menuOpen = true;
        menuCanvas.SetActive(true);

        focusRoutine = StartCoroutine(FocusAfterOpening());
    }

    public void CloseMenu()
    {
        if (!menuOpen)
        {
            return;
        }

        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem != null &&
            eventSystem.currentSelectedGameObject != null &&
            eventSystem.currentSelectedGameObject.transform
                .IsChildOf(menuCanvas.transform))
        {
            eventSystem.SetSelectedGameObject(null);
        }

        menuCanvas.SetActive(false);
        menuOpen = false;

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = movementWasEnabled;
        }

        if (playerInput != null && inputWasActive)
        {
            playerInput.ActivateInput();
        }
    }

    private IEnumerator FocusAfterOpening()
    {
        yield return null;

        if (menuOpen && menuCanvas.activeInHierarchy)
        {
            tabController.FocusActiveTab();
        }

        focusRoutine = null;
    }

    private void OnDisable()
    {
        if (menuOpen)
        {
            CloseMenu();
        }
    }
}