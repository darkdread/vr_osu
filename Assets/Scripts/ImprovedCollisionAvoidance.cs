using UnityEngine;
using System.Collections;

public class ImprovedCollisionAvoidance : MonoBehaviour {
	private RaycastHit hitLeft;
	private RaycastHit hitRight;
	private Vector3 hitNormal;
    public float minimumDistanceToAvoid;
	public float steerForce;
	public float rotateSpeed;
	public float translateSpeed;
	private Vector3 direction;
	private Quaternion rotate;
	private Vector3 leftRayDirection;
	private Vector3 rightRayDirection;
	enum Sensor {
		LEFT,
		RIGHT
	};
	Sensor touch;

	void Update() {
		leftRayDirection = transform.TransformDirection(new Vector3(-1, 0, 1));
		rightRayDirection = transform.TransformDirection(new Vector3(1, 0, 1));

		if(Physics.Raycast(transform.position, leftRayDirection, out hitLeft, minimumDistanceToAvoid)) {
			if(hitLeft.transform != transform) { // Intersection with own collider is omitted
				touch = Sensor.LEFT;
			}
		}

		if(Physics.Raycast(transform.position, rightRayDirection, out hitRight, minimumDistanceToAvoid)) {
			if(hitRight.transform != transform) { // Intersection with own collider is omitted
				touch = Sensor.RIGHT;
			}
		}

		switch(touch) {
			case Sensor.LEFT:
				Vector3 leftHitNormal = hitLeft.normal;
				Debug.DrawRay(hitLeft.point, leftHitNormal, Color.red);
				leftHitNormal.y = 0.0f; // Restrict movement in y direction
				direction = transform.forward + leftHitNormal * steerForce;
				break;
			case Sensor.RIGHT:
				Vector3 rightHitNormal = hitRight.normal;
				Debug.DrawRay(hitRight.point, rightHitNormal, Color.blue);
				rightHitNormal.y = 0.0f; // Restrict movement in y direction
				direction = transform.forward + rightHitNormal * steerForce;
				break;
		}

		rotate = Quaternion.LookRotation(direction.normalized);
		transform.rotation = Quaternion.Slerp(transform.rotation, rotate, Time.deltaTime * rotateSpeed);
		transform.position += transform.forward * Time.deltaTime * translateSpeed;
	}
}