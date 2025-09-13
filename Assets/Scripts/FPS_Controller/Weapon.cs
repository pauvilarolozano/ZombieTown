using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Weapon : MonoBehaviour {

	[SerializeField] private ParticleSystem flash;
	[SerializeField] private Transform shootPoint;
	[SerializeField] private GameObject bulletTrail;
	[SerializeField] private GameObject upgratedBulletTrail;
	[SerializeField] private GameObject bulletHoleEffect;

	[SerializeField] private AudioClip reloadSound;
	[SerializeField] private AudioClip gunSound;
	[SerializeField] private AudioClip upgradedGunSound;

	[SerializeField] private AudioClip emptyBulletsSound;

	[SerializeField] private Renderer renderWeapon;
	[SerializeField] private Texture weaponTexture;

	[SerializeField] private GameObject weaponOnTable;
	[SerializeField] private TextMeshProUGUI ammoText;

	[SerializeField] private Camera playerCam;

	[SerializeField] private LayerMask ignoreLayer;

	private AudioSource audioSource;

	private int bulletsMag;
	private int bulletsLeft;
	private int currentBullets;

	private float damage;
	private float weaponRate;
	private float fireTimer;

	private float bulletSpeed;
	private float maxDistance;

	private float recoilXAmount;
	private float recoilYAmount;

	private bool activeCoroutine;
	private bool isUpgrated;

	private IEnumerator coroutine;

	private ZombieAI zombie;
	private Animator animator;
	private FPSController fpsController;

	private float velocidadApuntado;
	private float zoomApuntado;
	private float zoomInicial;
	private bool apuntando;
	private float fadeTime;

	void Awake() {
		//APLICAR COMPONENTES ANTES DE EMPEZAR A JUGAR
		bulletsMag = 30;
		bulletsLeft = 200;
		damage = 25f;
		weaponRate = 0.12f;

		bulletSpeed = 100f;
		maxDistance = 1000f;

		recoilXAmount = 2f;
		recoilYAmount = 2.5f;

		activeCoroutine = false;
		isUpgrated = false;
		currentBullets = bulletsMag;

		animator = GetComponentInParent<Animator>();
		fpsController = GetComponentInParent<FPSController>();

		audioSource = GetComponent<AudioSource>();

		zoomApuntado = 45f;
		velocidadApuntado = 10f;

		zoomInicial = playerCam.fieldOfView;
		apuntando = false;

		fadeTime = 2f;

	}


	// Update is called once per frame
	void Update() {

		
		
		if (GameOverMenu.endGame) {
			return;
		}

		if (playerCam.enabled) {
			apuntando = Input.GetMouseButton(1);
			fpsController.setIsPointing(apuntando);

			SistemaApuntado(apuntando);
		}

		ammoText.text = currentBullets.ToString() + " / " + bulletsLeft.ToString();

		if (currentBullets <= 0 && Input.GetButtonDown("Fire1")) {
			audioSource.clip = emptyBulletsSound;
			audioSource.Play();

		}
		else if (Input.GetButton("Fire1")) {
			fire();

		}
		else { fpsController.setIsShooting(false); }

		if (fireTimer < weaponRate) {
			fireTimer += Time.deltaTime;
		}

		if (Input.GetKeyDown(KeyCode.R) && currentBullets != bulletsMag && bulletsLeft > 0 && fpsController.getIsShooting == false && fpsController.getIsRunning == false) {

			//EMPEZAR UN COROUTINE PARA PODER CANCELAR LA RECARGA SI NO SE COMPLETA LA ANIMACIÓN MÁS ADELANTE
			if (!activeCoroutine) {
				coroutine = countDownToReload();
				StartCoroutine(coroutine);
			}
		}

		if (activeCoroutine & fpsController.getIsRunning | activeCoroutine & fpsController.getIsShooting) {
			//SI REALIZAMOS ALGUNAS ACCIONES CANCELAMOS EL COROUTINE Y LA RECARGA 
			StopCoroutine(coroutine);
			audioSource.Stop();
			activeCoroutine = false;
		}
	}

	//MÉTODO PARA DISPARAR
	private void fire() {
		if (fireTimer < weaponRate || currentBullets <= 0) {
			return;

		} else {
			fpsController.setIsShooting(true);

			//CALCULAMOS DIRECTION HACIA EL CROSSHAIR
			Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
			RaycastHit rayCastHit;
			Vector3 targetPoint;


			//AQUI ES DONDE IMPACTARA LA BALA
			if (Physics.Raycast(ray, out rayCastHit, maxDistance, ~ignoreLayer)) targetPoint = rayCastHit.point;
			else targetPoint = ray.GetPoint(maxDistance); 
			
			//DIRECCION BALA
			Vector3 shootDir = (targetPoint - shootPoint.position).normalized;

			float apuntadoMult = apuntando ? 4f : 1f;

			float recoilX = Random.Range(-recoilXAmount/apuntadoMult, recoilXAmount/apuntadoMult);
			float recoilY = Random.Range(-recoilYAmount/apuntadoMult, recoilYAmount/apuntadoMult);
			

			shootDir = Quaternion.Euler(-recoilY, recoilX, 0) * shootDir;

			//DISPARAMOS EL RAYCAST FINAL
			if (Physics.Raycast(shootPoint.position, shootDir, out rayCastHit, maxDistance, ~ignoreLayer)) {

				//SI DETECTAMOS QUE EL OBJETO TOCADO POR EL RAYCAST ES DE UNA PARTE DEL ZOMBIE HAREMOS SUCESO DE ACCIONES PARA DAÑARLO
				if (rayCastHit.transform.gameObject.layer == LayerMask.NameToLayer("BodyPart")) {
					zombie = rayCastHit.transform.GetComponentInParent<ZombieAI>();
					zombie.takeDamage(rayCastHit.point, rayCastHit.transform.name, damage);

				} else {
					GameObject bHole = Instantiate(bulletHoleEffect, rayCastHit.point, Quaternion.LookRotation(rayCastHit.normal));
					bHole.transform.Translate(bHole.transform.forward * 0.001f, Space.World);
					StartCoroutine(destroyBulletHole(bHole));
				}

				targetPoint = rayCastHit.point;
			}

			GameObject bullet = isUpgrated ? upgratedBulletTrail : bulletTrail;
			GameObject trail = Instantiate(bullet, shootPoint.position, Quaternion.identity);
			StartCoroutine(MoveTrail(trail, targetPoint));


			//APLICAR SONIDOS Y PARTÍCULA DE DISPARAR

			if (!isUpgrated)
				audioSource.clip = gunSound;
			else
				audioSource.clip = upgradedGunSound;

			audioSource.Play();
			flash.Play();

			animator.CrossFadeInFixedTime("Fire", 0.1f);

			if (currentBullets > 0) currentBullets--;
			fireTimer = 0.0f;
		}
	}

	private void SistemaApuntado(bool apuntando) {


		//Vector3 targetPos = apuntando ? adsPosition.position : weaponDefaultPos.position;

		float targetFOV = zoomInicial;
		if (apuntando) {
			targetFOV = zoomApuntado;
			fpsController.setSpeed(fpsController.getWalkSpeed);
		}

		playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, velocidadApuntado * Time.deltaTime);
		
	}

	private IEnumerator MoveTrail(GameObject trail, Vector3 target) {
		Vector3 startPos = trail.transform.position;
		float distance = Vector3.Distance(startPos, target);
		float travelTime = distance / bulletSpeed;

		float elapsed = 0f;
		while (elapsed < travelTime) {
			if (trail == null) yield break;

			trail.transform.position = Vector3.Lerp(startPos, target, elapsed / travelTime);
			elapsed += Time.deltaTime;
			yield return null;
		}

		trail.transform.position = target;
		Destroy(trail, 0.3f);
	}

	private void reload() {

		//FUNCIÓN DE RECARGAR LAS BALAS Y MUNICIÓN
		int bulletsToLoad = bulletsMag - currentBullets;
		int bulletsToDedug = (bulletsLeft >= bulletsToLoad) ? bulletsToLoad : bulletsLeft;

		bulletsLeft -= bulletsToDedug;
		currentBullets += bulletsToDedug;

		fpsController.setIsReloading(false);
	}

	//FUNCIÓN QUE ES ASOCIADA PARA REALIZAR RECARGA SI
	private IEnumerator countDownToReload() {
		activeCoroutine = true;

		animator.CrossFadeInFixedTime("Reload", 0.1f);
		fpsController.setIsReloading(true);

		audioSource.clip = reloadSound;
		audioSource.Play();

		yield return new WaitForSeconds(2f);
		reload();
		activeCoroutine = false;
	}

	//FUNCIÓN PARA DESVANECER LA BULLET HOLE POCO A POCO
	private IEnumerator destroyBulletHole(GameObject bHole) {

		SpriteRenderer sprite = bHole.GetComponent<SpriteRenderer>();
		Color color = sprite.color;
		float t = 0f;

		yield return new WaitForSeconds(3f);

		while (t < fadeTime) {

			float alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
			sprite.color = new Color(color.r, color.g, color.b, alpha);
			t += Time.deltaTime;
			yield return null;
		}

		Destroy(bHole);
	}

	//ḾETODO PARA CALCULAR SI TENEMOS FULL MUNICIÓN
	public bool checkFullBulletsLeft() {
		return bulletsLeft == 200;
	}

	//ḾETODO PARA OBTENER FULL MUNICIÓN
	public void setBulletsLeft(int price) {
		if (!checkFullBulletsLeft()) {
			bulletsLeft = 200;
			ammoText.text = bulletsLeft.ToString();

			Score.setCurrentScore(-price);
		}
	}

	//ḾETODO PARA MEJORAR EL ARMA
	public void upgradeWeapon(int price) {
		isUpgrated = true;
		damage *= 2.5f;
		renderWeapon.material.SetTexture("_MainTex", weaponTexture);

		var main = flash.main;
		main.startColor = Color.magenta;
		setBulletsLeft(0);

		Score.setCurrentScore(-price);
		Destroy(weaponOnTable);
	}

}
 