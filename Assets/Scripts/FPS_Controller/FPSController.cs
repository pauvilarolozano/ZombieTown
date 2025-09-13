using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public delegate void CurveBobCallBack ();

[System.Serializable]
public class CurveBobEvent{

	public float time = 0.0f;
	public CurveBobCallBack function = null;
}

[System.Serializable]
public class CurveHeadBob{

	//ESTO GENERA UNA CURVA SIMILAR A LA FUNCIÓN DE COS/SIN DONDE LE ASIGNAMOS LOS PUNTOS PARA REALIZAR LA CURVATURA
	[SerializeField] private AnimationCurve headBobCurve = new AnimationCurve( new Keyframe(0f,0f), new Keyframe(0.5f, 1f), 
																	   new Keyframe(1f,0f), new Keyframe(1.5f, -1f),
																	   new Keyframe(2f,0f));
	[SerializeField] private float horizontalMultiplier = 0.02f;
	[SerializeField] private float verticalMultiplier = 0.02f;
	[SerializeField] private float verticalToHorizontalRatio = 2.0f;
	[SerializeField] private float interval = 1.5f;

	private float prevYPosCurve;

	private float xPosCurve;
	private float yPosCurve;

	private float curveEndTime;

	private CurveBobEvent stepSoundEvent;

	//INICIALIZADOR DE LOS COMPONENTES DE LA CURVA PARA REALIZAR EL MOVIMIENTO DE LA CABEZA
	public void initialize(){

		curveEndTime = headBobCurve [headBobCurve.length - 1].time;

		xPosCurve = 0.0f;
		yPosCurve = 0.0f;

		prevYPosCurve = 0.0f;
	}

	//INICIALIZADOR DEL EVENTO DEL MOVIMIENTO DE LA CABEZA
	public void initBobEvent(float time, CurveBobCallBack function){

		CurveBobEvent newEvent = new CurveBobEvent ();
		newEvent.time = time;
		newEvent.function = function;
		stepSoundEvent = newEvent;
	}

	//MÉTODO PARA DEVOLVER UN VECTOR PARA AÑADIR LUEGO AL JUGADOR PARA SIMULAR EL MOVIMIENTO DE LA CABEZA AL CAMINAR
	public Vector3 getBobCurveVector (float speed){

		xPosCurve += (speed * Time.deltaTime) / interval;
		yPosCurve += ((speed * Time.deltaTime) / interval) * verticalToHorizontalRatio;

		if (xPosCurve > curveEndTime)
			xPosCurve -= curveEndTime;
		
		if (yPosCurve > curveEndTime)
			yPosCurve -= curveEndTime;

		CurveBobEvent ev = stepSoundEvent;

		if ((prevYPosCurve < ev.time && yPosCurve >= ev.time) || (prevYPosCurve > yPosCurve && (ev.time > prevYPosCurve || ev.time <= yPosCurve))) {
			ev.function ();
		}
			
		float xPos = headBobCurve.Evaluate(xPosCurve) * horizontalMultiplier;
		float yPos = headBobCurve.Evaluate(yPosCurve) * verticalMultiplier;

		prevYPosCurve = yPosCurve;

		return new Vector3 (xPos, yPos, 0f);
	}
}


