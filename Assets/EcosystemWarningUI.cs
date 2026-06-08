using System.Collections;
using TMPro;
using UnityEngine;

public class EcosystemWarningUI : MonoBehaviour
{
    public static EcosystemWarningUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject warningRoot;
    public TextMeshProUGUI warningText;

    [Header("Timing")]
    public float defaultShowTime = 4f;
    public float typeSpeed = 0.03f;

    [Header("Behaviour")]
    public bool useTypewriterEffect = true;
    public bool hideOnStart = true;

    private Coroutine warningRoutine;

    private void Awake()
    {
        Instance = this;

        if (warningRoot == null)
        {
            warningRoot = gameObject;
        }

        if (warningText == null)
        {
            warningText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void Start()
    {
        if (hideOnStart && warningRoot != null)
        {
            warningRoot.SetActive(false);
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ShowWarning("Warning! Stegasaurus is Endangered!", 4f);
        }
    }
    public void ShowWarning(string message)
    {
        ShowWarning(message, defaultShowTime);
    }

    public void ShowWarning(string message, float showTime)
    {
        if (warningRoot == null || warningText == null)
        {
            Debug.LogWarning("EcosystemWarningUI is missing warningRoot or warningText.");
            return;
        }

        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
        }

        warningRoutine = StartCoroutine(ShowWarningRoutine(message, showTime));
    }

    private IEnumerator ShowWarningRoutine(string message, float showTime)
    {
        warningRoot.SetActive(true);
        warningText.text = "";

        if (useTypewriterEffect)
        {
            for (int i = 0; i < message.Length; i++)
            {
                warningText.text += message[i];
                yield return new WaitForSeconds(typeSpeed);
            }
        }
        else
        {
            warningText.text = message;
        }

        yield return new WaitForSeconds(showTime);

        warningText.text = "";
        warningRoot.SetActive(false);
        warningRoutine = null;
    }
}