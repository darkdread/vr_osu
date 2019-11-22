using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionAvoidance : MonoBehaviour
{
	public Transform target;

	public LayerMask obstacleMask;
	public float rayLength = 10f;
	public float speed = 5f;
	public float rotateSpeed = 8f;

	public bool showDebugRays;

	private Vector3 _velocity;
	private Vector3 _desiredVelocity;

	private Vector3 _direction;
	private Vector3 _reflectedDirection;
	private RaycastHit _hit;
	private float _penetrationDepth;

	// Update is called once per frame
	void Update()
    {
		_direction = (target.position - transform.position).normalized;
		//_direction = transform.forward;
		_velocity = transform.forward * speed * Time.deltaTime;
		_desiredVelocity = (target.position - transform.position).normalized;

		RaycastHit hit;
		Physics.Raycast(transform.position, transform.forward, out hit, rayLength, obstacleMask);

		_penetrationDepth = rayLength - hit.distance;
		print(_penetrationDepth);

		// Change direction.
		if (hit.collider != null) {
			_hit = hit;
			_reflectedDirection = Vector3.Reflect(_direction, hit.normal);

			_direction = (transform.forward + hit.normal * _penetrationDepth).normalized;
		}

		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_direction), Time.deltaTime * rotateSpeed);
		transform.position += _velocity;
	}

	private void OnDrawGizmos() {
		if (!showDebugRays) {
			return;
		}
		
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(transform.position, _direction * rayLength);

		Gizmos.color = Color.red;
		Gizmos.DrawRay(transform.position, _reflectedDirection * rayLength);

		Gizmos.color = Color.black;
		Gizmos.DrawRay(_hit.point, _hit.normal * _penetrationDepth);
	}
}
