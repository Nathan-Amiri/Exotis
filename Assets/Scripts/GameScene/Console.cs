using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;

public class Console : MonoBehaviour
{
    [SerializeField] private GameObject console;
    [SerializeField] private DelegationCore delegationCore;
    [SerializeField] private ExecutionCore executionCore;

    [SerializeField] private TMP_Text middleConsoleText;
    [SerializeField] private TMP_Text allyConsoleText;
    [SerializeField] private TMP_Text enemyConsoleText;


    private readonly List<(string, string)> messageQueue = new();
    private bool outputDelegationCore;
    public void WriteSingleConsoleMessage(string message, bool isDelegationCore, bool queuedMessage = false)
    {
        outputDelegationCore = isDelegationCore;

        if (queuedMessage)
            messageQueue.Add((message, string.Empty));
        else
        {
            console.SetActive(true);
            middleConsoleText.text = message;
        }
    }
    public void WriteMultipleConsoleMessage(string allyMessage, string enemyMessage, bool queuedMessage = false)
    {
        // Multiple messages are never written by DelegationCore
        outputDelegationCore = false;

        if (queuedMessage)
            messageQueue.Add((allyMessage, enemyMessage));
        else
        {
            console.SetActive(true);
            allyConsoleText.text = allyMessage;
            enemyConsoleText.text = enemyMessage;
        }
    }
    public void HideConsole()
    {
        console.SetActive(false);
        middleConsoleText.text = string.Empty;
    }

    public void SelectConsoleButton()
    {
        if (messageQueue.Count > 0)
        {
            if (messageQueue[0].Item2 != string.Empty)
                middleConsoleText.text = messageQueue[0].Item1;
            else
            {
                allyConsoleText.text = messageQueue[0].Item1;
                enemyConsoleText.text = messageQueue[0].Item2;
            }

            messageQueue.RemoveAt(0);
            return;
        }

        if (outputDelegationCore)
            delegationCore.SelectConsoleButton();
        else
            executionCore.SelectConsoleButton();
    }
}