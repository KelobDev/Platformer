using UnityEngine;

public class Point : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            LevelManager.instance.Addpoints(1);
            Destroy(this.gameObject);
        }
    }
}
