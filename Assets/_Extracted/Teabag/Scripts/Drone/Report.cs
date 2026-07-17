using Teabag.Authentication;
using UnityEngine;
using TMPro;
using Teabag.Core;

public class Report : MonoBehaviour
{
    PlayerLine line;
    public TextMeshPro text;
    public GameObject buttons;
    public ReportState state;

    private void Awake()
    {
        line = GetComponentInParent<PlayerLine>();
    }

    private void Update()
    {
        if (ModerationUtils.ReportedPlayers.Contains(line.gorilla.id))
            state = ReportState.Reported;

        switch (state)
        {
            case ReportState.None:
                buttons.SetActive(true);
                break;
            case ReportState.Reporting:
                text.text = "REPORTING";
                buttons.SetActive(false);
                break;
            case ReportState.Reported:
                text.text = "REPORTED!";
                buttons.SetActive(false);
                break;
        }
        text.gameObject.SetActive(!buttons.activeSelf);
    }

    public enum ReportState
    {
        None,
        Reporting,
        Reported
    }
}
