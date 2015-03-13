using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace UMA
{
	[System.Serializable]
	public partial class SlotData : ScriptableObject, ISerializationCallbackReceiver
	{
		public string slotName;
		public int listID = -1;

		[System.Obsolete("SlotData.meshRenderer is obsolete.", false)]
		public SkinnedMeshRenderer meshRenderer;
		[System.Obsolete("SlotData.boneNameHashes is obsolete.", false)]
		public int[] boneNameHashes;
		[System.Obsolete("SlotData.boneWeights is obsolete.", false)]
		public BoneWeight[] boneWeights;
		[System.Obsolete("SlotData.umaBoneData is obsolete.", false)]
		public Transform[] umaBoneData;

		public Material materialSample;
		public float overlayScale = 1.0f;
		public Transform[] animatedBones = new Transform[0];
		public string[] textureNameList;
		public DnaConverterBehaviour slotDNA;
		public UMAMeshData meshData;
		public int subMeshIndex;
		/// <summary>
		/// Use this to identify slots that serves the same purpose
		/// Eg. ChestArmor, Helmet, etc.
		/// </summary>
		public string slotGroup;
		/// <summary>
		/// Use this to identify what kind of overlays fit this slotData
		/// Eg. BaseMeshSkin, BaseMeshOverlays, GenericPlateArmor01
		/// </summary>
		public string[] tags;
		public UMADataSlotMaterialRectEvent SlotAtlassed;
		public UMADataEvent DNAApplied;
		public UMADataEvent CharacterCompleted;
		private List<OverlayData> overlayList = new List<OverlayData>();
        
		public SlotData Duplicate()
		{
			SlotData tempSlotData = CreateInstance<SlotData>();
            
			tempSlotData.slotName = slotName;
			tempSlotData.listID = listID;
			tempSlotData.materialSample = materialSample;
			tempSlotData.overlayScale = overlayScale;
			tempSlotData.slotDNA = slotDNA;
			tempSlotData.subMeshIndex = subMeshIndex;
            
			// All this data is passed as reference
			tempSlotData.meshData = meshData;
			tempSlotData.animatedBones = animatedBones;
			tempSlotData.textureNameList = textureNameList;
			//Overlays are duplicated, to lose reference
			for (int i = 0; i < overlayList.Count; i++)
			{
				tempSlotData.AddOverlay(overlayList[i].Duplicate());
			}
            
			tempSlotData.SlotAtlassed = SlotAtlassed;
			tempSlotData.DNAApplied = DNAApplied;
			tempSlotData.CharacterCompleted = CharacterCompleted;

			return tempSlotData;
		}
        
		public SlotData()
		{
            
		}

		public int GetTextureChannelCount(UMAGeneratorBase generator)
		{
			if (textureNameList != null && textureNameList.Length > 0)
			{
				if (string.IsNullOrEmpty(textureNameList[0]))
					return 0;
				return textureNameList.Length;
			}
			if (generator != null)
			{
				return generator.textureNameList.Length;
			}
			return 2; // UMA built in default
		}
        
		public bool RemoveOverlay(params string[] names)
		{
			bool changed = false;
			foreach (var name in names)
			{
				for (int i = 0; i < overlayList.Count; i++)
				{
                    if (overlayList[i].overlayName == name)
					{
						overlayList.RemoveAt(i);
						changed = true;
						break;
					}
				}
			}
			return changed;
		}
        
		public bool SetOverlayColor(Color32 color, params string[] names)
		{
			bool changed = false;
			foreach (var name in names)
			{
				foreach (var overlay in overlayList)
				{
					if (overlay.overlayName == name)
					{
						overlay.color = color;
						changed = true;
					}
				}
			}
			return changed;
		}
        
		public OverlayData GetOverlay(params string[] names)
		{
			foreach (var name in names)
			{
				foreach (var overlay in overlayList)
				{
					if (overlay.overlayName == name)
					{
						return overlay;
					}
				}
			}
			return null;
		}
        
		public void SetOverlay(int index, OverlayData overlay)
		{
			if (index >= overlayList.Count)
			{
				overlayList.Capacity = index + 1;
				while (index >= overlayList.Count)
				{
					overlayList.Add(null);
				}
			}
			overlayList[index] = overlay;
		}
        
		public OverlayData GetOverlay(int index)
		{
			if (index < 0 || index >= overlayList.Count)
				return null;
            return overlayList[index];
		}
        
		public int OverlayCount { get { return overlayList.Count; } }
        
		public void SetOverlayList(List<OverlayData> overlayList)
		{
			this.overlayList = overlayList;
		}
        
		public void AddOverlay(OverlayData overlayData)
		{
			overlayList.Add(overlayData);
		}
        
		public List<OverlayData> GetOverlayList()
		{
			return overlayList;
		}
        
		internal bool Validate(UMAGeneratorBase generator)
		{
			bool valid = true;
			if (meshData != null)
			{
				string[] activeList;
				if (textureNameList == null || textureNameList.Length == 0)
				{
					activeList = generator.textureNameList;
				} else
				{
					activeList = textureNameList;
				}
				int count = activeList.Length;
				while (count > 0 && string.IsNullOrEmpty(activeList[count - 1]))
				{
					count--;
				}
				for (int i = 0; i < overlayList.Count; i++)
				{
                    var overlayData = overlayList[i];
					if (overlayData != null)
					{
						if (overlayData.textureList.Length != count && count != 0)
						{
							Debug.LogError(string.Format("Overlay '{0}' only have {1} textures, but it is added to SlotData '{2}' which requires {3} textures.", overlayData.overlayName, overlayData.textureList.Length, slotName, count));
							valid = false;
						}
					}
				}
			}
			return valid;
		}
        
		public override string ToString()
		{
			return "SlotData: " + slotName;
		}

#if UNITY_EDITOR
		public void UpdateMeshData(SkinnedMeshRenderer meshRenderer)
		{
			meshData = new UMAMeshData();
			meshData.RetrieveDataFromUnityMesh(meshRenderer);
			UnityEditor.EditorUtility.SetDirty(this);
		}
#endif

		#region ISerializationCallbackReceiver Members

		public void OnAfterDeserialize()
		{
#if UNITY_EDITOR
			if (meshData == null && meshRenderer != null)
			{
				meshData = new UMAMeshData();
				meshData.RetrieveDataFromUnityMesh(meshRenderer);
				meshRenderer = null;
				UnityEditor.EditorUtility.SetDirty(this);
			}
#endif
		}

		public void OnBeforeSerialize()
		{
		}

		#endregion
	}
}