using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;

[System.Serializable]
public class ScoreData{

	private int scoreData;
	private int killsData;
	private int headShotData;
	private int numRound;

	public ScoreData(){
		this.scoreData = Score.getTotalScore();
		this.killsData = Score.getTotalKills();
		this.headShotData = Score.getTotalHs ();
		this.numRound = Round.getNumRound ();
	}

	public int getScoreData(){
		return scoreData;
	}

	public int getKillsData(){
		return killsData;
	}

	public int getHeadShotData(){
		return headShotData;
	}

	public int getNumRound(){
		return numRound;
	}

}

public class FileSystem : MonoBehaviour {

	public static void saveScoreData(){
		string path = Application.dataPath + "/scoreFile.bin";
		BinaryFormatter formatter = new BinaryFormatter ();

		FileStream fileStream = new FileStream(path,FileMode.Append);

		ScoreData scoreData = new ScoreData();

		formatter.Serialize (fileStream, scoreData);	
		fileStream.Close ();
	}

	//FUNCIÓN PARA CARGAR FICHERO BINARIO Y PASAR LOS DATOS A LA TABLA DE PUNTUACIONES
	public static void loadScoreData(){

		//LISTA PARA GUARDAR PUNTUACIONES DEL FICHERO
		List <ScoreData> scores = new List<ScoreData>();

		string path = Application.dataPath + "/scoreFile.bin";
		BinaryFormatter formatter = new BinaryFormatter ();

		Stream stream = new FileStream(path,FileMode.Open);

		//AÑADIMOS PUNTUACIONES DEL FICHERO BINARIO A LA LISTA CREADA ANTERIORMENTE
		while (stream.Position < stream.Length) {
			ScoreData score = formatter.Deserialize (stream) as ScoreData;	
			scores.Add (score);
		}

		//LISTA ORDENADA DE LAS PUNTUACIONES
		IEnumerable<ScoreData> orderedScores = scores.OrderByDescending(score => score.getNumRound()).ThenByDescending(score => score.getScoreData()).Take(5);

		List<GameObject> scoreMenuRows = MainMenu.getScoreMenuRows ();
			
		//EMPEZAMOS A RELLENAR LA TABLA DE PUNTUACIONES
		for (int i = 0; i < orderedScores.Count(); i++) {
			TextMeshProUGUI[] scoreColumns = scoreMenuRows [i].GetComponentsInChildren<TextMeshProUGUI>();

			for (int j = 0; j < scoreColumns.Length; j++) {
				switch(scoreColumns[j].name){
					case "RoundText":
					scoreColumns [j].text = orderedScores.ElementAt(i).getNumRound().ToString();
						break;

					case "PtsText":
						scoreColumns [j].text = orderedScores.ElementAt (i).getScoreData().ToString ();
						break;

					case "KillsText":
						scoreColumns [j].text = orderedScores.ElementAt(i).getKillsData().ToString();
						break;

					case "HeadshotsText":
						scoreColumns [j].text = orderedScores.ElementAt(i).getHeadShotData().ToString();
						break;
				}
			}
		}
	}

}
