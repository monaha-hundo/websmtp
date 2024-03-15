namespace Tests;

static public class Faker
{
    public static T PickRandom<T>(this IList<T> list)
    {
        var rI = Random.Shared.Next(0, list.Count - 1);
        return list[rI];
    }

}
