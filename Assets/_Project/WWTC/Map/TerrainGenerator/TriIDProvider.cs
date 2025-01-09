using UnityEngine;

public static class TriIDProvider
{
    private static int s_nextID = 0;

    public static int GetNewID()
    {
        return s_nextID++;
    }

    public static void Reset()
    {
        s_nextID = 0;
    }
}