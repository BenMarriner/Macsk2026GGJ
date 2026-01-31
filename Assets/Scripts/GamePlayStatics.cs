using UnityEngine;

public static class GamePlayStatics
{
    public static GameObject GetPlayer()
    {
        return GameObject.FindGameObjectWithTag("Player");
    }
}
