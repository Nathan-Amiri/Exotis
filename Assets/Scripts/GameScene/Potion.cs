using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Potion : MonoBehaviour
{
    [SerializeField] private Button button;

    [SerializeField] private DelegationCore delegationCore;

    public void OnClick()
    {
        button.interactable = false;

        delegationCore.SelectPotion();
    }
}