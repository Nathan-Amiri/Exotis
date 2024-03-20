using TMPro;
using UnityEngine;

public class Console : MonoBehaviour
{
    [SerializeField] private GameObject console;

    [SerializeField] private TMP_Text middleConsoleText;
    [SerializeField] private TMP_Text allyConsoleText;
    [SerializeField] private TMP_Text enemyConsoleText;

    [SerializeField] private GameObject consoleButton;

    public delegate void OutputMethod();
    public OutputMethod CurrentOutputMethod { get; private set; }
    public void WriteConsoleMessage(string singleOrAllyMessage, string enemyMessage = null, OutputMethod outputMethod = null, bool enemyColor = false)
    {
        // Reset all text
        ResetConsole();

        console.SetActive(true);

        if (enemyMessage == null)
        {
            middleConsoleText.color = enemyColor ? StaticLibrary.gameColors["enemyText"] : StaticLibrary.gameColors["allyText"];
            middleConsoleText.text = singleOrAllyMessage;
        }
        else
        {
            allyConsoleText.text = singleOrAllyMessage;
            enemyConsoleText.text = enemyMessage;
        }

        if (outputMethod != null)
        {
            CurrentOutputMethod = outputMethod;
            consoleButton.SetActive(true);
        }
    }

    public void SelectConsoleButton()
    {
        ResetConsole();

        CurrentOutputMethod();
    }

    public void ResetConsole()
    {
        console.SetActive(false);
        consoleButton.SetActive(false);

        middleConsoleText.text = string.Empty;
        allyConsoleText.text = string.Empty;
        enemyConsoleText.text = string.Empty;
    }
}