using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [Header("Flip rotation stats")]
    [SerializeField] private float flipRotationTime = 0.5f;
    private Coroutine turnCoroutine;

    private PlayerController player;

    private bool isFacingRight;
    private void Awake()
    {
        player = playerTransform.gameObject.GetComponent<PlayerController>();
        isFacingRight = player.face == 1;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = playerTransform.position;
    }
    public void CallTurn()
    {
        turnCoroutine = StartCoroutine(Flip());
    }

    private IEnumerator Flip()
    {
        float startRotation = transform.localEulerAngles.y;
        float endRotation = DetermineEndRotation();
        float yRotation = 0f;
        float elapsedTime = 0f;
        while (elapsedTime < flipRotationTime) { 
        
            elapsedTime += Time.deltaTime;
            yRotation = Mathf.Lerp(startRotation, endRotation, (elapsedTime/flipRotationTime));
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
        yield return null;
    }

    private float DetermineEndRotation()
    {
        isFacingRight = !isFacingRight;
        if (isFacingRight)
            return 180f;
        else return 0f;
    }
}
