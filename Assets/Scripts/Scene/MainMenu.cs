using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {


	[SerializeField] private GameObject mainMenu;
	[SerializeField] private GameObject scoreMenu;

	[SerializeField] private List<GameObject> temporalScoreMenuRows;

	private static List<GameObject> scoreMenuRows;

	void Awake(){
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		scoreMenuRows = temporalScoreMenuRows;

		FileSystem.loadScoreData();
	}


	public void playGame(){
		SceneManager.LoadScene("ZombieTown");
	}

	public void goToScoreMenu(){
		mainMenu.SetActive (false);
		scoreMenu.SetActive (true);
	}

	public void backToMainMenu(){
		scoreMenu.SetActive (false);
		mainMenu.SetActive (true);
	}

	public void exit(){
		Application.Quit();
	}

	public static List<GameObject> getScoreMenuRows(){
		return scoreMenuRows;
	}

		
}
