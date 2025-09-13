using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Score : MonoBehaviour {

	[SerializeField] private TextMeshProUGUI inGameTemporalScoreText;

	[SerializeField] private TextMeshProUGUI pauseTemporalScoreText;
	[SerializeField] private TextMeshProUGUI pauseTemporalKillsText;
	[SerializeField] private TextMeshProUGUI pauseTemporalHsText;

	[SerializeField] private TextMeshProUGUI gameOverTemporalScoreText;
	[SerializeField] private TextMeshProUGUI gameOverTemporalKillsText;
	[SerializeField] private TextMeshProUGUI gameOverTemporalHsText;

	private static TextMeshProUGUI inGameScoreText;

	private static TextMeshProUGUI pauseScoreText;
	private static TextMeshProUGUI pauseKillsText;
	private static TextMeshProUGUI pauseHsText;

	private static TextMeshProUGUI gameOverScoreText;
	private static TextMeshProUGUI gameOverKillsText;
	private static TextMeshProUGUI gameOverHsText;


	private static int totalScore = 500;
	private static int currentScore = 500;
	private static int kills = 0;
	private static int headShots = 0;

	void Awake(){
		//COMO NO SE PUEDE SERIALIZAR CAMPOS ESTÁTICOS CREAMOS VARIABLES TEMPORALES SERIALIZABLES DESDE EL INSPECTOR DE UNITY PARA ASOCIAR LUEGO SU VALOR A LOS CAMPOS ESTÁTICOS
		inGameScoreText = inGameTemporalScoreText;
		inGameScoreText.text = currentScore.ToString ();

		pauseScoreText = pauseTemporalScoreText;
		pauseKillsText = pauseTemporalKillsText;
		pauseHsText = pauseTemporalHsText;

		gameOverScoreText = gameOverTemporalScoreText;
		gameOverKillsText = gameOverTemporalKillsText;
		gameOverHsText = gameOverTemporalHsText;
	}
		
	//MÉTODOS ESTÁTICOS PARA ACTIALIZAR LAS PUNTUACIONES DESDE CUALQUIER SCRIPT SIN CREAR UN OBJETO PUNTUACIÓN
	public static void setCurrentScore(int value) { 
		currentScore += value; 
		inGameScoreText.text = currentScore.ToString();
	}

	public static void setKills() {	
		kills += 1;	
		pauseKillsText.text = kills.ToString();
	}

	public static void setHeadShots() {	
		headShots += 1;	
		pauseHsText.text = headShots.ToString();
	}

	public static void resetScores(){
		headShots = 0;
		kills = 0;
		totalScore = 500;
		currentScore = 500;
	}

	public static void setTotalScore(int value) { 
		totalScore += value; 
		pauseScoreText.text = totalScore.ToString();
	}

	public static void setGameOverMenuScores(){
		gameOverScoreText.text = totalScore.ToString();
		gameOverKillsText.text = kills.ToString();
		gameOverHsText.text = headShots.ToString();
	}

	public static int getCurrentScore(){
		return currentScore;
	}

	public static int getTotalScore(){
		return totalScore;
	}

	public static int getTotalKills(){
		return kills;
	}

	public static int getTotalHs(){
		return headShots;
	}


}


