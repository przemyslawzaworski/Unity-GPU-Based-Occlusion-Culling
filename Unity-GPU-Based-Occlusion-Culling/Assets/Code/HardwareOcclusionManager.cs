using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HardwareOcclusionManager : MonoBehaviour
{
	private HardwareOcclusion _HardwareOcclusion;
	private bool _State = true;

	void Start()
	{
		_HardwareOcclusion = GetComponent<HardwareOcclusion>();
	}

	void OnApplicationFocus(bool hasFocus)
	{
		if (_State != hasFocus && _HardwareOcclusion)
		{
			_HardwareOcclusion.enabled = hasFocus;
			_State = hasFocus;
		}
	}
}
