using UnityEngine;

public class VisionAI : MonoBehaviour {

	private ZombieAI zombie;

	private SphereCollider attackZone;

	private Vector3 crawlingCenterSC;
	private float crawlingRadiusSC;

	// Use this for initialization
	void Awake() {
		zombie = GetComponentInParent<ZombieAI>();
		attackZone = GetComponent<SphereCollider>();

		crawlingCenterSC = new Vector3(-0.28f, 0.4f, 0.59f);
		crawlingRadiusSC = 0.31f;
	}

	//MÉTODO PARA DETECTAR COLLIDER DENTRO DEL RANGO DE ATAQUE DEL ZOMBIE
	private void OnTriggerStay(Collider other) {
		if (other.gameObject.tag == "Player")
			zombie.setPlayerInrange(true);
	}

	private void OnTriggerExit(Collider other) {
		if (other.gameObject.tag == "Player")
			zombie.setPlayerInrange(false);
	}

	public void setSphereCollider() {
		attackZone.center = crawlingCenterSC;
		attackZone.radius = crawlingRadiusSC;
	}
}
