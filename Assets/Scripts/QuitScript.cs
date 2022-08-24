using System.Collections;
using UnityEngine;

public class QuitScript : MonoBehaviour {

    public AudioClip clip;

    public void ExitGame() {
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        StartCoroutine(QuitAfterDelay(clip.length));
    }

    IEnumerator QuitAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        Application.Quit();
    }
}
