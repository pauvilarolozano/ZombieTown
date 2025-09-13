using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BloodScreenEffect : MonoBehaviour {

	[SerializeField] private FPSController fpsController;
	[SerializeField] private RawImage bloodEffectImage;

	private float fpsHealth;
	private Color color;

	void Start(){
		fpsHealth = fpsController.getHealth;
		bloodEffectImage = GetComponent<RawImage> ();
	}

	void FixedUpdate(){
		fpsHealth = fpsController.getHealth;
		color = new Color (1f, 1f, 1f, 1f - fpsHealth/100f );
		bloodEffectImage.color = color;
	}
}
