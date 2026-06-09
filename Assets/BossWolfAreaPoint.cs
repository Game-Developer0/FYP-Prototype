using UnityEngine;

public class BossWolfAreaPoint : MonoBehaviour
{
    [Header("Area Settings")]
    public string targetID = "BossWolf";

    [Tooltip("How many Boss Wolves must be killed in this area before arrow stops pointing here.")]
    public int killsRequiredInThisArea = 2;

    private int killsInThisArea = 0;

    public bool IsCleared()
    {
        return killsInThisArea >= killsRequiredInThisArea;
    }

    public void RegisterKill()
    {
        killsInThisArea++;

        if (killsInThisArea > killsRequiredInThisArea)
        {
            killsInThisArea = killsRequiredInThisArea;
        }

        Debug.Log(gameObject.name + " Boss Wolf area progress: " + killsInThisArea + " / " + killsRequiredInThisArea);
    }

    public void ResetArea()
    {
        killsInThisArea = 0;
    }
}