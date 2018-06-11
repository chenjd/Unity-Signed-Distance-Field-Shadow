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
		Matrix4x4 cornersStore = Matrix4x4.identity;

		Transform camtr = cam.transform;
		Vector3[] frustumCorners = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, cam.stereoActiveEye, frustumCorners);
        var bottomLeft = camtr.TransformVector(frustumCorners[0]);
        var topLeft = camtr.TransformVector(frustumCorners[1]);
        var topRight = camtr.TransformVector(frustumCorners[2]);
        var bottomRight = camtr.TransformVector(frustumCorners[3]);

        Matrix4x4 frustumCornersArray = Matrix4x4.identity;
        frustumCornersArray.SetRow(0, bottomLeft);
        frustumCornersArray.SetRow(1, bottomRight);
        frustumCornersArray.SetRow(2, topLeft);
        frustumCornersArray.SetRow(3, topRight);
		return frustumCornersArray;

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
		EffectMat.SetVector("_CamPosition", this.CurCam.transform.position);
		EffectMat.SetVector("_LightDir", this.lightDir.forward);
		EffectMat.SetVector("_LightPos", this.lightDir.position);

		Graphics.Blit(source, destination, EffectMat, 0);
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
