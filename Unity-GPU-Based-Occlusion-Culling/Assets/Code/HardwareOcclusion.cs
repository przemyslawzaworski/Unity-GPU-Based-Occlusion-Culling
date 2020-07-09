using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HardwareOcclusion : MonoBehaviour 
{
	public GameObject[] Targets;
	public Shader HardwareOcclusionShader;
	public bool Debug = false;

	private Material[] _Materials;
	private ComputeBuffer _Buffer;
	private Vector4[] _Elements;
	private Vector4[] _Cache;
	private GameObject[] _Cells;
	private List<List<Renderer>> _MeshRenderers;

	GameObject GenerateCell (GameObject parent)
	{
		BoxCollider bc = parent.AddComponent<BoxCollider>();
		Bounds bounds = new Bounds (Vector3.zero, Vector3.zero);
		bool hasBounds = false;
		Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
		for (int i=0; i<renderers.Length; i++) 
		{
			if (hasBounds) 
			{
				bounds.Encapsulate(renderers[i].bounds);
			} 
			else 
			{
				bounds = renderers[i].bounds;
				hasBounds = true;
			}
		}
		if (hasBounds) 
		{
			bc.center = bounds.center - parent.transform.position;
			bc.size = bounds.size;
		}
		else
		{
			bc.size = bc.center = Vector3.zero;
			bc.size = Vector3.zero;
		}
		bc.size = Vector3.Scale(bc.size, new Vector3(1.01f, 1.01f, 1.01f));	  
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.position = parent.transform.position + bc.center;
		cube.transform.localScale = bc.size;
		Destroy(bc);
		Destroy(cube.GetComponent<BoxCollider>());
		cube.transform.parent = parent.transform;
		return cube;
	}

	bool ArrayState (Vector4[] a, Vector4[] b)
	{
		for (int i=0; i<a.Length; i++)
		{
			bool x = Vector4.Dot(a[i], a[i]) > 0.0f;
			bool y = Vector4.Dot(b[i], b[i]) > 0.0f;
			if (x != y) return false;
		}
		return true;
	}

	void ArrayCopy (Vector4[] source, Vector4[] destination)
	{
		for (int i=0; i<source.Length; i++) destination[i] = source[i];
	}

	void Init()
	{
		_MeshRenderers = new List<List<Renderer>>();
		_Materials = new Material[Targets.Length];
		_Buffer = new ComputeBuffer(1, Targets.Length * 16, ComputeBufferType.Default);
		_Elements = new Vector4[Targets.Length];
		_Cache = new Vector4[Targets.Length];
		if (_Cache.Length > 0) _Cache[0] = Vector4.one;
		_Cells = new GameObject[Targets.Length];
		Graphics.ClearRandomWriteTargets();
		Graphics.SetRandomWriteTarget(1, _Buffer, false);
		for (int i=0; i<Targets.Length; i++)
		{
			_MeshRenderers.Add(Targets[i].GetComponentsInChildren<Renderer>().ToList());
			_Materials[i] = new Material(HardwareOcclusionShader);
			_Cells[i] = GenerateCell(Targets[i]);
			_Cells[i].name = "Cell" + i.ToString();
			_Cells[i].GetComponent<Renderer>().material = _Materials[i];
			_Materials[i].SetPass(0);
			_Materials[i].SetInt("index", i);
			_Materials[i].SetInt("debug", System.Convert.ToInt32(Debug));
			_Materials[i].SetBuffer("buffer", _Buffer);
		}
	}

	void OnEnable() 
	{
		Init();
	}

	void Update() 
	{
		_Buffer.GetData(_Elements);
		bool state = ArrayState (_Elements, _Cache);
		if (!state)
		{
			for (int i=0; i<_MeshRenderers.Count; i++)
			{
				for (int j=0; j<_MeshRenderers[i].Count; j++)
				{
					_MeshRenderers[i][j].enabled = (Vector4.Dot(_Elements[i], _Elements[i]) > 0.0f);
				}
			}
			ArrayCopy(_Elements, _Cache);
		}
		System.Array.Clear(_Elements, 0, _Elements.Length);
		_Buffer.SetData(_Elements);
	}

	void OnDisable()
	{
		_Buffer.Dispose();
		for (int i=0; i<_Cells.Length; i++) Destroy(_Cells[i]);
		for (int i=0; i<_MeshRenderers.Count; i++)
		{
			for (int j=0; j<_MeshRenderers[i].Count; j++)
			{
				_MeshRenderers[i][j].enabled = true;
			}
		}
	}
}
