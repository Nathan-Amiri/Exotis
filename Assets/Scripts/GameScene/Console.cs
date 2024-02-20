using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Console : MonoBehaviour
{
    [SerializeField] private GameObject console;
    [SerializeField] private DelegationCore delegationCore;
    [SerializeField] private ExecutionCore executionCore;

    [SerializeField] private TMP_Text middleConsoleText;
    [SerializeField] private TMP_Text allyConsoleText;
    [SerializeField] private TMP_Text enemyConsoleText;


    private List<string> messageQueue = new();
    private bool outputDelegationCore;
    public void WriteConsoleMessage(string message)
    {
        //if (queuedMessage)
        //    messageQueue.Add(message);
        //else
        //{
        //    console.SetActive(message != string.Empty);
        //    middleConsoleText.text = message;
        //}
    }

    public void SelectConsoleButton()
    {
        if (messageQueue.Count > 0)
        {
            middleConsoleText.text = messageQueue[0];
            messageQueue.RemoveAt(0);
            return;
        }

        if (outputDelegationCore)
            delegationCore.SelectConsoleButton();
        else
            executionCore.SelectConsoleButton();
    }
}