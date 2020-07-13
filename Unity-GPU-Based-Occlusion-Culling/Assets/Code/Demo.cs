using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
	void Awake()
	{
		GameObject[] spheres = new GameObject[64 * 64];
		int i = 0;
		for (int x = 0; x < 64; x++)
		{
			for (int y = 0; y < 64; y++)
			{
				spheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				spheres[i].transform.position = new Vector3(x * 2.0f, 0.0f, y * 2.0f);
				spheres[i].name = "Sphere" + i.ToString();
				i++;
			}
		}
		GameObject.Find("HardwareOcclusion").GetComponent<HardwareOcclusion>().Targets = spheres;
	}
}