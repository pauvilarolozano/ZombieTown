using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractionItem : MonoBehaviour {

	[SerializeField] private GameObject messageCanvas;
	[SerializeField] private Weapon wp;

	[SerializeField] private string message;
	[SerializeField] private int price;

	[SerializeField] private AudioSource audioSource;

	private delegate void Function(int price);
	Function interactionItemFunction;

	void Awake(){
			
		//COMPROVAR CON QUE ITEM DE INTERACCIÓN ESTAMOS TRATANDO PARA ASOCIAR UNA FUNCIÓN O OTRA
		switch (this.gameObject.name) {
			case "AmmoItem":
				interactionItemFunction = wp.setBulletsLeft;
				audioSource = GetComponent<AudioSource>();
				break;

			case "UpgradeItem":
				interactionItemFunction = wp.upgradeWeapon;
				break;
		}

	}

	//EN CASO DE ACERCARNOS A LA ZONA DE INTERACCIÓN GENERAR MENSAJE
	private void OnTriggerStay(Collider other) {
		if (!GameOverMenu.endGame) {
			if (other.tag == "Player") {
				messageCanvas.GetComponent<TextMeshProUGUI> ().text = message;
				messageCanvas.SetActive (true);

				if (Input.GetKeyDown(KeyCode.E) && Score.getCurrentScore() >= price) {
					//SI TENEMOS EL SUFICIENTE DINERO (PUNTUACIÓN ACTUAL) REALIZAR MÉTODO SEGÚN FUNCIÓN ALMACENADA AL PRINCIPIO DEL SCRIPT
					audioSource.Play ();
					interactionItemFunction (price);
					messageCanvas.SetActive (false);
				}
			}
		
		} else {
			messageCanvas.SetActive (false);
		}
	}

	//AL SALIR DE LA ZONA DE INTERACCIÓN EL MENSAJE DESAPARECE
	private void OnTriggerExit(Collider other){
		if (other.tag == "Player") {
			messageCanvas.SetActive (false);
		}
	}

}
