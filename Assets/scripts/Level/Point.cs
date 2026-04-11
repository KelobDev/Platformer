using UnityEngine;

public class Point : MonoBehaviour
{

    private void Awake()
    {
        if (LevelManager.instance != null)
            LevelManager.instance.maxPoints += 1;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            LevelManager.instance.Addpoints(1);
            Destroy(this.gameObject);
        }
    }
}
