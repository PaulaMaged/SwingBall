using System.Collections;
using UnityEngine;

public class DebugTransform : MonoBehaviour
{
    private Coroutine coroutine;

    public bool coroutineActivationState = false;
    public float timeBetweenDebugLogs = 2;
    // Update is called once per frame
    void Start()
    {
    }

    private void Update()
    {
        if (!coroutineActivationState)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutineActivationState = false;
            }
        }
        else
        {
            if (coroutine == null)
            {
                coroutine = StartCoroutine(printStatus());
                coroutineActivationState = true;
            }
        }
    }

    public IEnumerator printStatus()
    {
        while (true)
        {
            Debug.Log(transform.rotation.eulerAngles);
            yield return new WaitForSeconds(timeBetweenDebugLogs);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
