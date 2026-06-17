// PlayerInputSO.cs (기존 거에 추가)
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace _00_Members.PTY.Scripts.SO
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/PlayerInput")]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public event UnityAction<Vector2> OnMovement;
        public event UnityAction<Vector2> OnLookAction;

        private Controls _controls;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls?.Player.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            OnMovement?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            OnLookAction?.Invoke(context.ReadValue<Vector2>());
        }
    }
}