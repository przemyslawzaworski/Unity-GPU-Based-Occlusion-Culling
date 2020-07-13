using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HardwareOcclusion : MonoBehaviour 
{
	public GameObject[] Targets;
	public Shader HardwareOcclusionShader;
	public bool Dynamic = false;
	public bool Debug = false;

	private Material _Material;
	private ComputeBuffer _Reader;
	private ComputeBuffer _Writer;
	private Vector4[] _Elements;
	private Vector4[] _Cache;
	private List<List<Renderer>> _MeshRenderers;
	private List<Vector4> _Vertices;

	Vector4[] GenerateCell (GameObject parent, int index)
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
		Mesh mesh = cube.GetComponent<MeshFilter>().sharedMesh;
		Vector4[] vertices = new Vector4[mesh.triangles.Length];
		for (int i=0; i<vertices.Length; i++)
		{
			Vector3 p = cube.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]);
			vertices[i] = new Vector4(p.x, p.y, p.z, index);
		}
		Destroy(bc);
		Destroy(cube);
		return vertices;
	}

	void GenerateMap ()
	{
		_Vertices.Clear();
		_Vertices.TrimExcess();
		for (int i=0; i<Targets.Length; i++) _Vertices.AddRange(GenerateCell(Targets[i], i));
		_Reader.SetData(_Vertices.ToArray());
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
		if (_Material == null) _Material = new Material(HardwareOcclusionShader);
		_MeshRenderers = new List<List<Renderer>>();
		_Writer = new ComputeBuffer(Targets.Length, 16, ComputeBufferType.Default);
		_Elements = new Vector4[Targets.Length];
		_Cache = new Vector4[Targets.Length];
		if (_Cache.Length > 0) _Cache[0] = Vector4.one;
		_Vertices = new List<Vector4>();
		Graphics.ClearRandomWriteTargets();
		Graphics.SetRandomWriteTarget(1, _Writer, false);
		for (int i=0; i<Targets.Length; i++)
		{
			_MeshRenderers.Add(Targets[i].GetComponentsInChildren<Renderer>().ToList());
			_Vertices.AddRange(GenerateCell(Targets[i], i));
		}
		_Reader = new ComputeBuffer(_Vertices.Count, 16, ComputeBufferType.Default);
		_Reader.SetData(_Vertices.ToArray());
		_Material.SetBuffer("_Reader", _Reader);
		_Material.SetBuffer("_Writer", _Writer);
		_Material.SetInt("_Debug", System.Convert.ToInt32(Debug));
	}

	void OnEnable() 
	{
		Init();
	}

	void Update() 
	{
		if (Dynamic) GenerateMap();
		_Writer.GetData(_Elements);
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
		_Writer.SetData(_Elements);
	}

	void OnRenderObject() 
	{
		_Material.SetPass(0);
		Graphics.DrawProcedural(MeshTopology.Triangles, _Vertices.Count, 1);
	}

	void OnDisable()
	{
		_Reader.Dispose();
		_Writer.Dispose();
		for (int i=0; i<_MeshRenderers.Count; i++)
		{
			for (int j=0; j<_MeshRenderers[i].Count; j++)
			{
				_MeshRenderers[i][j].enabled = true;
			}
		}
	}
}
