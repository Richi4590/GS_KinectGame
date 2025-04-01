using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugViewer : MonoBehaviour
{
    public bool objectsInitiallyActive = false;
    // Start is called before the first frame update
    public List<GameObject> gameObjects;

    void Start()
    {
        gameObjects.ForEach(o => o.SetActive(objectsInitiallyActive));
    }

    private void Update()
    {
        HandleKeyboardInputs();
    }

    private void HandleKeyboardInputs()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (gameObjects.Count > 0)
            {
                gameObjects.ForEach(o => o.SetActive(true));
            }
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            if (gameObjects.Count > 0)
            {
                gameObjects.ForEach(o => o.SetActive(false));
            }
        }
    }

}
