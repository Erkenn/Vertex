using System.Reflection;

public static class UIManagerExtensions
{
    public static bool HasMethod(this UIManager uiManager, string methodName)
    {
        var type = typeof(UIManager);
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        return method != null;
    }
}