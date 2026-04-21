namespace JetFlow.Testing.Helpers;

internal static class CollectionsHelper
{
    public static bool CollectionsMatchIgnoreOrder<T>(IEnumerable<T> first, IEnumerable<T> second)
    {
        if (first == null || second == null)
            return false;
        var firstList = first.ToList();
        var secondList = second.ToList();
        if (firstList.Count != secondList.Count)
            return false;
        var firstSet = new HashSet<T>(firstList);
        var secondSet = new HashSet<T>(secondList);
        return firstSet.SetEquals(secondSet);
    }
}
