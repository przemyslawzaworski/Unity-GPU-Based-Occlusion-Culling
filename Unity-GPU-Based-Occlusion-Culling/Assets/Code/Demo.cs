using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
	float Random (Vector2 p ) 
	{
		return Mathf.Abs((Mathf.Sin( p.x * 12.9898f + p.y * 78.233f ) * 43758.5453f) % 1);
	}

	void Awake()
	{
		GameObject[] trees = new GameObject[64 * 64];
		int i = 0;
		for (int x = 0; x < 64; x++)
		{
			for (int y = 0; y < 64; y++)
			{
				trees[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				float height = Random(new Vector2(x,y)) * 10.0f + 1.0f;
				trees[i].transform.localScale = new Vector3(1.0f, height, 1.0f);
				trees[i].transform.position = new Vector3(x * 2.0f, height / 2.0f, y * 2.0f);
				trees[i].name = "Tree" + i.ToString();
				trees[i].GetComponent<Renderer>().material.SetColor("_Color", new Color(0.0f, 0.5f, 0.0f, 1.0f));
				i++;
			}
		}
		GameObject.Find("HardwareOcclusion").GetComponent<HardwareOcclusion>().Targets = trees;
	}
}