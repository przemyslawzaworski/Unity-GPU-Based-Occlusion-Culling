using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
	public GameObject Cube;
	public GameObject Cylinder;
	public GameObject Sphere;
	public GameObject Torus;

	private Vector3 _Origin;

	void Start()
	{
		_Origin = Cube.transform.position;
	}

	void Update()
	{
		Cube.transform.position = _Origin + 5.0f * new Vector3(0.0f, Mathf.Sin(2.0f * Time.time), 0.0f);
		Cylinder.transform.Rotate (new Vector3(0.0f, 10 * Time.deltaTime, 0.0f), Space.Self);
		float scale = Mathf.Sin(Time.time) * 0.5f + 1.0f;
		Sphere.transform.localScale = new Vector3(scale, scale, scale);
		Torus.transform.Rotate (new Vector3(10 * Time.deltaTime, 0.0f, 0.0f), Space.Self);
	}
}
