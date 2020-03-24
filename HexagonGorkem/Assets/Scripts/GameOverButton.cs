using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverButton : MonoBehaviour
{
    Button button;
    [SerializeField] private GameObject GameOverObject;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(NewGame);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void NewGame()
    {
        Debug.Log("New Game!");
        Destroy(GameOverObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }
}
