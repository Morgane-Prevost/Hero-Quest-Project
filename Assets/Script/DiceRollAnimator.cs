using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceRollAnimator : MonoBehaviour
{
    [SerializeField] private RawImage image;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float frameInterval = 0.05f;
    [SerializeField] private float maxShakeAngle = 25f;

    private Coroutine running;

    public void Play(Texture[] randomFrames, Texture finalTexture, Action onDone = null)
    {
        if (!image.IsActive()) image.gameObject.SetActive(true);
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Roll(randomFrames, finalTexture, onDone));
        Debug.Log("Play appelé, frames: " + (randomFrames?.Length ?? -1) + ", final: " + finalTexture);
    }

    private IEnumerator Roll(Texture[] frames, Texture finalTexture, Action onDone)
    {
        float elapsed = 0f;
        float interval = frameInterval;

        while (elapsed < duration)
        {
            image.texture = frames[UnityEngine.Random.Range(0, frames.Length)];
            image.transform.localRotation =
                Quaternion.Euler(0, 0, UnityEngine.Random.Range(-maxShakeAngle, maxShakeAngle));

            yield return new WaitForSeconds(interval);
            elapsed += interval;
            interval *= 1.12f; // ralentit à chaque frame
        }

        image.texture = finalTexture;
        image.transform.localRotation = Quaternion.identity;

        // petit rebond final
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float scale = 1f + Mathf.Sin(t / 0.15f * Mathf.PI) * 0.2f;
            image.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        image.transform.localScale = Vector3.one;

        running = null;

        yield return new WaitForSeconds(0.5f);

        if (image.IsActive()) image.gameObject.SetActive(false);
        onDone?.Invoke();
    }
}