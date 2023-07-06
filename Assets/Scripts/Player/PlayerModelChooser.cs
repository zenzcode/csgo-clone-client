using System.Collections;
using System.Collections.Generic;
using Player.Game;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerModelChooser : MonoBehaviour
    {
        [SerializeField] private GameObject firstPersonParent;
        [SerializeField] private GameObject thirdPersonParent;

        [SerializeField] private GameObject firstPersonModel;
        [SerializeField] private GameObject thirdPersonModel;
        private Player _player;
        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _player = GetComponentInParent<Player>();
            _playerController.Owner = _player;
            
            firstPersonParent.SetActive(false);
            thirdPersonParent.SetActive(false);
            if (_player.IsLocal)
            {
                firstPersonParent.SetActive(true);
                _playerController.PlayerCam = firstPersonParent.GetComponentInChildren<Camera>();
                _playerController.PlayerModel = firstPersonModel;
                _playerController.ModelParent = firstPersonParent;
            }
            else
            {
                _playerController.PlayerCam = thirdPersonModel.GetComponentInChildren<Camera>();
                _playerController.PlayerModel = thirdPersonModel;
                _playerController.ModelParent = thirdPersonParent;
                thirdPersonParent.SetActive(true);
            }
        }

    }

}
