
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StoryScene : MonoBehaviour
{
    public bool final;

    [SerializeField]
    private TextMeshPro text;

    public List<string> lines = new List<string>();

    private void Start()
    {
        Time.timeScale = 1f;
        text.gameObject.SetActive(false);
        StartCoroutine(StorySequence());
    }

    private IEnumerator StorySequence()
    {

        yield return new WaitForSeconds(2f);

        foreach (var line in lines)
        {
            text.gameObject.SetActive(true);
            text.text = line;

            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => Input.GetMouseButton(0));

            text.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
        }

        if (!final)
        {
            yield return new WaitForSeconds(0.25f);

            SceneTransition.instance.Transition(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
            yield return new WaitUntil(() => Input.anyKey);
            Application.Quit();
        }
    }
}
