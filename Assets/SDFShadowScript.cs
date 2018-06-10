using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SDFShadowScript : MonoBehaviour
{

#region Field

	[SerializeField]
	private Shader effectShader;

	[SerializeField]
	private Transform lightDir;

	private Material effectMat;

	private Camera curCam;


#endregion

	//blit之后是一个quad，只有4个顶点。则我们只需要求四个顶点的射线方向，之后fragment的射线方向使用插值之后的结果即可。
	//所以现在的任务是求出从四个顶点射向屏幕的方向，传递给vs。
	private Matrix4x4 GetCameraConer(Camera cam)
	{

		float camFov = cam.fieldOfView;
		float camAspect = cam.aspect;


		Matrix4x4 cornersStore = Matrix4x4.identity;


		float fovHalf = camFov * .5f;


		float tan_fov = Mathf.Tan(fovHalf * Mathf.Deg2Rad);

		Vector3 toRight = Vector3.right * tan_fov * camAspect;

		Vector3 toTop = Vector3.up * tan_fov;

		Vector3 topLeft = (-Vector3.forward - toRight + toTop);
		Vector3 topRight = (-Vector3.forward + toRight + toTop);
		Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
		Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

		cornersStore.SetRow(0, topLeft);
		cornersStore.SetRow(1, topRight);
		cornersStore.SetRow(2, bottomRight);
		cornersStore.SetRow(3, bottomLeft);

		return cornersStore;
	}



	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if(!EffectMat)
		{
			Graphics.Blit(source, destination);
			return;
		}

		EffectMat.SetMatrix("_Corners", GetCameraConer(this.CurCam));
		EffectMat.SetMatrix("_CamInvViewMatrix", this.CurCam.cameraToWorldMatrix);
		EffectMat.SetVector("_CamPosition", this.CurCam.transform.position);
		Debug.LogError("rotation" + this.lightDir.rotation.ToString());
		Debug.LogError("rotation_f" +  this.lightDir.forward);
		EffectMat.SetVector("_LightDir", this.lightDir.forward);
		EffectMat.SetVector("_LightPos", this.lightDir.position);

		this.ModifiedGraphicsBlit(source, destination, EffectMat, 0);
	}


    /// <summary>
    /// 为了将射线的index传入vs中。
    /// </summary>
    /// <param name="src">Source.</param>
    /// <param name="dest">Destination.</param>
    /// <param name="material">Material.</param>
    /// <param name="pass">Pass.</param>
	private void ModifiedGraphicsBlit(RenderTexture src, RenderTexture dest, Material material, int pass)
	{
		RenderTexture.active = dest;

		material.SetTexture("_MainTex", src);
    
		GL.PushMatrix();
		GL.LoadOrtho();

		material.SetPass(pass);


		GL.Begin(GL.QUADS);

		GL.MultiTexCoord2(0, 0f, 0f);
		GL.Vertex3(0f, 0f, 3f);
        

		GL.MultiTexCoord2(0, 1f, 0f);
        GL.Vertex3(1f, 0f, 2f);


		GL.MultiTexCoord2(0, 1f, 1f);
        GL.Vertex3(1f, 1f, 1f);


		GL.MultiTexCoord2(0, 0f, 1f);
        GL.Vertex3(0f, 1f, 0f);


		GL.End();
		GL.PopMatrix();
        
	}




	#region Property

	public Material EffectMat
	{
		get
		{
			if (!effectMat && effectShader)
			{
				effectMat = new Material(effectShader);
				effectMat.hideFlags = HideFlags.HideAndDontSave;
			}

			return effectMat;
		}
	}


	public Camera CurCam
	{

		get
		{
			if(!curCam)
			{
				this.curCam = GetComponent<Camera>();
			}

			return curCam;
		}
	}



    #endregion

}
