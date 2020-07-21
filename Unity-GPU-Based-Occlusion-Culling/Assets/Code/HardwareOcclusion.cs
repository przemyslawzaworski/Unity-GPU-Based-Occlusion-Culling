using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using Unity.Collections;

public class HardwareOcclusion : MonoBehaviour 
{
	public GameObject[] Targets;
	public Shader HardwareOcclusionShader;
	public ComputeShader IntersectionShader;
	public bool Dynamic = false;
	public uint Delay = 1;
	public bool Debug = false;

	private Material _Material;
	private ComputeBuffer _Reader;
	private ComputeBuffer _Writer;
	private Vector4[] _Elements;
	private Vector4[] _Cache;
	private List<List<Renderer>> _MeshRenderers;
	private List<Vector4> _Vertices;

	private ComputeBuffer _AABB;
	private ComputeBuffer _Intersection;
	private Cuboid[] _Cuboids;
	private int[] _Reset;
	private int _CellIndex = -1;
	private Coroutine _Coroutine;

	struct Cuboid
	{
		public Vector3 Center;
		public Vector3 Scale;
	};

	Vector3 GetCenterFromCubeVertices (Vector4[] verts)
	{
		Vector3 total = Vector3.zero;
		int length = verts.Length;
		for (int i = 0; i < length; i++)
		{
			total += new Vector3(verts[i].x, verts[i].y, verts[i].z);
		}
		return total / length;
	}

	Vector3 GetScaleFromCubeVertices (Vector4[] verts)
	{
		Vector3 min = Vector3.positiveInfinity;
		Vector3 max = Vector3.negativeInfinity;
		for (int i = 0; i < verts.Length; i++)
		{
			Vector3 point = new Vector3(verts[i].x, verts[i].y, verts[i].z);
			min = Vector3.Min(min, point);
			max = Vector3.Max(max, point);
		}
		return (max - min) * 0.5f;
	}

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
		for (int i=0; i<Targets.Length; i++) 
		{
			Vector4[] aabb = GenerateCell(Targets[i], i);
			_Cuboids[i].Center = GetCenterFromCubeVertices(aabb);
			_Cuboids[i].Scale = GetScaleFromCubeVertices(aabb);
			_Vertices.AddRange(aabb);
		}
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
		_Cuboids = new Cuboid[Targets.Length];
		if (_Cache.Length > 0) _Cache[0] = Vector4.one;
		_Vertices = new List<Vector4>();
		Graphics.ClearRandomWriteTargets();
		Graphics.SetRandomWriteTarget(1, _Writer, false);
		for (int i=0; i<Targets.Length; i++)
		{
			_MeshRenderers.Add(Targets[i].GetComponentsInChildren<Renderer>().ToList());
			Vector4[] aabb = GenerateCell(Targets[i], i);
			_Cuboids[i].Center = GetCenterFromCubeVertices(aabb);
			_Cuboids[i].Scale = GetScaleFromCubeVertices(aabb);
			_Vertices.AddRange(aabb);
		}
		_Reader = new ComputeBuffer(_Vertices.Count, 16, ComputeBufferType.Default);
		_Reader.SetData(_Vertices.ToArray());
		_Material.SetBuffer("_Reader", _Reader);
		_Material.SetBuffer("_Writer", _Writer);
		_Material.SetInt("_Debug", System.Convert.ToInt32(Debug));
		_AABB = new ComputeBuffer(_Cuboids.Length, 24, ComputeBufferType.Default);
		_Intersection = new ComputeBuffer(1, 4, ComputeBufferType.Default);
		IntersectionShader.SetBuffer(0, "_AABB", _AABB);
		IntersectionShader.SetBuffer(0, "_Intersection", _Intersection);
		_AABB.SetData(_Cuboids);
		_Reset = new int[1] {-1};
		_Coroutine = StartCoroutine(UpdateAsync());
	}

	void OnEnable() 
	{
		Init();
	}

	void Update() 
	{
		if (Dynamic) GenerateMap();
		if (Time.frameCount % Delay != 0) return;
		_Writer.GetData(_Elements);
		bool state = ArrayState (_Elements, _Cache);
		if (!state)
		{
			for (int i=0; i<_MeshRenderers.Count; i++)
			{
				for (int j=0; j<_MeshRenderers[i].Count; j++)
				{
					if (i == _CellIndex)
						_MeshRenderers[i][j].enabled = true;
					else
						_MeshRenderers[i][j].enabled = (Vector4.Dot(_Elements[i], _Elements[i]) > 0.0f);
				}
			}
			ArrayCopy(_Elements, _Cache);
		}
		System.Array.Clear(_Elements, 0, _Elements.Length);
		_Writer.SetData(_Elements);
	}

	IEnumerator UpdateAsync()
	{
		while (true)
		{
			Vector3 p = Camera.main.transform.position;
			IntersectionShader.SetVector("_Point", new Vector4(p.x, p.y, p.z, 0.0f));
			_Intersection.SetData(_Reset);
			int threadGroupsX = (int) Mathf.Ceil(_Cuboids.Length / 8.0f);
			IntersectionShader.Dispatch(0, threadGroupsX, 1, 1);
			AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(_Intersection);
			yield return new WaitUntil(() => request.done);
			_CellIndex = request.GetData<int>()[0];
		}
	}

	void OnRenderObject() 
	{
		_Material.SetPass(0);
		Graphics.DrawProcedural(MeshTopology.Triangles, _Vertices.Count, 1);
	}

	void OnDisable()
	{
		StopCoroutine(_Coroutine);
		_Reader.Dispose();
		_Writer.Dispose();
		_AABB.Dispose();
		_Intersection.Dispose();
		for (int i=0; i<_MeshRenderers.Count; i++)
		{
			for (int j=0; j<_MeshRenderers[i].Count; j++)
			{
				_MeshRenderers[i][j].enabled = true;
			}
		}
	}
}