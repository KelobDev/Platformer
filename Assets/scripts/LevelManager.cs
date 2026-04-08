using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Rating")]
    [SerializeField] private GameObject finishPanel;
    private void Update()
    {
        if(hasTimeStar)
            timer += Time.deltaTime;
    }
    private void Awake()
    {
        instance = this;
        finishPanel.SetActive(false);
    }
    public void Addpoints(int amount)
    {
        playerPoints += amount;
    }
    public void FinishLevel()
    {
        finishPanel.SetActive(true);
        float pointsPercent = playerPoints / maxPoints * 100;
        if (pointsPercent > 80)
        {
            Debug.Log("3 gwiazdki ");
        }
        else if (pointsPercent > 50)
        {
            Debug.Log("2 gwiazdki ");
        }
        else
        {
            Debug.Log("1 gwiazdka ");
        }
    }
    public void GoToHub()
    {
        SceneManager.LoadScene(0);
    }

}
