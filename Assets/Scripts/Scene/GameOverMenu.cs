using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameOverMenu : MonoBehaviour {

	[SerializeField] private FPSController fpsController;
	[SerializeField] private GameObject playerGun;

	[SerializeField] private GameObject gameOverMenu;
	[SerializeField] private GameObject playerHud;

 	private IEnumerator coroutine;

	public static bool endGame = false;

	void Awake(){
		coroutine = countDownToChangeMenu();
	}

	// Update is called once per frame
	void Update () {

		//CALCULAR SI EL JUGADOR MUERE EN ALGÚN MOMENTO DE LA PARTIDA
		if (fpsController.getHealth <= 0 && !endGame) {
			endGame = true;

			Score.setGameOverMenuScores();
			gameOverMenu.SetActive (true);
			playerGun.SetActive(false);
			playerHud.SetActive(false);



			StartCoroutine (coroutine);
		
		}
	}

	//MÉTODO INUMERATOR PARA VOLVER AL MENÚ PRINCIPAL UNA VEZ PASAN 10SEGUNDOS PARA VISUALIZAR LAS PUNTUACIONES DE LA PARTIDA
	private IEnumerator countDownToChangeMenu(){

		FileSystem.saveScoreData ();
		//FileSystem.loadScoreData ();

		yield return new WaitForSeconds(10f);

		Score.resetScores ();
		SceneManager.LoadScene ("MainMenu");
		endGame = false;
	}
}
