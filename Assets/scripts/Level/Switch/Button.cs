using UnityEngine;

public class Button : MonoBehaviour
{
    [SerializeField] private SwitchDoor door;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        door.ChangeDoorState(true);
        //change animation
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        door.ChangeDoorState(false);
        //change animation
    }
}
