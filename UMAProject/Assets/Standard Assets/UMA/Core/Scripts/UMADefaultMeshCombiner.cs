using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace UMA
{
    public class UMADefaultMeshCombiner : UMAMeshCombiner
    {
        protected List<SkinnedMeshCombiner.CombineInstance> combinedMeshList;
        protected List<Material> combinedMaterialList;

        UMAData umaData;
        string[] textureNameList;
        int atlasResolution;

		protected void EnsureUMADataSetup(UMAData umaData)
		{
			if (umaData.firstBake)
			{
				GameObject newGlobal = new GameObject("Global");
				newGlobal.transform.parent = umaData.transform;
				newGlobal.transform.localPosition = Vector3.zero;
				newGlobal.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

				umaData.skeleton = new UMASkeletonDefault(umaData.transform);

				var newRenderer = umaData.gameObject.AddComponent<SkinnedMeshRenderer>();
				newRenderer.rootBone = newGlobal.transform;
				umaData.myRenderer = newRenderer;
				umaData.myRenderer.enabled = false;
				umaData.myRenderer.sharedMesh = new Mesh();

				umaData.umaRoot = umaData.gameObject;
			}
			else
			{
				umaData.cleanMesh(false);
			}
		}

        public override void UpdateUMAMesh(bool updatedAtlas, UMAData umaData, string[] textureNameList, int atlasResolution)
        {
            this.umaData = umaData;
            this.textureNameList = textureNameList;
            this.atlasResolution = atlasResolution;

            combinedMeshList = new List<SkinnedMeshCombiner.CombineInstance>();
            combinedMaterialList = new List<Material>();

            if (updatedAtlas)
            {
                CombineByShader();
            }
            else
            {
                CombineByMaterial();
            }

			EnsureUMADataSetup(umaData);
			umaData.skeleton.BeginSkeletonUpdate();

			//            var boneMap = new Dictionary<Transform, Transform>();
			SkinnedMeshCombiner.CombineMeshes(umaData.myRenderer, combinedMeshList.ToArray(), umaData.skeleton);

            if (updatedAtlas)
            {
                RecalculateUV();
            }

            umaData.umaRecipe.ClearDNAConverters();
            for (int i = 0; i < umaData.umaRecipe.slotDataList.Length; i++)
            {
                SlotData slotData = umaData.umaRecipe.slotDataList[i];
                if (slotData != null)
                {
//                    umaData.EnsureBoneData(slotData.umaBoneData, slotData.animatedBones, boneMap);

                    umaData.umaRecipe.AddDNAUpdater(slotData.slotDNA);
                }
            }

            umaData.myRenderer.quality = SkinQuality.Bone4;
            //umaData.myRenderer.useLightProbes = true;
            var materials = combinedMaterialList.ToArray();
            umaData.myRenderer.sharedMaterials = materials;
            //umaData.myRenderer.sharedMesh.RecalculateBounds();
            umaData.myRenderer.sharedMesh.name = "UMAMesh";

            umaData.firstBake = false;

            //FireSlotAtlasNotification(umaData, materials);
        }

		//private void FireSlotAtlasNotification(UMAData umaData, Material[] materials)
		//{
		//    for (int atlasIndex = 0; atlasIndex < umaData.atlasList.atlas.Count; atlasIndex++)
		//    {
		//        for (int materialDefinitionIndex = 0; materialDefinitionIndex < umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions.Count; materialDefinitionIndex++)
		//        {
		//            var materialDefinition = umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions[materialDefinitionIndex];
		//            var slotData = materialDefinition.source.slotData;
		//            if (slotData.SlotAtlassed != null)
		//            {
		//                slotData.SlotAtlassed.Invoke(umaData, slotData, materials[atlasIndex], materialDefinition.atlasRegion);
		//            }
		//        }
		//    }
		//    SlotData[] slots = umaData.umaRecipe.slotDataList;
		//    for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
		//    {
		//        var slotData = slots[slotIndex];
		//        if (slotData == null) continue;
		//        if (slotData.textureNameList.Length == 1 && string.IsNullOrEmpty(slotData.textureNameList[0]))
		//        {
		//            if (slotData.SlotAtlassed != null)
		//            {
		//                slotData.SlotAtlassed.Invoke(umaData, slotData, materials[atlasIndex], materialDefinition.atlasRegion);
		//            }
		//        }
		//    }
		//}

        protected void CombineByShader()
        {
            SkinnedMeshCombiner.CombineInstance combineInstance;

            for (int atlasIndex = 0; atlasIndex < umaData.atlasList.atlas.Count; atlasIndex++)
            {
                combinedMaterialList.Add(umaData.atlasList.atlas[atlasIndex].materialSample);

                for (int materialDefinitionIndex = 0; materialDefinitionIndex < umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions.Count; materialDefinitionIndex++)
                {
					var materialDefinition = umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions[materialDefinitionIndex];
					var slotData = materialDefinition.source.slotData;
                    combineInstance = new SkinnedMeshCombiner.CombineInstance();
					combineInstance.meshData = slotData.meshData;
					combineInstance.targetSubmeshIndices = new int[combineInstance.meshData.subMeshCount];
					for (int i = 0; i < combineInstance.meshData.subMeshCount; i++)
					{
						combineInstance.targetSubmeshIndices[i] = -1;
					}
					combineInstance.targetSubmeshIndices[slotData.subMeshIndex] = atlasIndex;
                    combinedMeshList.Add(combineInstance);

					if (slotData.SlotAtlassed != null)
					{
						slotData.SlotAtlassed.Invoke(umaData, slotData, umaData.atlasList.atlas[atlasIndex].materialSample, materialDefinition.atlasRegion);
					}
                }
            }


            SlotData[] slots = umaData.umaRecipe.slotDataList;
            int indexCount = 0;
            List<Material> sourceMaterials = null;
            int atlassedMaterials = combinedMaterialList.Count;
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
				if (slots[slotIndex] == null) continue;
                if (slots[slotIndex].textureNameList.Length == 1 && string.IsNullOrEmpty(slots[slotIndex].textureNameList[0]))
                {
                    combineInstance = new SkinnedMeshCombiner.CombineInstance();
                    combineInstance.meshData = slots[slotIndex].meshData;
                    combineInstance.targetSubmeshIndices = new int[combineInstance.meshData.subMeshCount];
                    for (int i = 0; i < combineInstance.meshData.subMeshCount; i++)
                    {
                        combineInstance.targetSubmeshIndices[i] = -1;
                    }

                    bool contains = false;
					Material slotMaterial = null;
                    if (sourceMaterials != null)
                    {
                        for (int i = 0; i < sourceMaterials.Count; i++)
                        {
                            if (slots[slotIndex].materialSample == sourceMaterials[i])
                            {
								slotMaterial = combinedMaterialList[i + atlassedMaterials];
                                combineInstance.targetSubmeshIndices[slots[slotIndex].subMeshIndex] = i+atlassedMaterials;
                                contains = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        sourceMaterials = new List<Material>(slots.Length);
                    }
                    if (!contains)
                    {
                        sourceMaterials.Add(slots[slotIndex].materialSample);
						slotMaterial = new Material(slots[slotIndex].materialSample);
                        combinedMaterialList.Add(slotMaterial);
                        combineInstance.targetSubmeshIndices[slots[slotIndex].subMeshIndex] = combinedMaterialList.Count - 1;
                    }
					if (slots[slotIndex].SlotAtlassed != null)
					{
						slots[slotIndex].SlotAtlassed.Invoke(umaData, slots[slotIndex], slotMaterial, new Rect(0,0,1,1));
					}

                    combinedMeshList.Add(combineInstance);
                    indexCount++;

                }

            }

        }

        protected void CombineByMaterial()
        {
            SlotData[] slots = umaData.umaRecipe.slotDataList;
            bool[] shareMaterial = new bool[slots.Length];

            SkinnedMeshCombiner.CombineInstance combineInstance;

            int indexCount = 0;
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                if (slots[slotIndex] != null)
                {
                    if (!shareMaterial[slotIndex])
                    {
                        combineInstance = new SkinnedMeshCombiner.CombineInstance();
						combineInstance.meshData = slots[slotIndex].meshData;
						combineInstance.targetSubmeshIndices = new int[combineInstance.meshData.subMeshCount];
						for (int i = 0; i < combineInstance.meshData.subMeshCount; i++)
						{
							combineInstance.targetSubmeshIndices[i] = -1;
						}

						combineInstance.targetSubmeshIndices[slots[slotIndex].subMeshIndex] = indexCount;
                        combinedMeshList.Add(combineInstance);

                        Material tempMaterial = Instantiate(slots[slotIndex].materialSample) as Material;
                        tempMaterial.name = slots[slotIndex].slotName;
                        for (int textureType = 0; textureType < textureNameList.Length; textureType++)
                        {
                            if (tempMaterial.HasProperty(textureNameList[textureType]))
                            {
                                slots[slotIndex].GetOverlay(0).textureList[textureType].filterMode = FilterMode.Bilinear;
                                tempMaterial.SetTexture(textureNameList[textureType], slots[slotIndex].GetOverlay(0).textureList[textureType]);
                            }
                        }
                        combinedMaterialList.Add(tempMaterial);


                        shareMaterial[slotIndex] = true;

                        for (int slotIndex2 = slotIndex; slotIndex2 < slots.Length; slotIndex2++)
                        {
                            if (slots[slotIndex2] != null)
                            {
                                if (slotIndex2 != slotIndex && !shareMaterial[slotIndex2])
                                {
                                    if (slots[slotIndex].GetOverlay(0).textureList[0].name == slots[slotIndex2].GetOverlay(0).textureList[0].name)
                                    {
                                        combineInstance = new SkinnedMeshCombiner.CombineInstance();
										combineInstance.meshData = slots[slotIndex2].meshData;
										combineInstance.targetSubmeshIndices = new int[combineInstance.meshData.subMeshCount];
										for (int i = 0; i < combineInstance.meshData.subMeshCount; i++)
										{
											combineInstance.targetSubmeshIndices[i] = -1;
										}

                                        combineInstance.targetSubmeshIndices[slots[slotIndex2].subMeshIndex] = indexCount;
                                        combinedMeshList.Add(combineInstance);

                                        shareMaterial[slotIndex2] = true;
                                    }
                                }
                            }
                        }
                        indexCount++;

                    }
                }
                else
                {
                    shareMaterial[slotIndex] = true;
                }
            }
        }

#if true
        protected void RecalculateUV()
        {
            Vector2[] uvs = umaData.myRenderer.sharedMesh.uv;

            int idx = 0;
            //Handle Atlassed Verts
            for (int atlasIndex = 0; atlasIndex < umaData.atlasList.atlas.Count; atlasIndex++)
            {
                for (int materialDefinitionIndex = 0; materialDefinitionIndex < umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions.Count; materialDefinitionIndex++)
                {
                    var tempAtlasRect = umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions[materialDefinitionIndex].atlasRegion;
                    int vertexCount = umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions[materialDefinitionIndex].source.slotData.meshData.vertices.Length;
					float atlasXMin = tempAtlasRect.xMin / atlasResolution;
					float atlasXMax = tempAtlasRect.xMax / atlasResolution;
					float atlasYMin = tempAtlasRect.yMin / atlasResolution;
					float atlasYMax = tempAtlasRect.yMax / atlasResolution;
					while (vertexCount-- > 0)
                    {
						uvs[idx].x = Mathf.Lerp(atlasXMin, atlasXMax, uvs[idx].x);
						uvs[idx].y = Mathf.Lerp(atlasYMin, atlasYMax, uvs[idx].y);
                        idx++;
                    }

                }
            }

            umaData.myRenderer.sharedMesh.uv = uvs;
        }
#else
		protected void RecalculateUV()
		{
			Vector2[] originalUVs = umaData.myRenderer.sharedMesh.uv;
			Vector2[] atlasUVs = new Vector2[originalUVs.Length];
			
			int idx = 0;
			//Handle Atlassed Verts
			for (int atlasIndex = 0; atlasIndex < umaData.atlasList.atlas.Count; atlasIndex++)
			{
				for (int materialDefinitionIndex = 0; materialDefinitionIndex < umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions.Count; materialDefinitionIndex++)
				{
					var tempAtlasRect = umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions[materialDefinitionIndex].atlasRegion;
					int vertexCount = umaData.atlasList.atlas[atlasIndex].atlasMaterialDefinitions[materialDefinitionIndex].source.slotData.meshRenderer.sharedMesh.vertexCount;
					while (vertexCount-- > 0)
					{
						atlasUVs[idx].x = Mathf.Lerp(tempAtlasRect.xMin / atlasResolution, tempAtlasRect.xMax / atlasResolution, originalUVs[idx].x);
						atlasUVs[idx].y = Mathf.Lerp(tempAtlasRect.yMin / atlasResolution, tempAtlasRect.yMax / atlasResolution, originalUVs[idx].y);
						idx++;
					}
					
				}
			}
			
			//Handle Non Atlassed Verts
			SlotData[] slots = umaData.umaRecipe.slotDataList;
			for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
			{
				if (slots[slotIndex] == null) continue;
				if (slots[slotIndex].textureNameList.Length == 1 && string.IsNullOrEmpty(slots[slotIndex].textureNameList[0]))
				{
					var vertexCount = slots[slotIndex].meshRenderer.sharedMesh.vertexCount;
					while (vertexCount-- > 0)
					{
						atlasUVs[idx] = originalUVs[idx];
						idx++;
					}
				}
			}
			
			umaData.myRenderer.sharedMesh.uv = atlasUVs;
		}	
#endif
	}
}