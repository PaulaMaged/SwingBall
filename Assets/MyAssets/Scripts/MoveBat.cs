using System.Collections;
using UnityEngine;

public class MoveBat : MonoBehaviour
{
    Vector3 initPosition;
    private Coroutine coroutine;
    public bool isMoving = false;
    public float offset = 0.25f;
    public float deltaMovement = 0.001f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initPosition = transform.position;
        coroutine = StartCoroutine(Sway());
    }

    private void Update()
    {
        if (isMoving)
        {
            if (coroutine != null) return;

            transform.position = initPosition;
            coroutine = StartCoroutine(Sway());
        }
        else
        {
            if (coroutine == null) return;
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    private IEnumerator Sway()
    {
        float delta = 0;
        while (true)
        {
            if (Mathf.Abs(delta) > offset)
            {
                deltaMovement *= -1;
            }

            transform.position += deltaMovement * Vector3.right;
            delta += deltaMovement;

            yield return null;
        }
    }
}