[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour {

	//ASIGNACIÓN DE VARIABLES
	[SerializeField] private float walkSpeed = 1.0f;
	[SerializeField] private float runSpeed = 4.5f;
	[SerializeField] private float croughSpeed = 1.5f;
	[SerializeField] private float jumpForce = 7.5f;

	[SerializeField] private float stickToGroundForce = 5.0f;
	[SerializeField] private float gravityMultiplier = 2.5f;
	[SerializeField] private float runStepLengthen = 0.6f;

	[SerializeField] private float health = 100;

	[SerializeField] private UnityStandardAssets.Characters.FirstPerson.MouseLook mouseLook = new UnityStandardAssets.Characters.FirstPerson.MouseLook();

	[SerializeField] private AudioClip jumpSound;
	[SerializeField] private AudioClip stepSound;
	[SerializeField] private AudioClip landSound;

	[SerializeField] private CurveHeadBob headBob = new CurveHeadBob();

	public List<AudioSource> audioSources = new List<AudioSource> ();
	private int audioToUse = 0;

	private AudioSource audioSource;

	private float speed;
	private float controllerHeiht = 0.0f;

	private Camera mainCamera;

	private Vector2 inputVector = Vector2.zero;
	private Vector3 moveDirection = Vector3.zero;

	private bool isWalking = true;
	private bool isRunning = false;
	private bool isJumping = false;
	private bool jumpButtonPressed = false;
	private bool previouslyGrounded = false;
	private bool isCroughing = false;
	private bool isShooting = false;
	private bool isReloading = false;
	private bool isPointing = false;
	private bool isDeath = false;

	private Vector3 cameraLocalPosition = Vector3.zero;

	// Timers
	private float animTransactionTimer = 0.0f;

	private CharacterController characterController;
	private Animator animator;

	protected void Awake() {

		characterController = GetComponent<CharacterController>();
		controllerHeiht = characterController.height;

		animator = GetComponent<Animator>();

		audioSource = GetComponent<AudioSource>();

		mainCamera = Camera.main;
		cameraLocalPosition = mainCamera.transform.localPosition;
		mouseLook.Init(transform, mainCamera.transform);


		headBob.initialize();
		headBob.initBobEvent(2f, playStepSound);

		isWalking = true;
		isRunning = false;
		isJumping = false;
	 	jumpButtonPressed = false;
		previouslyGrounded = false;
		isCroughing = false;
		isShooting = false;
		isReloading = false;
		isPointing = false;
		isDeath = false;
	}

	protected void Update(){

		if (GameOverMenu.endGame)
			return;
			
		if (Time.timeScale > Mathf.Epsilon) mouseLook.LookRotation(transform, mainCamera.transform);

		if (!jumpButtonPressed && !isCroughing ) jumpButtonPressed = Input.GetButtonDown("Jump");

		if (Input.GetButtonDown("Crough")) {
			isCroughing = !isCroughing;
			characterController.height = isCroughing ? controllerHeiht / 1.5f : controllerHeiht;
		}

		isShooting = Input.GetButton("Fire1");


		//LLamar función para calcular el estatus del jugador
		calculateCharacterStatus ();
		recoverHealth (); 

	}

	protected void FixedUpdate(){

		if (GameOverMenu.endGame)
			return;

		//LLAMADA DE MÉTODOS PARA CALCULAR CONSTANTEMENTE LOS DIFERENTES COMPORTAMIENTOS FÍSICOS DEL JUGADOR
		movement ();
		jump ();
		checkVelocity ();

	}

	//MÉTODO PARA REALIZAR EL MOVIMIENTO DEL PERSONAJES Y APLICARLES SUS FUERZAS CORRESPONDIENTES EN DIRECCIÓN X, Z
	private void movement(){
		//Leer input axis
		float horizontal = Input.GetAxis("Horizontal");
		float vertical = Input.GetAxis("Vertical");

		inputVector = new Vector2(horizontal, vertical);

		if (inputVector.sqrMagnitude > 1)	inputVector.Normalize();

		Vector3 desiredMove = transform.forward*inputVector.y + transform.right*inputVector.x;

		RaycastHit hitInfo;
		if (Physics.SphereCast (transform.position, characterController.radius, Vector3.down, out hitInfo, characterController.height / 2f, 1))
			desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
		

		//Aplicar velocidad a los componente x-z.
		moveDirection.x = desiredMove.x*speed;
		moveDirection.z = desiredMove.z*speed;

		// Mover el personaje
		characterController.Move(moveDirection*Time.fixedDeltaTime);

		if (characterController.velocity.magnitude > 0.01f)
			mainCamera.transform.localPosition = cameraLocalPosition + headBob.getBobCurveVector (characterController.velocity.magnitude * (isWalking? 1.0f : runStepLengthen));
		else
			mainCamera.transform.localPosition = cameraLocalPosition;
		
	}

	//MÉTODO QUE ES LLAMADO PARA REALIZAR SONIDOS DE ANDAR O CORRER
	void playStepSound(){

		if (isCroughing | !characterController.isGrounded)
			return;
		
		audioSources [audioToUse].clip = stepSound;
		audioSources [audioToUse].Play ();
		audioToUse = (audioToUse == 0) ? 1: 0;
	}

	//MÉTODO PARA SALTAR
	private void jump(){
		if (characterController.isGrounded){

			// Aplicar fuerza negativa en el eje Y
			moveDirection.y = -stickToGroundForce;

			//Si estamos pulsando la tecla para saltar asignar una fuerza vertical positiva y asignar booleanos
			if (jumpButtonPressed){


				moveDirection.y = jumpForce;

				jumpButtonPressed = false;
				isJumping = true;

				//Reiniciar timer de animación para que cuando salte y vuelva a correr la transacción de la animación se haga correctamente
				animTransactionTimer = 0f;
			}

		}else{
			moveDirection += Physics.gravity*gravityMultiplier*Time.fixedDeltaTime;
		}
	}


	//MÉTODO PARA CALCULAR LA VELOCIDAD Y REALIZAR ANIMACIÓN DE CORRER
	private void checkVelocity(){
			//Si la dirección del personaje es únicamente horizontal o hacia atrás no se podrá correr y se asigna el valor de walkSpeed
		if (Input.GetKey (KeyCode.W) && Input.GetKey (KeyCode.LeftShift) && !Input.GetKey (KeyCode.S)) {
			isWalking = false;
			isRunning = true;

			if (!isJumping && !isCroughing &&!isShooting &&!isPointing) {
				if (animTransactionTimer < 0.2f) {
					animator.CrossFadeInFixedTime ("Run", 0.1f);
					animTransactionTimer += Time.deltaTime;
			
				} else {
					animator.PlayInFixedTime ("Run");

				}
			}
				
		} else {
			isWalking = true;
			isRunning = false;
			animTransactionTimer = 0.0f;
		}

		//Asignar valor según si estamos andando o corriendo
		speed = isCroughing? croughSpeed: isWalking ? walkSpeed : runSpeed;

	}
		
	//MÉTODO PARA CALCULAR Y VARIAR ALGUNOS ESTATUS DEL JUGADOR
	private void calculateCharacterStatus(){

		if (!previouslyGrounded && characterController.isGrounded) {

			isJumping = false;
			audioSource.clip = landSound;
			audioSource.Play ();


		}else if (isShooting) {

			if (isCroughing) {
				speed = croughSpeed;

			} else if (isRunning) {
				speed = walkSpeed;

			} else
				speed = walkSpeed;

		}

		previouslyGrounded = characterController.isGrounded;

	}
		
	//MÉTODO PARA RESTARNOS VIDA
	public void takeDamage(float damage){
		setHealth (damage);
		if (health < 0) health = 0.0f;

	}

	//MÉTODO PARA DETECTAR SI NOS GOLPEA UNA PARTE DEL ZOMBIE Y COGER SU COMPONENTE PARA QUITARNOS VIDA
	private void OnTriggerEnter(Collider other){
		if(other.gameObject.layer == LayerMask.NameToLayer("BodyPart")){
			ZombieAI zombie = other.GetComponentInParent<ZombieAI> ();
			takeDamage(zombie.makeDamage());
		}
	}	
		
	//MÉTODO PARA RECUPERAR VIDA 
	public void recoverHealth(){
		if (health < 100 && !isDeath) {
			health += Time.deltaTime * 10;
		}
	}

	private void OnControllerColliderHit(ControllerColliderHit hit) {
		
		//Layer de la parte del cuerpo de un zombie
		if (hit.collider.gameObject.layer == 8) {

			Vector3 pushDir = transform.position - hit.collider.transform.position;
			pushDir.y = 0;
			pushDir.Normalize();

			//Empujar jugador fuera en caoso de subirse a un zombie
			characterController.Move(pushDir * 7.5f * Time.deltaTime);

		}
    }
		
	//MÉTODOS SET/GET

	public bool getIsShooting { get { return this.isShooting; } }
	public bool getIsReloading { get{ return this.isReloading;}}
	public bool getIsRunning { get{ return this.isRunning;}}

	public float getHealth { get{ return health; }}

	public void playRondSound() { audioSources[2].Play(); }

	public float getWalkSpeed { get{ return walkSpeed;}}
	public float getRunSpeed { get{ return runSpeed;}}

	public void setSpeed(float value) { this.speed = value; }

	public void setIsShooting(bool value){ this.isShooting = value; }
	public void setIsReloading(bool value) { this.isReloading = value; }
	public void setIsPointing(bool value) { this.isPointing = value; }

	public void setHealth (float damage){health = health - damage; }

}