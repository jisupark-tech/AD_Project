using UnityEngine;

public class Effect : MonoBehaviour
{
    public float DestoryTime = 0.5f;
    private float curtime = 0;
    private void OnEnable()
    {
        curtime = 0;
    }
    void Update()
    {
        if(this.gameObject.activeSelf)
        {
            curtime += Time.deltaTime;

            if (curtime >= DestoryTime)
            {
                this.gameObject.SetActive(false);
            }
        }
    }
}
