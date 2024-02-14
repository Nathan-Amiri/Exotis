using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Console : MonoBehaviour
{
    [SerializeField] private GameObject console;

    [SerializeField] private TMP_Text middleConsoleText;
    [SerializeField] private TMP_Text allyConsoleText;
    [SerializeField] private TMP_Text enemyConsoleText;

    public void WriteConsoleMessage(string message)
    {
        console.SetActive(message != string.Empty);
        middleConsoleText.text = message;
    }
}