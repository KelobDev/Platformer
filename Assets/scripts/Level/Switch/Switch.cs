using UnityEngine;

public class Switch : MonoBehaviour
{
    private bool switchState = false;
    [SerializeField] private SwitchDoor door;
    public void changeState()
    {
        switchState = !switchState;
        door.ChangeDoorState(switchState);
    }
   
}
