using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerModelChooser : MonoBehaviour
    {
        [SerializeField] private GameObject _firstPerson;
        [SerializeField] private GameObject _thirdPerson;
        private Player _player;

        private void Awake()
        {
            _player = GetComponentInParent<Player>();
            _firstPerson.SetActive(false);
            _thirdPerson.SetActive(false);
            if (_player.IsLocal)
            {
                _firstPerson.SetActive(true);
            }
            else
            {
                _thirdPerson.SetActive(true);
            }
        }

    }

}
