using UnityEngine.InputSystem;
namespace FreezerGameplay
{
    public static class GameInput
    {
        public static bool CancelPressed => RuntimeControls.Current ? !RuntimeControls.Current.SuppressInput && RuntimeControls.Current.Pressed("System/Cancel") : (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)||(Gamepad.current?.buttonEast.wasPressedThisFrame ?? false);
        public static bool InteractPressed(PlayerInput input)
        {
            var action = input && input.actions != null ? input.actions.FindAction("Player/Interact", false) : null;
            if (action != null) return action.enabled && action.WasPressedThisFrame();
            return (Keyboard.current?.eKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false);
        }
    }
}
