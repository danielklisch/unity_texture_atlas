using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TextureAtlas))]
public class TextureAtlasEditor : Editor {
    
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        TextureAtlas atlas = target as TextureAtlas;
        if (atlas.NeedsID()) atlas.SetID((int)(Random.value*1000000));
            
        if (atlas.atlasSize == TextureAtlas.SizeTypeEnum.Fixed) 
            atlas.SetResolution(EditorGUILayout.IntField("Atlas Width", atlas.GetWidth()),EditorGUILayout.IntField("Atlas Height", atlas.GetHeight()));
        
        if(GUILayout.Button("Bake Textures")) {
            atlas.BakeTextures();
            AssetDatabase.CreateAsset(atlas.albedoAtlas, "Assets/TextureAtlas/AtlasData/"+atlas.GetName()+"_albedo.asset");
            AssetDatabase.CreateAsset(atlas.normalAtlas, "Assets/TextureAtlas/AtlasData/"+atlas.GetName()+"_normals.asset");
            AssetDatabase.CreateAsset(atlas.emissionAtlas, "Assets/TextureAtlas/AtlasData/"+atlas.GetName()+"_emission.asset");
            AssetDatabase.CreateAsset(atlas.GetCoords(), "Assets/TextureAtlas/AtlasData/"+atlas.GetName()+"_coords.asset");
        }
        
        if (atlas.atlasType == TextureAtlas.AtlasTypeEnum.BakedMeshes || atlas.atlasType == TextureAtlas.AtlasTypeEnum.ReplaceOriginals) {
            if(GUILayout.Button("Bake Meshes")) {
                atlas.BakeMeshes(true);
                for (int i=0;i<atlas.GetAtlasedMeshes().Length;i++) {
                    AssetDatabase.CreateAsset(atlas.GetAtlasedMeshes()[i], "Assets/TextureAtlas/AtlasData/"+atlas.GetName()+"_mesh"+i+".asset");
                }
                AssetDatabase.CreateAsset(atlas.GetMaterial(), "Assets/TextureAtlas/AtlasData/"+atlas.GetName()+"_material.asset");
            }
        }
        
        if (atlas.CanRevert()) {
            if(GUILayout.Button("Revert")) {
                atlas.Revert();
            }
        }
        
        if (atlas.albedoAtlas == null) EditorGUILayout.HelpBox("No Baked Atlas Texture!", MessageType.Warning);
        if (atlas.atlasType == TextureAtlas.AtlasTypeEnum.BakedMeshes && (atlas.GetAtlasedMeshes().Length == 0 || atlas.GetAtlasedMeshes()[0] == null)) EditorGUILayout.HelpBox("No Baked Meshes!", MessageType.Warning);
        
    }
    
}
