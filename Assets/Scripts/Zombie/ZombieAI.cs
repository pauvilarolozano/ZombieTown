using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour {

	public enum Locomotion { Idle, Walking, Running, Crawling }

	//VALORES DE CADA LOCOMOCION ASOCIADOS AL BLEND TREE
	public Dictionary<Locomotion, float> locomotionValues;

	[SerializeField] private List<AudioClip> breathSoundList;
	[SerializeField] private List<AudioClip> attackSoundList;
	[SerializeField] private AudioClip hsSound;
	[SerializeField] private AudioClip screamSound;
	[SerializeField] private ParticleSystem blood;

	private Locomotion locomotionState;

	private NavMeshAgent navMesh;
	private Animator animator;
	private AudioSource audioSrc;
	private Transform playerTransform;

	private bool canScream;
	private bool isDead;
	private bool isAttacking;
	private bool isScreaming;
	private bool playerInRange;
	private bool activatedNewLocomotion;

	private float initHealth;
	private float currentHealth;
	private float legsDamage;
	private float attackForce;

	private IEnumerator dieCoroutine;
	private IEnumerator screamCoroutine;
	private IEnumerator crawlCoroutine;

	// Use this before initialization
	void Awake() {

		locomotionValues = new Dictionary<Locomotion, float>() {
			{ Locomotion.Idle, 0f },
			{ Locomotion.Crawling, 0.25f },
			{ Locomotion.Walking, 0.5f },
			{ Locomotion.Running, 1f }
		};

		animator = GetComponent<Animator>();
		navMesh = GetComponent<NavMeshAgent>();
		navMesh.speed = 1f;

		dieCoroutine = countDownToDie();
		screamCoroutine = screamAction();
		crawlCoroutine = startCrawling();

		playerTransform = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<Transform>();

		audioSrc = GetComponent<AudioSource>();

		canScream = false;
		isDead = false;
		isAttacking = false;
		isScreaming = false;
		playerInRange = false;
		activatedNewLocomotion = false;

		initHealth = 100f;
		currentHealth = initHealth;
		legsDamage = 0;
		attackForce = 25;

		//ROTACION = navMesh, POSICION = root motion
		navMesh.speed = 1.5f;
		navMesh.updatePosition = true;
		navMesh.updateRotation = true;

		navMesh.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

		animator.applyRootMotion = false;

		//MÉTODO PARA INVOCAR RESPIRACIÓN DEL ZOMBIE CADA 10-15 SEGUNDOS MIENTRAS ESTE VIVO
		float breathTime = UnityEngine.Random.Range(5f, 15f);
		InvokeRepeating("randomBreathSound", breathTime, breathTime);
	}

	void FixedUpdate() {

		if (playerInRange) {

			if (GameOverMenu.endGame) {

				if (locomotionState != Locomotion.Idle) {
					locomotionState = Locomotion.Idle;
					navMesh.SetDestination(transform.position);
					animator.SetFloat("speed", locomotionValues[locomotionState]);
					animator.CrossFadeInFixedTime("Locomotion", 0.2f, -1, Random.Range(0f, 3f));
					navMesh.speed = 0;
				}

			} else if (!isDead && !isAttacking && !isScreaming) {
				StartCoroutine(WaitForAttack());
			}

		} else if (!isDead && !isAttacking && !isScreaming) {

			navMesh.SetDestination(playerTransform.position);

			if (activatedNewLocomotion) {
				animator.SetFloat("speed", locomotionValues[locomotionState]);
				animator.CrossFadeInFixedTime("Locomotion", 0.3f, -1, Random.Range(0f, 3f));
				activatedNewLocomotion = false;
			}
		}
		

		if (!canScream && locomotionState == Locomotion.Walking && Round.getNumZombies() - Round.getNumDeadZombies() == 1)
			canScream = true;
	}


	private void OnAnimatorMove() {

		if (!playerInRange && !isDead && !isAttacking && !isScreaming) {

			Vector3 deltaPos = animator.deltaPosition;

			if (Time.deltaTime > 0) {
				float animSpeed = deltaPos.magnitude / Time.deltaTime;
				Vector3 desiredVelocity = navMesh.desiredVelocity;

				if (desiredVelocity.sqrMagnitude > 0.0001f) {
					navMesh.velocity = desiredVelocity.normalized * animSpeed;

				} else navMesh.velocity = Vector3.zero;
			}
		}
	}

	public void init(float inc, bool willScream, Locomotion locomotion) {

		canScream = willScream;
		locomotionState = locomotion;

		inc = Random.Range(inc * 0.5f, inc * 1.5f);

		if (inc > 2f) {
			inc = 2f;
		}

		initHealth += initHealth * inc;
		currentHealth = initHealth;

		attackForce += attackForce * inc;
		navMesh.speed += navMesh.speed * inc;

		int round = Round.getNumRound();

		if (locomotionState == Locomotion.Walking) {
			animator.speed = navMesh.speed;
		}

		animator.SetFloat("speed", locomotionValues[locomotionState]);
		animator.Play("Locomotion", 0, Random.Range(0f, 3f));

	}

	private IEnumerator WaitForAttack() {
		navMesh.SetDestination(transform.position);

		//HASTA QUE EL ZOMBIE NO ESTE MIRANDO AL JUGADOR NO REALIZARÁ EL ATAQUE
		while (!checkRotation() && !isAttacking) {
			Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
			toPlayer.y = 0;
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), Time.deltaTime * 5.0f);

			yield return null;
		}

		isAttacking = true;
		randomAttack();

		yield return new WaitUntil(() =>
			animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.6f &&
			!animator.IsInTransition(0)
		);

		isAttacking = false;
		activatedNewLocomotion = true;
	}

	//REALIZAR ATAQUE ALEATORIO DEL ZOMBIE
	private void randomAttack() {

		int random = Random.Range(0, 3);

		if (locomotionState != Locomotion.Crawling) {
			if (random <= 1)
				animator.CrossFadeInFixedTime("attack", 0.2f);
			else
				animator.CrossFadeInFixedTime("punch", 0.2f);

		} else animator.CrossFadeInFixedTime("crawl attack", 0.3f);

		if (random < attackSoundList.Count) {
			audioSrc.clip = attackSoundList[random];
			audioSrc.Play();
		}
	}

	//MÉTODO PARA COMPROVAR QUE EL ZOMBIE ESTÁ MIRANDO AL JUGADOR
	private bool checkRotation() {

		Vector3 forward = transform.forward;
		Vector3 toOther = (playerTransform.position - transform.position).normalized;

		float angle = Vector3.Angle(forward, toOther);
		return angle < 60f;
	}


	//MÉTODO PARA CHECKEAR SI EL ZOMBIE SIGUE VIVO
	private void checkDeath(string bodyPart) {
		if (currentHealth <= 0.0f && !isDead) {
			isDead = true;

			if (locomotionState == Locomotion.Crawling)
				animator.CrossFadeInFixedTime("death crawl", 0.7f);
			else
				animator.CrossFadeInFixedTime("death", 0.2f);

			Round.setDeadZombies(bodyPart);

			if (bodyPart == "Head") {

				audioSrc.volume = 1.0f;
				audioSrc.clip = hsSound;
				audioSrc.Play();

			}
			else audioSrc.Stop();

			Destroy(navMesh);
			StartCoroutine(dieCoroutine);
		}
	}


	//MÉTODO PARA ELIMINAR EL ZOMBIE DE LA ESCENA AL MORIR PARA NO CARGAR EL PC CUANDO TENGAMOS 300 ZOMBIES EN PANTALLA 
	private IEnumerator countDownToDie() {
		//Al morir se activa la Coroutine y a los 10 segundos el objeto se destruye para que no se visualize en el mapa
		yield return new WaitForSeconds(10f);
		Destroy(gameObject);
	}

	//MÉTODO PARA REALIZAR SONIDOS DEL ZOMBIE ALEATORIAMENTE DE LA TÍPICA RESPIRACIÓN
	private void randomBreathSound() {

		if (isDead)
			return;

		int random = Random.Range(0, breathSoundList.Count - 1);

		audioSrc.clip = breathSoundList[random];
		audioSrc.Play();
	}


	//MÉTODO PARA RECIBIR DAÑO DEL ZOMBIE DEPENDIENDO DE LA PARTE DONDE IMPACTE LA BALA
	public void takeDamage(Vector3 colliderPoint, string bodyPart, float damage) {

		//CREAR PARTICULAR Y DESTRUIRLA PARA EFECTO DE SANGRE AL IMPACTAR LA BALA EN ZOMBIE
		ParticleSystem vBlood = (ParticleSystem)Instantiate(blood, colliderPoint, Quaternion.identity);
		vBlood.Play();
		Destroy(vBlood.gameObject, 0.5f);

		if (!isDead) {

			switch (bodyPart) {
				case "Head":
					damage *= 2.0f;
					Score.setCurrentScore(30);
					Score.setTotalScore(30);
					break;

				case "Spine":
					Score.setCurrentScore(20);
					Score.setTotalScore(20);
					break;

				//Extremidades
				default:
					damage *= 0.6f;
					Score.setCurrentScore(15);
					Score.setTotalScore(10);

					if (bodyPart.Contains("Leg")) {
						legsDamage += damage;

						//ZOMBIE STARTS TO CRAWL
						if (legsDamage / initHealth >= 0.6f && !isScreaming) StartCoroutine(crawlCoroutine);
					}

					break;

			}

			setHealth(damage, bodyPart);

			//SCREAM IF AGRESSIVE BEHAVIOUR AND LOW HEALTH
			if (canScream && currentHealth / initHealth <= 0.6f && !isScreaming) {
				StartCoroutine(screamCoroutine);
			}
		}
	}



	private IEnumerator screamAction() {

		if (!isDead) {
			canScream = false;
			
			isScreaming = true;
			animator.speed = 1.0f;

			animator.CrossFadeInFixedTime("scream", 0.2f);

			audioSrc.clip = screamSound;
			audioSrc.Play();

			yield return new WaitUntil(() =>
				   animator.GetCurrentAnimatorStateInfo(0).IsName("scream") &&
				animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.8f &&
				!animator.IsInTransition(0)
			);

			locomotionState = Locomotion.Running;
			activatedNewLocomotion = true;
			isScreaming = false;
		}
	}

	private IEnumerator startCrawling() {

		float currentSpeed = animator.GetFloat("speed");
		locomotionState = Locomotion.Crawling;
		float targetSpeed = locomotionValues[locomotionState];

		Transform attackZone = transform.Find("Sensitive");
		GameObject go = attackZone.gameObject;
		go.GetComponent<VisionAI>().setSphereCollider();

		//TODO CAMBIAR MESH PARA NO SUBIRSE ENCIMA, Y PROBLEMA EN ATTACK EN BUCLE
		while (Mathf.Abs(currentSpeed - targetSpeed) > 0.01f) {
			currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 3f);
			animator.SetFloat("speed", currentSpeed);
			yield return null;
		}

		animator.speed = 2f;
		animator.SetFloat("speed", targetSpeed);
	}


	//MÉTODO PARA DEVOLVER DAÑO DEL ZOMBIE
	public float makeDamage() {
		if (!isAttacking) return 0;

		return attackForce;
	}

	//MÉTODO PARA SETEAR EL DAÑO 
	public void setHealth(float damage, string bodyPart) {
		currentHealth = currentHealth - damage;

		checkDeath(bodyPart);
	}

	public bool getIsAttacking() {
		return isAttacking;
	}

	public bool getIsDead() {
		return isDead;
	}

	public Locomotion getLocomotionState() {
		return locomotionState;
	}

	public void setIsAttacking(bool value) {
		isAttacking = value;
	}

	public void setIsDead(bool value) {
		isDead = value;
	}

	public void setLocomotionState(Locomotion locomotion) {
		locomotionState = locomotion;
	}

	public void setPlayerInrange(bool value) {
		playerInRange = value;
	}
	

}
