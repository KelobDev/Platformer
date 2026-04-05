using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    //Rating system
    [Header("Points")]
    [SerializeField] private int maxPoints;
    [SerializeField] private int playerPoints;

    [Header("Time")]
    [SerializeField] private float timer;
    [SerializeField] private float timeForStar;
    [SerializeField] private bool hasTimeStar;

    private void Update()
    {
        if(hasTimeStar)
            timer += Time.deltaTime;
    }
    private void Awake()
    {
        instance = this;
    }
    public void Addpoints(int amount)
    {
        playerPoints += amount;
    }

}
