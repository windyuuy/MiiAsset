using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="ShaderStripConfig")]
public class ShaderStripConfig : ScriptableObject
{
	public ShaderVariantCollection[] shaderVariantCollections;
}
