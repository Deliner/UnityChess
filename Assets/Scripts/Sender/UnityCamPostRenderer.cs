using UnityEngine;
using System.Collections;

public class UnityCamPostRenderer : MonoBehaviour {
	
	UnityCam _ownerCamera;
	
	void Start () {
		_ownerCamera = gameObject.GetComponent<UnityCam> ();
	}
	
	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		_ownerCamera.RenderImage (source, destination);
	}
}
