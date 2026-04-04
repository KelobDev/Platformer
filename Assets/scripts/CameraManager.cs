using UnityEngine;
using Cinemachine;
using System.Collections;
public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [Header("Controls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float fallPanAmount = 0.25f;
    [SerializeField] private float fallYPanTime = 0.35f;
    public float fallSpeedYDampingChangeThreshold = -15f;

    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }

    private Coroutine lerpYPanCoroutine;
    [SerializeField] private CinemachineVirtualCamera cam;

    private float normYPanAmount;
    private CinemachineFramingTransposer framingTransposer;
    private void Awake()
    {
        if (instance == null) 
            instance = this;
        
        framingTransposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
        normYPanAmount = framingTransposer.m_YDamping;
    }

    public void LerpYDamping(bool isPlayerFalling)
    {
        if (lerpYPanCoroutine != null)
            StopCoroutine(lerpYPanCoroutine);

        lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }
    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        IsLerpingYDamping = true;

        float startDampAmount = framingTransposer.m_YDamping;
        float endDampAmount = 0f;

        if (isPlayerFalling)
        {
            endDampAmount = fallPanAmount;
            LerpedFromPlayerFalling = true;
        }
        else
        {
            endDampAmount = normYPanAmount;
        }
        float elapsedTime = 0f;
        while (elapsedTime < fallYPanTime)
        {
            elapsedTime += Time.deltaTime;

            float lerpPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, (elapsedTime / fallYPanTime));
            framingTransposer.m_YDamping = lerpPanAmount;
            yield return null;
        }
        IsLerpingYDamping = false;
    }
}
