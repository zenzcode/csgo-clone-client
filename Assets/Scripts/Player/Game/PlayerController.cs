using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using Managers;
using Player.Game.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Game
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float sensitivity = 1;

        [SerializeField] private float movementSpeed = 15;

        [SerializeField] private float jumpForce = 10;

        private ClientInput _input;

        private InputAction _lookAction;

        private float _yaw, _pitch;
        
        private List<MovementTick> _unacknowledgedTicks;

        public Camera PlayerCam { private get; set; }

        public GameObject PlayerModel { private get; set; }

        public Player Owner { private get; set; }

        private float _mouseDeltaX = 0, _mouseDeltaY = 0;

        private Vector3 _posStartTick = Vector3.zero, _eulerStartTick = Vector3.zero;
        
        private void Awake()
        {
            _unacknowledgedTicks = new List<MovementTick>();
            _input = new ClientInput();
            _lookAction = _input.Player.Look;
            _lookAction.Enable();

            _lookAction.performed += PlayerLook;
        }

        private void FixedUpdate()
        {
            _posStartTick = Owner.transform.position;
            _eulerStartTick = Owner.transform.eulerAngles;
            if (PlayerManager.Instance.IsLocal(Owner.PlayerId))
            {
                LookAround();
                SendNewMovementTick();
                
            }
            else
            {
                //HandleNewMovementTick();
            }
        }

        private void LookAround()
        {
            Debug.Log("LOOK");
            _yaw += _mouseDeltaX;
            _pitch = Mathf.Clamp(_pitch + _mouseDeltaY, -89, 89);
            PlayerCam.transform.eulerAngles = new Vector3(_yaw, 0, 0);
            PlayerModel.transform.eulerAngles = new Vector3(0, 0, _pitch);
            Debug.Log($"YAW: {_yaw}; PITCH: {_pitch}");
        }

        private void SendNewMovementTick()
        {
            var movementTick = new MovementTick()
            {
                Tick = NetworkManager.Instance.Tick,
                Input = 0,
                ClientId = Owner.PlayerId,
                StartPosition = _posStartTick,
                EndPosition = Owner.transform.position,
                EulerAngles = _eulerStartTick,
                EndEulerAngles = Owner.transform.eulerAngles,
                MouseDeltaX = _mouseDeltaX,
                MouseDeltaY = _mouseDeltaY,
                DeltaTime = Time.deltaTime
            };
            
            _unacknowledgedTicks.Add(movementTick);

            _mouseDeltaX = 0;
            _mouseDeltaY = 0;
        }

        private void PlayerLook(InputAction.CallbackContext callbackContext)
        {
            var delta = callbackContext.ReadValue<Vector2>();
            _mouseDeltaX = delta.x;
            _mouseDeltaY = delta.y;
        }
    }
}
