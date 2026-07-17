using Teabag.Authentication;
using Teabag.UI;

public class ReportButton : GorillaButton
{
    public Report AttachedReportComponent => GetComponentInParent<Report>();

    public PlayerLine line;
    public string reason;

    public override async void OnPress()
    {
        SetMaterial(true);
        AttachedReportComponent.state = Report.ReportState.Reporting;
        if (!await ModerationUtils.ReportPlayerAsync(line.gorilla.id, reason))
            AttachedReportComponent.state = Report.ReportState.None;
        SetMaterial(false);
    }
}
