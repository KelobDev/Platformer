using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Settings")]
    public int levelID;
    public bool isTimeTrial;

    //Rating system
    [Header("Points & Time")]
    public int maxPoints;
    [SerializeField, Range(0f, 1f)] private float goldPointsPercent = 0.9f;
    [SerializeField, Range(0f, 1f)] private float silverPointsPercent = 0.6f;
    [SerializeField] private int playerPoints;
    [SerializeField] private bool secret1Found, secret2Found, secret3Found;
    [SerializeField] private float timer;
    [SerializeField] private float goldTime;
    [SerializeField] private float silverTime;
    [SerializeField] private float bonzeTime;

    [Header("UI")]
    [SerializeField] private GameObject finishPanel;
    [SerializeField] private GameObject[] stars;
    [SerializeField] private TMP_Text PointsText;
    private void Update()
    {
       timer += Time.deltaTime;
    }
    private void Awake()
    {
        instance = this;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
                stars[i].SetActive(false);
        }
        finishPanel.SetActive(false);
    }
    public void Addpoints(int amount)
    {
        playerPoints += amount;
    }
    public void UnlockSecret(int numb)
    {
        switch (numb)
        {
            case 1:
                secret1Found = true;
                break;
            case 2: 
                secret2Found = true;
                break;
                case 3:
                secret3Found = true;
                break;
        }
    }
    public void FinishLevel()
    {
      finishPanel.SetActive(true);
        bool[] currentStars = new bool[5];
        //give stars for timeo or secrets found
        if (isTimeTrial)
        {
            if(timer <= goldTime) currentStars[0] = true;
            if(timer <= silverTime) currentStars[1] = true;
            if(timer <= bonzeTime) currentStars[2] = true;
        }
        else
        {
            if (secret1Found) currentStars[0] = true;
            if (secret2Found) currentStars[1] = true;
            if (secret3Found) currentStars[2] = true;
        }
        //give stars for collected points
        float pointsPercent = (float)playerPoints / maxPoints;
        if (pointsPercent >= goldPointsPercent) currentStars[3] = true;
        if (pointsPercent >= silverPointsPercent) currentStars[4] = true;

        
        PointsText.text ="Points: "+playerPoints.ToString();
        //display stars
        for (int i = 0; i < 5; i++)
        {
            stars[i].SetActive(currentStars[i]);
        }
        SaveProgress(currentStars);
    }
    private void SaveProgress(bool[] earnedNow) {

        string path = Application.persistentDataPath + "/level_" + levelID + ".json";
        string pathH = Application.persistentDataPath + "/Hub.json";

        LevelData data = new LevelData();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<LevelData>(json);
        }

        for (int i = 0; i < 5; i++)
        {
            if (earnedNow[i]) data.starsEarned[i] = true;
        }

        
        if (playerPoints > data.bestCoins)
        {

            HubData dataH = new HubData();
            if (File.Exists(pathH))
            {
                string jsonH = File.ReadAllText(pathH);
                dataH = JsonUtility.FromJson<HubData>(jsonH);
            }

            
            dataH.Points += playerPoints-data.bestCoins;    
            data.bestCoins = playerPoints;

            string hubJson = JsonUtility.ToJson(dataH);
            File.WriteAllText(pathH, hubJson);

        }

        string newJson = JsonUtility.ToJson(data);
        File.WriteAllText(path, newJson);

    }
    public void GoToHub()
    {
        SceneManager.LoadScene(0);
    }

}
