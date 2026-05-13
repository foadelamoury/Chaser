
using System.Globalization;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : NetworkBehaviour
{
    [Header("Input Values")]
    public Vector2 MovementInput { get; private set; }
    public bool IsBraking { get; private set; }

    public override void OnNetworkSpawn()
    {
        // Get the InputHandler component attached to this object
        var playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            if (!IsOwner)
            {
                playerInput.enabled = false;
            }
            else
            {
                playerInput.enabled = true;
            }
        }
    }

    public void OnMove(InputValue inputValue)
    {
        MovementInput = inputValue.Get<Vector2>();
    }

    public void OnBrake(InputValue inputValue)
    {
        IsBraking = inputValue.isPressed;
    }





}
