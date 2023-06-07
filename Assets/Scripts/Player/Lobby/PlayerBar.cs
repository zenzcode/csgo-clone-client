using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerBar : MonoBehaviour
{
    public ushort Owner;
    public GameObject LeaderIcon;
    public TMP_Text UsernameText;
    public TMP_Text PingText;
    public Button KickButton;
}
