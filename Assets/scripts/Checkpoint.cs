using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public bool taken = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !taken)
        {
            collision.gameObject.GetComponent<PlayerController>().SetCheckpoint(this.gameObject);
        }
    }
}
