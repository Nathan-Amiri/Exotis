using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonUp : MonoBehaviour, IPointerUpHandler
{
    [SerializeField]
    private Button.ButtonClickedEvent onClick = new();

    private Button button;
    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable)
            onClick?.Invoke();
    }
}