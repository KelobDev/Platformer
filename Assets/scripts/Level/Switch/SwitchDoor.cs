using UnityEngine;

public class SwitchDoor : MonoBehaviour
{
    [SerializeField] private Collider2D collider;
    private SpriteRenderer sr;
    private void Awake()
    {
        sr = this.gameObject.GetComponent<SpriteRenderer>();
    }
    private void Update()
    {
        if (collider.enabled)
        {
            sr.color = Color.red;
        }
        else
        {
            sr.color = Color.green;
        }
    }
    public void ChangeDoorState(bool switchState)
    {
       collider.enabled = !switchState;
    }
}
