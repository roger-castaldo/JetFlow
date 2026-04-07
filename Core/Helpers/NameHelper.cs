namespace JetFlow.Helpers;

internal static class NameHelper
{
    public static string GetWorkflowName<TWorkflow>()
        => CleanName(typeof(TWorkflow).Name);

    public static string GetActivityName<TActivity>()
        => CleanName(typeof(TActivity).Name);

    private static string CleanName(string value)
        => new string(value.Where(c => char.IsLetterOrDigit(c)).ToArray());
}
