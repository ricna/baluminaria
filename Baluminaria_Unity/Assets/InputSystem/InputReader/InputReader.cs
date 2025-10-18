using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static BalloonInputActions;


[CreateAssetMenu(fileName = "New Input Reader", menuName = "Unrez/Input Reader")]
public class InputReader : ScriptableObject, IBalloonControlsActions
{
    private BalloonInputActions controls;

    public event Action<Vector2> OnMoveEvent;
    public event Action<bool> OnAscendEvent;
    public event Action<bool> OnDescendEvent;
    public event Action<Vector2> OnLookEvent;
    public event Action OnToggleAutomaticEvent;
    public event Action OnToggleMenuEvent;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new BalloonInputActions();
            controls.BalloonControls.SetCallbacks(this);
        }
        controls.BalloonControls.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        //Vector2 input = context.ReadValue<Vector2>();
        OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnAscend(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnAscendEvent?.Invoke(true);
        }
        else if (context.canceled)
        {
            OnAscendEvent?.Invoke(false);
        }
    }

    public void OnDescend(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnDescendEvent?.Invoke(true);
        }
        else if (context.canceled)
        {
            OnDescendEvent?.Invoke(false);
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        OnLookEvent?.Invoke(context.ReadValue<Vector2>());

    }

    public void OnToggleAutomatic(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnToggleAutomaticEvent?.Invoke();
        }
    }

    public void OnToggleMenu(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnToggleMenuEvent?.Invoke();
        }

    }
}

