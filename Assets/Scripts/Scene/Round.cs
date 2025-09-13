using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Round : MonoBehaviour {

	[SerializeField] private TextMeshProUGUI roundText;
	[SerializeField] private UnityEngine.UI.Text finalMessageText;

	[SerializeField] Camera playerCam;
	[SerializeField] Camera ammoDoorCam;
	[SerializeField] Camera upgradeDoorCam;

	[SerializeField] private GameObject ammoDoor;
	[SerializeField] private GameObject upgrateDoor;
	[SerializeField] private List<Transform> respawnsTransform = new List<Transform>();
	[SerializeField] private GameObject zombieGO;

	[SerializeField] private FPSController fpsController;

	private static int numZombies;
	private static int numRound;
	private static int numDeadZombies;

	private int MAX_ZOMBIES;
	private float increment;

	private Vector3 ammoDoorTargetPos;
	private Vector3 upgradeDoorTargetPos;


	void Awake() {
		numRound = 0;
		numZombies = 1;
		numDeadZombies = 0;

		MAX_ZOMBIES = 150;
		increment = 0;

		roundText.text = numRound.ToString() + 1;

		upgradeDoorTargetPos = upgrateDoor.transform.localPosition;
		upgradeDoorTargetPos.x = 1.3f;

		ammoDoorTargetPos = ammoDoor.transform.localPosition;
		ammoDoorTargetPos.z = -1.3f;

		playerCam.enabled = true;
		ammoDoorCam.enabled = false;
		upgradeDoorCam.enabled = false;

		StartCoroutine("startRound");	
	}

	// Update is called once per frame
	void Update() {
		//SI LA RONDA HA TERMINADO LLAMAMOS ESTAS DOS FUNCIONES
		if (isRoundOver()) {
			StartCoroutine("startRound");
			checkOpeningDoors();
		}
	}

	//MÉTODO PARA COMPROVAR SI LA RONDA HA TERMINADO
	private bool isRoundOver() {
		return numDeadZombies == numZombies;
	}

	//MÉTODO IENUMEATOR PARA GENERAR LA RONDA DENTRO DEL COROUTINES
	private IEnumerator startRound() {
		fpsController.playRondSound();

		numDeadZombies = 0;
		numRound += 1;
		numZombies += 3;

		if (numZombies > MAX_ZOMBIES) numZombies = MAX_ZOMBIES;

		roundText.text = numRound.ToString();
		finalMessageText.text = "You have survived " + numRound.ToString() + " round/s";

		increment += 0.03f;

		yield return new WaitForSeconds(5f);

		//	INSTANCIAMOS LOS ZOMBIS DE LA RONDA
		for (int i = 0; i < numZombies; i++) {
			GameObject gObject = Instantiate(zombieGO, respawnsTransform[randomRespawn(respawnsTransform.Count)].position, Quaternion.identity);
			ZombieAI zombie = gObject.GetComponent<ZombieAI>();

			ZombieAI.Locomotion locomotion;
			bool willScream = false;

			if (numRound <= 2) {
				locomotion = ZombieAI.Locomotion.Walking;

			} else if (numRound <= 5) {
				if (i % 3 == 0) {
					locomotion = ZombieAI.Locomotion.Walking;
					if (i % 6 == 0) willScream = true;

				} else locomotion = ZombieAI.Locomotion.Running;

			} else {
				if (i % 4 == 0) {
					locomotion = ZombieAI.Locomotion.Walking;
					if (i % 12 == 0) willScream = true;
					
				} else locomotion = ZombieAI.Locomotion.Running;
			}

			zombie.init(increment, willScream, locomotion);
			yield return new WaitForSeconds(1.75f);
		}
	}

	//MÉTODO PARA ESCOGER UN RESPAWN ALEATORIO DE LOS POSSIBLES DENTRO EL MAPA
	private int randomRespawn(int respawnCount) {
		return Random.Range(0, respawnCount - 1);
	}

	//MÉTODO PARA CALCULAR LOS ZOMBIES MUERTOS Y INCREMENTAR PUNTUACIÓN
	public static void setDeadZombies(string bodyPart) {
		numDeadZombies += 1;
		Score.setKills();

		if (bodyPart.Equals("Head")) {
			Score.setHeadShots();
		}
	}

	//MÉTODO PARA ABRIR PUERTAS DEPENDIENDO DE LA RONDA
	private void checkOpeningDoors() {
		if (numRound == 3) { 
			StartCoroutine(openDoor(ammoDoor,ammoDoorTargetPos,ammoDoorCam));
		
		} else if (numRound == 6) { 
			StartCoroutine(openDoor(upgrateDoor,upgradeDoorTargetPos,upgradeDoorCam));
		}
	}

	private IEnumerator openDoor(GameObject door, Vector3 targetPos, Camera cam) {

		yield return new WaitForSeconds(0.75f);

		playerCam.enabled = false;
		cam.enabled = true;

		AudioSource audioSource = door.GetComponent<AudioSource>();
		audioSource.Play();

		Vector3 startPos = door.transform.localPosition;
		float openingTime = audioSource.clip.length;
		float t = 0;

		while (t < openingTime && door.transform.localPosition != targetPos) {
			t += Time.deltaTime;
			float tt = Mathf.Clamp01(t / openingTime);
			door.transform.localPosition = Vector3.Lerp(startPos, targetPos, tt);

			yield return null;
		}
		cam.enabled = false;
		playerCam.enabled = true;
	}

	public static int getNumRound() {
		return numRound;
	}

	public static int getNumZombies() {
		return numZombies;
	}
	
	public static int getNumDeadZombies(){
		return numDeadZombies;
	}
		
}
