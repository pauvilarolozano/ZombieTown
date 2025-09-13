using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PauseMenu : MonoBehaviour {

	[SerializeField] private GameObject pauseMenu;	
	[SerializeField] private GameObject playerHud;

	private bool gameIsPaused = false;

	// Update is called once per frame
	void Update () {

		if (!GameOverMenu.endGame) {
			if (Input.GetKeyDown (KeyCode.P)) {
				if (gameIsPaused)
					resumeGame ();
				else
					pauseGame ();
			}
		}
	}

	private void resumeGame(){
		pauseMenu.SetActive (false);
		playerHud.SetActive (true);

		Cursor.visible = false;
		gameIsPaused = false;

		Time.timeScale = 1f;
	}

	private void pauseGame(){
		Cursor.lockState = CursorLockMode.None;
		playerHud.SetActive (false);
		pauseMenu.SetActive (true);

		Cursor.visible = true;
		gameIsPaused = true;

		Time.timeScale = 0f;
	}

	public void quitGame(){
		exitInit ();
		SceneManager.LoadScene ("MainMenu");
	}

	public void resetGame(){
		exitInit ();
		Scene scene = SceneManager.GetActiveScene ();
		SceneManager.LoadScene (scene.name);
	}

	private void exitInit(){
		gameIsPaused = false;
		Time.timeScale = 1f;
		Cursor.visible = true;
		Score.resetScores ();
	}

}
