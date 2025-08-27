using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PearlManager : MonoBehaviour
{
    public PearlManager Instance;

    private List<GameObject> pearls;
    private Queue<GameObject> pearlsQueue;

    // Start is called before the first frame update
    void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        pearls = new List<GameObject>(transform.childCount);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform c = transform.GetChild(i);
            pearls.Add(c.gameObject);
        }

        pearlsQueue = new Queue<GameObject>(pearls);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ActivatePearl()
    {
        if (pearlsQueue.Count < 0)
            return;

        GameObject pearl = pearlsQueue.Dequeue();
        pearl.transform.GetChild(0).gameObject.SetActive(true); //particle
        pearl.transform.GetChild(1).gameObject.SetActive(true); //Ball
    }
}
