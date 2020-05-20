using UnityEngine;

public class CameraController : MonoBehaviour 
{
	public float mainSpeed = 10.0f;
	public float shiftAdd = 25.0f;
	public float maxShift = 100.0f;
	public float camSens = 0.25f;	
	float totalRun;	
	Vector3 lastMouse;

	void Start () 
	{
		totalRun = 1.0f;
		lastMouse = new Vector3(255.0f, 255.0f, 255.0f); 		
	}

	Vector3 GetBaseInput()  
	{
		Vector3 p_Velocity = new Vector3 (0.0f,0.0f,0.0f);
		if (Input.GetKey (KeyCode.W))
		{
			p_Velocity += new Vector3(0.0f, 0.0f , 1.0f);
		}
		if (Input.GetKey (KeyCode.S))
		{
			p_Velocity += new Vector3(0.0f, 0.0f , -1.0f);
		}
		if (Input.GetKey (KeyCode.A))
		{
			p_Velocity += new Vector3(-1.0f, 0.0f , 0.0f);
		}
		if (Input.GetKey (KeyCode.D))
		{
			p_Velocity += new Vector3(1.0f, 0.0f , 0.0f);
		}
		return p_Velocity;
	}
	
	void Update () 
	{
		lastMouse = Input.mousePosition - lastMouse ;
		lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0.0f );
		lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x , transform.eulerAngles.y + lastMouse.y, 0.0f);
		transform.eulerAngles = lastMouse;
		lastMouse =  Input.mousePosition;  
		float f = 0.0f;
		var p = GetBaseInput();
		if (Input.GetKey (KeyCode.LeftShift))
		{
			totalRun += Time.deltaTime;
			p  = p * totalRun * shiftAdd;
			p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
			p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
			p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
		}
		else
		{
			totalRun = Mathf.Clamp(totalRun * 0.5f, 1.0f, 1000.0f);
			p = p * mainSpeed;
		}  
		p = p * Time.deltaTime;
		if (Input.GetKey(KeyCode.Space))
		{ 
			f = transform.position.y;
			transform.Translate(p);
			transform.position = new Vector3(transform.position.x,f,transform.position.z);
		}
		else
		{
			transform.Translate( p);
		}
	}
}