using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enums;
using Helpe;
using Manager;
using Managers;
using Misc;
using Player.Game.Movement;
using Riptide;
using Unity.VisualScripting.FullSerializer.Internal.Converters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Game
{
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        //TOOD: Move sensitivity to settings class
        [SerializeField] private float sensitivity = 10;

        [SerializeField] private float movementSpeed = 15;

        [SerializeField] private float jumpForce = 10;

        [SerializeField] private LayerMask playerLayer;

        //Only used on simulated client to determine wheter we are receiving an older tick than last
        private uint LastHandledTick = 0;

        private ClientInput _input;

        private InputAction _lookAction;

        private InputAction _moveAction;

        private float _yaw, _pitch;

        private List<MovementTick> _unacknowledgedTicks;

        public Camera PlayerCam { private get; set; }

        public GameObject PlayerModel { private get; set; }

        public GameObject ModelParent { private get; set; }

        public Player Owner { get; set; }

        private float _mouseDeltaX = 0, _mouseDeltaY = 0;

        private Vector2 _lastMoveNormalized = Vector2.zero;

        private Vector2 _yawPitchStartTick = Vector2.zero;

        private Vector3 _posStartTick = Vector3.zero;

        private Rigidbody _rigidbody;

        private CapsuleCollider _capsuleCollider;

        private Collider[] collisions = new Collider[10];

        private Vector3 _targetPosition = Vector3.zero;

        private bool _hasTargetPosition = false;

        private Animator _animator;

        #region Simulation
        #region Velocity Interpolation
        private bool _reachedTargetVelocity = false;
        private float _targetVelocity = 0;
        [Header("Simulation Settings")]
        [Space(10)]
        [Header("Velocity Settings")]
        [SerializeField] private float velocityRampUpDownSpeed = 4;
        #endregion Velocity Interpolation
        #region Direction Interpolation
        private bool _reachedTargetDirection = false;
        private float _targetDirection = 0;
        [Space(10)]
        [Header("Direction Settings")]
        [SerializeField] private float directionRampUpDownSpeed = 4;
        #endregion Direction Interpolation
        #endregion Simulation

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
            _unacknowledgedTicks = new List<MovementTick>();
            _input = new ClientInput();
            _moveAction = _input.Player.Move;
            _moveAction.Enable();
            _lookAction = _input.Player.Look;
            _lookAction.Enable();

            _lookAction.performed += PlayerLook;
            _moveAction.canceled += PlayerMoveStop;
            _moveAction.performed += PlayerMove;
        }

        private void Start()
        {
            if (Owner.IsLocal)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if(!PlayerModel)
            {
                return;
            }

            _animator = PlayerModel.GetComponent<Animator>();
        }

        private void OnEnable()
        {
            EventManager.MovementTickResultReceived += EventManager_MovementTickResultReceived;
        }

        private void OnDisable()
        {
            EventManager.MovementTickResultReceived -= EventManager_MovementTickResultReceived;
        }

        private void Update()
        {
            if (!Owner)
            {
                return;
            }

            if(Owner.IsLocal)
            {
                return;
            }

            if(!_hasTargetPosition)
            {
                return;
            }

            if(Vector3.Distance(Owner.transform.position, _targetPosition) <= Statics.MinPosDelta)
            {
                Owner.transform.position = _targetPosition;
                _hasTargetPosition = false;
                return;
            }

            Owner.transform.position = Vector3.Lerp(Owner.transform.position, _targetPosition, Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if(!Owner)
            {
                return;
            }

            _posStartTick = Owner.transform.position;
            _yawPitchStartTick = new Vector2(_yaw, _pitch);
            if (PlayerManager.Instance.IsLocal(Owner.PlayerId))
            {
                LookAround(Time.deltaTime);
                Move(Time.deltaTime);
                SendNewMovementTick();
            }
        }

        private void LookAround(float deltaTime) 
        {
            if(new Vector2(_mouseDeltaX, _mouseDeltaY).IsNearlyZero())
            {
                return;
            }

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

        private void Move(float deltaTime)
        {
            if (!PlayerCam)
            {
                return;
            }

            if(_lastMoveNormalized.IsNearlyZero())
            {
                return;
            }

            Vector3 moveVector = (ModelParent.transform.forward * _lastMoveNormalized.y + PlayerCam.transform.right * _lastMoveNormalized.x);

            Vector3 targetPosition = Owner.transform.position + moveVector * movementSpeed * deltaTime;

            Array.Clear(collisions, 0, collisions.Length);

            int collisionNum = Physics.OverlapCapsuleNonAlloc(targetPosition, targetPosition + Vector3.up * _capsuleCollider.bounds.extents.y, 0.5f, collisions, playerLayer);
            
            if(collisionNum != 0)
            {
                foreach(Collider collision in collisions)
                {
                    //collision is invalid or we collided with ourselves
                    if (!collision || collision.transform.root == transform.root)
                    {
                        continue;
                    }

                    //collision with non-player somehow
                    if(!collision.TryGetComponent<PlayerController>(out PlayerController playerController))
                    {
                        continue;
                    }

                    return;
                }
            }

            Owner.transform.position = targetPosition;
        }

        private void SendNewMovementTick()
        {
            var movementTick = new MovementTick()
            {
                Tick = NetworkManager.Instance.Tick,
                Input = ConvertInputToInt(),
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

        private int ConvertInputToInt()
        {
            int returnInput = 0;
            if(_lastMoveNormalized.x > 0)
            {
                returnInput |= 1 << 0;
            }
            else if(_lastMoveNormalized.x < 0)
            {
                returnInput |= 1 << 1;
            }

            if(_lastMoveNormalized.y > 0)
            {
                returnInput |= 1 << 2;
            }
            else if(_lastMoveNormalized.y < 0)
            {
                returnInput |= 1 << 3;
            }

            return returnInput;
        }
        private Vector2 GetVectorFromInput(int input)
        {
            Vector2 result = Vector2.zero;

            if ((1 << 0 & input) != 0)
            {
                result.x = 1;
            }
            else if ((1 << 1 & input) != 0)
            {
                result.x = -1;
            }

            if ((1 << 2 & input) != 0)
            {
                result.y = 1;
            }
            else if ((1 << 3 & input) != 0)
            {
                result.y = -1;
            }

            return result;
        }

        private void PlayerLook(InputAction.CallbackContext callbackContext)
        {
            var delta = callbackContext.ReadValue<Vector2>();
            _mouseDeltaX = delta.x;
            _mouseDeltaY = delta.y;
        }

        private void PlayerMove(InputAction.CallbackContext callbackContext)
        {
            var moveInput = callbackContext.ReadValue<Vector2>();
            _lastMoveNormalized = moveInput;
        }

        private void PlayerMoveStop(InputAction.CallbackContext callbackContext)
        {
            _lastMoveNormalized = Vector2.zero;
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
                CheckMoveDelta(tickResult);
            }
            else
            {
                //if interp didnt finish, snap to position
                Owner.transform.position = tickResult.ActualStartPosition;
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

        private void CheckMoveDelta(MovementTickResult tickResult)
        {
            if ((tickResult.ActualEndPosition - tickResult.PassedEndPosition).magnitude > Statics.MaxPositionDelta || (tickResult.ActualStartPosition - tickResult.StartPosition).magnitude > Statics.MaxPositionDelta)
            {
                Debug.LogWarning($"Server and Client result differed to much - recalculating.");
                RecalculateMoveSinceTick(tickResult);
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
                _unacknowledgedTicks[index] = tick;
            }

            SetPlayerRotation(new Vector2(lastTickStartYaw, lastTickStartPitch), lastTickEndYaw, lastTickEndPitch, lastTickDeltaTime);
        }

        private void RecalculateMoveSinceTick(MovementTickResult tickResult)
        {
            Vector3 lastTickEndPosition = tickResult.ActualEndPosition;

            for (var index = 0; index < _unacknowledgedTicks.Count; ++index)
            {
                MovementTick tick = _unacknowledgedTicks[index];
                tick.StartPosition = lastTickEndPosition;

                Vector3 moveDirection = (tick.EndPosition - tick.StartPosition).normalized;
                Vector2 inputThisTick = GetVectorFromInput(tick.Input);

                Quaternion modelParentQuaternion = Quaternion.Euler(new Vector3(ModelParent.transform.rotation.x, tick.Yaw, ModelParent.transform.rotation.z));

                Quaternion camQuaternion = Quaternion.Euler(new Vector3(tick.Pitch, tick.Yaw, 0));

                Vector3 modelForward = modelParentQuaternion * Vector3.forward;
                Vector3 camRight = camQuaternion * Vector3.right;

                Vector3 moveVector = (modelForward * inputThisTick.y + camRight * inputThisTick.x);

                lastTickEndPosition = Vector3.Lerp(lastTickEndPosition, lastTickEndPosition + moveVector * movementSpeed, tick.DeltaTime);
                tick.EndPosition = lastTickEndPosition;
                _unacknowledgedTicks[index] = tick;

            }

            Debug.LogError("NEW POS: " + lastTickEndPosition);
            Owner.transform.position = lastTickEndPosition;
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

            _targetPosition = tickResult.ActualEndPosition;

            //Todo: Check Slowwalk
            Vector3 moveDirection = Vector3.Normalize(_targetPosition - Owner.transform.position);

            float forwardDirectionDotProduct = Vector3.Dot(ModelParent.transform.forward, moveDirection);

            _targetVelocity = forwardDirectionDotProduct > 0 ? 1f : -1f;

            if(Mathf.Approximately(forwardDirectionDotProduct, 0))
            {
                _targetVelocity = 0;
            }

            float currentVelocityValue = _animator.GetFloat(Statics.VelocityAnimationParamter);

            if (Mathf.Approximately(_targetVelocity, currentVelocityValue))
            {
                _reachedTargetVelocity = true;
            }
            else
            {
                _reachedTargetVelocity = false;
            }

            if (!_reachedTargetVelocity)
            {
                float newVelocityValue = 0;
                if (_targetVelocity < 0)
                {
                    newVelocityValue = GetMaxNewValue(currentVelocityValue, tickResult.DeltaTime, Mathf.Sign(_targetVelocity), velocityRampUpDownSpeed, _targetVelocity);
                }
                else if(_targetVelocity > 0)
                {
                    newVelocityValue = GetMinNewValue(currentVelocityValue, tickResult.DeltaTime, Mathf.Sign(_targetVelocity), velocityRampUpDownSpeed, _targetVelocity);
                }
                else if(_targetVelocity == 0)
                {
                    newVelocityValue = Mathf.Sign(currentVelocityValue) > 0 ? GetMaxNewValue(currentVelocityValue, tickResult.DeltaTime, -1, velocityRampUpDownSpeed, _targetVelocity)
                        : GetMinNewValue(currentVelocityValue, tickResult.DeltaTime, 1, velocityRampUpDownSpeed, _targetVelocity);
                }

                _animator.SetFloat(Statics.VelocityAnimationParamter, newVelocityValue);
            }

            float currentDirectionValue = _animator.GetFloat(Statics.DirectionAnimationParamter);

            Vector2 inputTick = GetVectorFromInput(tickResult.Input);

            _targetDirection = inputTick.x;

            if (Mathf.Approximately(_targetDirection, currentDirectionValue))
            {
                _reachedTargetDirection = true;
            }
            else
            {
                _reachedTargetDirection = false;
            }

            if (inputTick.y < 0)
            {
                _targetDirection *= -1;
            }

            if (!_reachedTargetDirection)
            {
                float newDirectionValue = 0;
                if (_targetDirection < 0)
                {
                    newDirectionValue = GetMaxNewValue(currentDirectionValue, tickResult.DeltaTime, Mathf.Sign(_targetDirection), directionRampUpDownSpeed, _targetDirection);
                }
                else if (_targetDirection > 0)
                {
                    newDirectionValue = GetMinNewValue(currentDirectionValue, tickResult.DeltaTime, Mathf.Sign(_targetDirection), directionRampUpDownSpeed, _targetDirection);
                }
                else if (_targetDirection == 0)
                {
                    newDirectionValue = Mathf.Sign(currentDirectionValue) > 0 ? GetMaxNewValue(currentDirectionValue, tickResult.DeltaTime, -1, directionRampUpDownSpeed, _targetDirection)
                        : GetMinNewValue(currentDirectionValue, tickResult.DeltaTime, 1, directionRampUpDownSpeed, _targetDirection);
                }

                _animator.SetFloat(Statics.DirectionAnimationParamter, newDirectionValue);
            }

            _hasTargetPosition = true;

            if (Vector3.Distance(Owner.transform.position, _targetPosition) <= Statics.MinPosDelta)
            {
                _reachedTargetVelocity = false;
                _targetVelocity = 0;
                _reachedTargetDirection = false;
                _targetDirection = 0;
            }
        }

        #region Shared
        private float GetMaxNewValue(float currentValue, float deltaTime, float sign, float rampUpSpeed, float target)
        {
            return Mathf.Max((currentValue + (sign * deltaTime * rampUpSpeed)), target);
        }

        private float GetMinNewValue(float currentValue, float deltaTime, float sign, float rampUpSpeed, float target)
        {
            return Mathf.Min((currentValue + (sign * deltaTime * rampUpSpeed)), target);
        }
        #endregion Velocity
    }
}
