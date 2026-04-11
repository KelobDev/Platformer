using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
public class HubManager : MonoBehaviour
{
    //data
    private int points;
    [Header("UI")]
    [SerializeField] private TMP_Text pointsText;

    private void Awake()
    {
        LoadHubData();
    }
    private void LoadHubData()
    {
        string path = Application.persistentDataPath + "/Hub.json";
        HubData data = new HubData();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<HubData>(json);
        }
        points = data.Points;
        pointsText.text = "Points: " + points.ToString();
    }
}
