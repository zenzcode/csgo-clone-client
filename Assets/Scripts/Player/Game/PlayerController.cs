using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enums;
using Manager;
using Managers;
using Misc;
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
        private uint LastHandledTick = 0;

        private ClientInput _input;

        private InputAction _lookAction;

        private float _yaw, _pitch;
        
        private List<MovementTick> _unacknowledgedTicks;

        public Camera PlayerCam { private get; set; }

        public GameObject PlayerModel { private get; set; }

        public GameObject ModelParent { private get; set; }

        public Player Owner { get; set; }

        private float _mouseDeltaX = 0, _mouseDeltaY = 0;

        private Vector2 _yawPitchStartTick = Vector2.zero;

        private Vector3 _posStartTick = Vector3.zero;
        
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

        private void OnEnable()
        {
            EventManager.MovementTickResultReceived += EventManager_MovementTickResultReceived;
        }

        private void OnDisable()
        {
            EventManager.MovementTickResultReceived -= EventManager_MovementTickResultReceived;
        }

        private void FixedUpdate()
        {
            _posStartTick = Owner.transform.position;
            _yawPitchStartTick = new Vector2(_yaw, _pitch);
            if (PlayerManager.Instance.IsLocal(Owner.PlayerId))
            {
                LookAround(Time.deltaTime);
                SendNewMovementTick();
            }
        }

        private void LookAround(float deltaTime) 
        {
            var newYawPitch = CalculateLook(_mouseDeltaX, _mouseDeltaY, _yaw, _pitch, deltaTime);
            _yaw = newYawPitch.x;
            _pitch = newYawPitch.y;
            SetPlayerRotation(_yawPitchStartTick, _yaw, _pitch, deltaTime);
        }

        private Vector2 CalculateLook(float mouseDeltaX, float mouseDeltaY, float yaw, float pitch, float deltaTime)
        {
            yaw += mouseDeltaX * deltaTime * sensitivity;
            pitch = Mathf.Clamp(pitch - (mouseDeltaY * deltaTime * sensitivity), -89, 89);

            return new Vector2(yaw, pitch);
        }

        private void SetPlayerRotation(Vector2 yawPitchTickStart, float yaw, float pitch, float deltaTime)
        {
            PlayerCam.transform.eulerAngles = Vector3.Lerp(new Vector3(yawPitchTickStart.y, yawPitchTickStart.x, 0), new Vector3(pitch, yaw, 0), deltaTime);
            ModelParent.transform.rotation = Quaternion.Euler(ModelParent.transform.rotation.x, yaw, ModelParent.transform.rotation.z);
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
                Yaw = _yawPitchStartTick.x,
                EndYaw = _yaw,
                Pitch = _yawPitchStartTick.y,
                EndPitch = _pitch,
                MouseDeltaX = _mouseDeltaX,
                MouseDeltaY = _mouseDeltaY,
                DeltaTime = Time.deltaTime,
                Sensitivity = sensitivity
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

        [MessageHandler((ushort)ServerToClientMessages.TickResult)]
        private static void TickResultReceived(Message message)
        {
            var movementTick = message.GetSerializable<MovementTickResult>();
            EventManager.CallMovementTickResultReceived(movementTick);
        }

        private void EventManager_MovementTickResultReceived(MovementTickResult tickResult)
        {
            if(tickResult.ClientId != Owner.PlayerId)
            {
                return;
            }

            if(Owner.IsLocal)
            {
                //DISTANCE CHECK
                RemoveOlderUnacknowledgedMoves(tickResult.Tick);
                CheckLookDelta(tickResult);
            }
            else
            {
                //INTERP
                SimulateTick(tickResult);

            }
        }

        private void RemoveOlderUnacknowledgedMoves(uint tick)
        {
            _unacknowledgedTicks = _unacknowledgedTicks.Where(movementTick => movementTick.Tick > tick).ToList();
        }

        private void CheckLookDelta(MovementTickResult tickResult)
        {
            if(Mathf.Abs(tickResult.ActualEndYaw - tickResult.PassedEndYaw) > Statics.MaxYawPitchDelta || Mathf.Abs(tickResult.ActualEndPitch - tickResult.PassedEndPitch) > Statics.MaxYawPitchDelta)
            {
                Debug.LogWarning($"Server and Client result differed to much - recalculating.");
                RecalculateLookSinceTick(tickResult);
            }
        }

        private void RecalculateLookSinceTick(MovementTickResult tickResult)
        {
            var lastTickStartYaw = tickResult.StartYaw;
            var lastTickStartPitch = tickResult.StartPitch;
            var lastTickEndYaw = tickResult.ActualEndYaw;
            var lastTickEndPitch = tickResult.ActualEndPitch;
            var lastTickDeltaTime = tickResult.DeltaTime;

            for(var index = 0; index < _unacknowledgedTicks.Count; ++index)
            {
                var tick = _unacknowledgedTicks[index];
                lastTickStartYaw = tick.Yaw = lastTickEndYaw;
                lastTickStartPitch = tick.Pitch = lastTickEndPitch;
                lastTickDeltaTime = tickResult.DeltaTime;
                var newYawPitch = CalculateLook(tick.MouseDeltaX, tick.MouseDeltaX, tick.Yaw, tick.Pitch, tick.DeltaTime);
                lastTickEndYaw = tick.EndYaw = newYawPitch.x;
                lastTickEndPitch = tick.EndPitch = newYawPitch.y;
            }

            SetPlayerRotation(new Vector2(lastTickStartYaw, lastTickStartPitch), lastTickEndYaw, lastTickEndPitch, lastTickDeltaTime);
        }

        private void SimulateTick(MovementTickResult tickResult)
        {
            if (tickResult.Tick < LastHandledTick)
            {
                return;
            }

            LastHandledTick = tickResult.Tick;

            var deltaYaw = tickResult.ActualEndYaw - tickResult.StartYaw;

            //use later for aim offset
            var deltaPitch = tickResult.ActualEndPitch - tickResult.StartPitch;

            ModelParent.transform.Rotate(new Vector3(0, deltaYaw, 0), Space.Self);
        }
    }
}
