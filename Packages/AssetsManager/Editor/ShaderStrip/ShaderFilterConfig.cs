using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShaderFilterConfig", menuName = "AppConfig/ShaderFilterConfig", order = 0)]
public class ShaderFilterConfig : ScriptableObject
{
	public string keys;
	public bool reverse;
	public bool overall;
}
