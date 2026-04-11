using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
enum DoorType
{
    LEVEL,
    HUB
}
public class LevelDoor : MonoBehaviour
{
    public int sceneNum = 0;
    [Header("Door type")]
    [SerializeField] private DoorType doorType;
    [Header("Rating system")]
    [SerializeField] private int levelID; 
    [SerializeField] private GameObject[] stars;
    private void Awake()
    {
        for (int i = 0; i < 5; i++)
            stars[i].SetActive(false);
        if(doorType == DoorType.LEVEL)
            LoadData();
    }
    private void LoadData()
    {
        string path = Application.persistentDataPath + "/level_" + levelID + ".json";
        LevelData data = new LevelData();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<LevelData>(json);
        }
        for (int i = 0; i < 5; i++)
        {
            if (data.starsEarned[i]) stars[i].SetActive(true);
        }
        
    }
    public void Loadscene()
    {
        SceneManager.LoadScene(sceneNum);
    }
}
