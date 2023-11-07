using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Elemental : NetworkBehaviour
{
    //Inherits from NetworkBehaviour to support netcode serialization
    //(this class does not contain networking logic)

    //assigned in prefab:
    [SerializeField] private List<Image> colorOutlines;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image speedColorBackground;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image icon; //set a to 150 when targeting
    [SerializeField] private GameObject targetButton;
}