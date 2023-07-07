using System;
using System.Collections;
using System.Collections.Generic;
using Enums;
using Manager;
using Managers;
using Player.Game.Movement;
using Riptide;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Game
{
    public class PlayerController : MonoBehaviour
    {
        //TOOD: Move sensitivity to settings class
        [SerializeField] private float sensitivity = 10;

        [SerializeField] private float movementSpeed = 15;

        [SerializeField] private float jumpForce = 10;

        //Only used on simulated client to determine wheter we are receiving an older tick than last
        private int LastHandledTick = 0;

        private ClientInput _input;

        private InputAction _lookAction;

        private float _yaw, _pitch;
        
        private List<MovementTick> _unacknowledgedTicks;

        public Camera PlayerCam { private get; set; }

        public GameObject PlayerModel { private get; set; }

        public GameObject ModelParent { private get; set; }

        public Player Owner { get; set; }

        private float _mouseDeltaX = 0, _mouseDeltaY = 0;

        private Vector3 _posStartTick = Vector3.zero, _eulerStartTick = Vector3.zero;
        
        private void Awake()
        {
            _unacknowledgedTicks = new List<MovementTick>();
            _input = new ClientInput();
            _lookAction = _input.Player.Look;
            _lookAction.Enable();

            _lookAction.performed += PlayerLook;

            if(Owner.IsLocal)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void FixedUpdate()
        {
            _posStartTick = Owner.transform.position;
            _eulerStartTick = Owner.transform.eulerAngles;
            if (PlayerManager.Instance.IsLocal(Owner.PlayerId))
            {
                LookAround(Time.deltaTime);
                SendNewMovementTick();
            }
        }

        private void LookAround(float DeltaTime)
        {
            _yaw += _mouseDeltaX * DeltaTime * sensitivity;
            _pitch = Mathf.Clamp(_pitch - (_mouseDeltaY * DeltaTime * sensitivity), -89, 89);
            PlayerCam.transform.eulerAngles = new Vector3(_pitch, _yaw, 0);
            ModelParent.transform.rotation = Quaternion.Euler(ModelParent.transform.rotation.x, _yaw, ModelParent.transform.rotation.z);
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

            var message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerMessages.Tick);
            message.Add(movementTick);
            NetworkManager.Instance.Client.Send(message);
        }

        private void PlayerLook(InputAction.CallbackContext callbackContext)
        {
            var delta = callbackContext.ReadValue<Vector2>();
            _mouseDeltaX = delta.x;
            _mouseDeltaY = delta.y;
        }
    }
}
