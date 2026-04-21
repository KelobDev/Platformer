using UnityEngine;

public class Secret : MonoBehaviour
{
    [SerializeField, Range(1,3)] private int number = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            LevelManager.instance.UnlockSecret(number);
            Destroy(this.gameObject);
        }
    }
}
