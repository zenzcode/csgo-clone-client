using System;
using System.Collections;
using System.Collections.Generic;
using Player.Game.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Game
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;

        [SerializeField] private float sensitivity = 1;

        [SerializeField] private float movementSpeed = 15;

        [SerializeField] private float jumpForce = 10;

        private ClientInput _input;

        private InputAction _lookAction;

        private float _yaw, _pitch;
        
        private List<MovementTick> _unacknowledgedTicks;

        private void Awake()
        {
            _input = new ClientInput();
            _lookAction = _input.Player.Look;
            _lookAction.Enable();

            _lookAction.performed += PlayerLook;
        }

        private void FixedUpdate()
        {
        }

        private void PlayerLook(InputAction.CallbackContext callbackContext)
        {
            Debug.Log(callbackContext.ReadValue<Vector2>());
        }
    }
}
