using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
// 生成Prefilter Cubemap,用于环境光Irradiance
public class PreCompute : MonoBehaviour
{
	
	public ComputeShader prefilterDiffuseCS,prefilterSpecularCS,envBrdfLutfCS;
	
	public Cubemap environmentCubemap;
	
	private Cubemap outputCubemap;

	void Start()
	{
		PrefilterDiffuseCubemap(environmentCubemap);
		PrefilterSpecularCubemap(environmentCubemap);
		BakeBRDFLut();
		//Debug.Log("Diffuse Irradiance Map was Generated!");
	}
	
	// out要求传递前必须初始化
	void PrefilterDiffuseCubemap(Cubemap envCubemap) 
	{
		int size = 128;
		outputCubemap = new Cubemap(size, TextureFormat.RGBAFloat, false);
		ComputeBuffer reslutBuffer = new ComputeBuffer(size * size, sizeof(float) * 4);
		Color[] tempColors = new Color[size * size];
		for (int face = 0; face < 6; ++face)
		{
			prefilterDiffuseCS.SetInt("_Face", face);
			prefilterDiffuseCS.SetTexture(0, "_Cubemap", envCubemap);
			prefilterDiffuseCS.SetInt("_Resolution", size);
			prefilterDiffuseCS.SetBuffer(0, "_Reslut", reslutBuffer);
			prefilterDiffuseCS.Dispatch(0, size / 8, size / 8, 1);
			reslutBuffer.GetData(tempColors);
			outputCubemap.SetPixels(tempColors, (CubemapFace)face);
		}
		reslutBuffer.Release();
		outputCubemap.Apply();
		
		AssetDatabase.CreateAsset(outputCubemap, "Assets/ArtAssets/Textures/prefilterDiffuseCubemap.cubemap");
		AssetDatabase.Refresh();
		Debug.Log("prefilterDiffuseCubemap has been generated!");
	}
	
	
	void PrefilterSpecularCubemap(Cubemap cubemap)
	{
		int kernelHandle = prefilterSpecularCS.FindKernel("CSMainGGX");
		int bakeSize = 128;
		outputCubemap = new Cubemap(bakeSize, TextureFormat.RGBAFloat, true);
		int maxMip = outputCubemap.mipmapCount;
		int sampleCubemapSize = cubemap.width;
		outputCubemap.filterMode = FilterMode.Trilinear;
		for (int mip = 0; mip < maxMip; mip++)
		{
			int size = bakeSize;
			size = size >> mip;
			int size2 = size * size;
			Color[] tempColors = new Color[size2];
			float roughness = (float)mip / (float)(maxMip - 1);
			ComputeBuffer reslutBuffer = new ComputeBuffer(size2, sizeof(float) * 4);
			for (int face = 0; face < 6; ++face)
			{
				prefilterSpecularCS.SetInt("_Face", face);
				prefilterSpecularCS.SetTexture(kernelHandle, "_Cubemap", cubemap);
				prefilterSpecularCS.SetFloat("_SampleCubemapSize", sampleCubemapSize);
				prefilterSpecularCS.SetInt("_Resolution", size);
				Debug.Log("roughness" + roughness);
				prefilterSpecularCS.SetFloat("_FilterMipRoughness", roughness);
				prefilterSpecularCS.SetBuffer(1, "_Reslut", reslutBuffer);
				prefilterSpecularCS.Dispatch(1, size, size, 1);
				reslutBuffer.GetData(tempColors);
				outputCubemap.SetPixels(tempColors, (CubemapFace)face, mip);
			}
			reslutBuffer.Release();
		}
		outputCubemap.Apply(false);
		
		AssetDatabase.CreateAsset(outputCubemap, "Assets/ArtAssets/Textures/prefilterSpecularCubemap.cubemap");
		AssetDatabase.Refresh();
		Debug.Log("prefilterSpecularCubemap has been generated!");
	}
	
	// Environment BRDF/ Bake BRDF Lut
	void BakeBRDFLut() 
	{
		int resolution = 512;
		int resolution2 = resolution * resolution;
		Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32,false,true);
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.filterMode = FilterMode.Point;
		Color[] tempColors = new Color[resolution2];
		ComputeBuffer reslutBuffer = new ComputeBuffer(resolution2, sizeof(float) * 4);
		envBrdfLutfCS.SetBuffer(2, "_Reslut", reslutBuffer);
		envBrdfLutfCS.SetInt("_Resolution", resolution);
		envBrdfLutfCS.Dispatch(2, resolution/8, resolution/8, 1);
		reslutBuffer.GetData(tempColors);
		tex.SetPixels(tempColors,  0);
		tex.Apply();
		
		AssetDatabase.CreateAsset(tex, "Assets/ArtAssets/Textures/EnvironmentBRDFLut.png");
		AssetDatabase.Refresh();
		
		Debug.Log("EnvironmentBRDFLut.png has been generated!");
	}
}
