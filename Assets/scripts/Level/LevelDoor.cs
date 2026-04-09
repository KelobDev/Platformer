using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelDoor : MonoBehaviour
{
    public int sceneNum = 0;

    public void Loadscene()
    {
        SceneManager.LoadScene(sceneNum);
    }
}
