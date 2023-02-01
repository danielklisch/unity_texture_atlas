using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class MaterialGroup {
    
    public Material[] mats;
    
    public MaterialGroup() {
        mats = new Material[0];
    }
    
    public MaterialGroup(Material[] mats) {
        this.mats = mats;
    }
}

public class TextureAtlas : MonoBehaviour {

    public enum AtlasTypeEnum {DynamicMeshes, BakedMeshes, ReplaceOriginals};
    public enum SizeTypeEnum {Automatic, Fixed};
    
    [HideInInspector]
    public int id = -1;
    public Texture2D albedoAtlas;
    public Texture2D normalAtlas;
    public Texture2D emissionAtlas;
    [HideInInspector]
    public Mesh coords;
    [HideInInspector]
    public Mesh[] atlasedMeshes = new Mesh[0];
    [HideInInspector]
    public Material material;
    public int border = 1;
    public AtlasTypeEnum atlasType;
    public SizeTypeEnum atlasSize;
    [HideInInspector]
    public int atlasWidth = 1024;
    [HideInInspector]
    public int atlasHeight = 1024;
    
    [HideInInspector]
    public List<Mesh> original_meshes = new List<Mesh>();
    [HideInInspector]
    public List<MaterialGroup> original_mats = new List<MaterialGroup>();
    
    public bool NeedsID() {
        return id<0;
    }
    
    public void SetID(int n) {
        id = n;
    }
    
    public Mesh GetCoords() {
        return coords;
    }
    
    public Mesh[] GetAtlasedMeshes() {
        return atlasedMeshes;
    }
    
    public Material GetMaterial() {
        return material;
    }
    
    public void Awake() {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();
        if (atlasType == AtlasTypeEnum.DynamicMeshes) {
            BakeMeshes(false);
            for (int i=0;i<filters.Length;i++) {
                filters[i].sharedMesh = atlasedMeshes[i];
                filters[i].GetComponent<MeshRenderer>().sharedMaterials = new Material[]{material};
            }
        }
        if (atlasType == AtlasTypeEnum.BakedMeshes) {
            for (int i=0;i<filters.Length;i++) {
                filters[i].sharedMesh = atlasedMeshes[i];
                filters[i].GetComponent<MeshRenderer>().sharedMaterials = new Material[]{material};
            }
        }
    }
    
    public string GetName() {
        return gameObject.name+"_"+id;
    }
    
    public void SetResolution(int width, int height) {
        atlasWidth = width;
        atlasHeight = height;
    }
    
    public int GetWidth() {
        return atlasWidth;
    }
    
    public int GetHeight() {
        return atlasHeight;
    }
    
    [HideInInspector]
    public Material[] sortedMaterials;
    
    public Color NewColor(Color c,float a) {
        return new Color(c.r,c.g,c.b,a);
    }
    
    public Color NewNormal(Color c,float a) {
        return new Color(c.a,c.g,0.5f+0.5f*Mathf.Sqrt(1-(2*c.a-1)*(2*c.a-1)-(2*c.g-1)*(2*c.g-1)),a);
    }
    
    public void BakeTextures() {
        if (original_meshes.Count>0) Revert();
        sortedMaterials = GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials).Distinct().OrderBy(m => -m.mainTexture.width).ToArray();
        Vector3[] vertices = new Vector3[sortedMaterials.Length];
        Vector2[] uv = new Vector2[sortedMaterials.Length];
        
        if (atlasSize == SizeTypeEnum.Automatic) {
            int n_pixels = 0;
            foreach (Material mat in sortedMaterials) {
                n_pixels += (mat.mainTexture.width+2*border)*(mat.mainTexture.height+2*border);
            }
            atlasWidth = 2*Mathf.NextPowerOfTwo((int)Mathf.Sqrt(n_pixels));
            atlasHeight = atlasWidth;
            if (atlasWidth*atlasHeight > n_pixels*2) atlasHeight /= 2;
        }
        
        albedoAtlas = new Texture2D(atlasWidth,atlasHeight,TextureFormat.RGBA32,false);
        normalAtlas = new Texture2D(atlasWidth,atlasHeight,TextureFormat.RGBA32,false);
        emissionAtlas = new Texture2D(atlasWidth,atlasHeight,TextureFormat.RGBA32,false);
        int x = 0;
        int y = 0;
        int x_switch = 0;
        int y_switch = 0;
        for (int i=0;i<sortedMaterials.Length;i++) {
            int res = sortedMaterials[i].mainTexture.width;
            Texture2D tex_albedo = sortedMaterials[i].mainTexture as Texture2D;
            Color c_albedo = sortedMaterials[i].color;
            Texture2D tex_normal = sortedMaterials[i].GetTexture("_BumpMap") as Texture2D;
            Texture2D tex_emission = sortedMaterials[i].GetTexture("_EmissionMap") as Texture2D;
            Texture2D tex_metal = sortedMaterials[i].GetTexture("_MetallicGlossMap") as Texture2D;
            Color c_emission = sortedMaterials[i].GetColor("_EmissionColor");
            float gloss_scale = sortedMaterials[i].GetFloat("_GlossMapScale");
            float glossiness = sortedMaterials[i].GetFloat("_Glossiness");
            float metallic = sortedMaterials[i].GetFloat("_Metallic");
            EditorUtility.DisplayProgressBar("Baking Atlas Texture", "packing "+tex_albedo.name+"...", i*1f/sortedMaterials.Length);
            if (x+res+2>atlasWidth) {
                y = y_switch+res+2*border;
                x = x_switch;
                y_switch = y;
                x_switch = 0;
            }
            for (int ty=0;ty<res+2*border;ty++) {
                for (int tx=0;tx<res+2*border;tx++) {
                    albedoAtlas.SetPixel(x+tx,y+ty,tex_albedo.GetPixel(tx-border,ty-border)*c_albedo);
                    float gloss = glossiness;
                    float metal = metallic;
                    if (tex_metal != null) {
                        Color c_metal = tex_metal.GetPixel(tx-border,ty-border);
                        metal = c_metal.r;
                        gloss = c_metal.a*gloss_scale;
                    }
                    if (tex_normal != null) normalAtlas.SetPixel(x+tx,y+ty,NewNormal(tex_normal.GetPixel(tx-border,ty-border),gloss));
                    else normalAtlas.SetPixel(x+tx,y+ty,new Color(0.5f, 0.5f, 1f,gloss));
                    if (tex_emission != null) emissionAtlas.SetPixel(x+tx,y+ty,NewColor(tex_emission.GetPixel(tx-border,ty-border)*c_emission,metal));
                    else emissionAtlas.SetPixel(x+tx,y+ty,new Color(0,0,0,metal));
                }
            }
            vertices[i] = new Vector3(((x+border)*1f/atlasWidth), ((y+border)*1f/atlasHeight), 0);
            uv[i] = new Vector2((res*1f/atlasWidth),(res*1f/atlasHeight));
            x += res+2*border;
            if (i+1<sortedMaterials.Length && sortedMaterials[i+1].mainTexture.width != res) {
                x_switch = x;
                y_switch = y;
            }
        }
        albedoAtlas.Apply();
        normalAtlas.Apply();
        emissionAtlas.Apply();
        
        coords = new Mesh();
        coords.vertices = vertices;
        coords.uv = uv;
        
        EditorUtility.ClearProgressBar();
    }
    
    [HideInInspector]
    public MeshFilter[] filters;
    private Dictionary<Mesh,Mesh> meshes;
    
    public void BakeMeshes(bool show_loading) {
        if (original_meshes.Count>0) Revert();
        meshes = new Dictionary<Mesh,Mesh>();
        //Material[] sortedMaterials = GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials).Distinct().OrderBy(m => -m.mainTexture.width).ToArray();
        Vector3[] vertices = coords.vertices;
        Vector2[] uvs = coords.uv;
        filters = GetComponentsInChildren<MeshFilter>();
        atlasedMeshes = new Mesh[filters.Length];
        material = new Material(Shader.Find("Custom/Atlased"));
        material.shaderKeywords = new string[1]{"_NORMALMAP"};
        material.SetTexture("_Tex1",albedoAtlas);
        material.SetTexture("_Tex2",emissionAtlas);
        material.SetTexture("_Tex3",normalAtlas);
        //material.SetFloat("_Glossiness", sortedMaterials[0].GetFloat("_Glossiness"));
        //material.SetFloat("_Metallic", sortedMaterials[0].GetFloat("_Metallic"));
        for (int i=0;i<filters.Length;i++) {
            Material[] sharedMaterials = filters[i].GetComponent<MeshRenderer>().sharedMaterials;
            if (atlasType == AtlasTypeEnum.ReplaceOriginals) {
                original_meshes.Add(filters[i].sharedMesh);
                original_mats.Add(new MaterialGroup(sharedMaterials));
            }
            if (!meshes.ContainsKey(filters[i].sharedMesh)) {
                Vector4[] vCoords = new Vector4[sharedMaterials.Length];
                for (int j=0;j<sharedMaterials.Length;j++) {
                    int index = System.Array.IndexOf(sortedMaterials,sharedMaterials[j]);
                    vCoords[j] = new Vector4(vertices[index].x,vertices[index].y,uvs[index].x,uvs[index].y);
                }
                meshes.Add(filters[i].sharedMesh,MakeMesh(filters[i].sharedMesh,vCoords,show_loading, new Vector2(i,filters.Length)));
            }
            atlasedMeshes[i] = meshes[filters[i].sharedMesh];
            if (atlasType == AtlasTypeEnum.ReplaceOriginals) {
                filters[i].sharedMesh = atlasedMeshes[i];
                filters[i].GetComponent<MeshRenderer>().sharedMaterials = new Material[]{material};
            }
        }
        if (show_loading) EditorUtility.ClearProgressBar();        
    }
    
    public Mesh MakeMesh(Mesh mesh, Vector4[] coords, bool show_loading, Vector2 progress) {
        List<int[]> submesh_indices = new List<int[]>();
        for (int i=0;i<mesh.subMeshCount;i++) {
            submesh_indices.Add(mesh.GetTriangles(i));
        }
        Mesh new_mesh = new Mesh();
        new_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        new_mesh.vertices = mesh.vertices;
        new_mesh.triangles = mesh.triangles;
        new_mesh.normals = mesh.normals;
        new_mesh.tangents = mesh.tangents;
        new_mesh.uv = mesh.uv;
        Vector2[] uv2 = new Vector2[mesh.vertices.Length];
        Vector2[] uv3 = new Vector2[mesh.vertices.Length];
        for (int i=0;i<uv2.Length;i++) {
            int index = GetMaterialIndex(mesh,i,submesh_indices);
            if (index<0||index>=coords.Length){
                //Debug.Log(new Vector2(i,index));
            }
            else {
                Vector4 coord = coords[index];
                uv2[i] = new Vector2(coord.x,coord.y);
                uv3[i] = new Vector2(coord.z,coord.w);
            }
            if (i%100==0 && show_loading) EditorUtility.DisplayProgressBar("Baking Meshes", "Converting "+mesh.name+" ("+(progress.x+1)+"/"+progress.y+")", (progress.x+i*1f/uv2.Length)/progress.y);
        }
        new_mesh.uv2 = uv2;
        new_mesh.uv3 = uv3;
        return new_mesh;
    }
    
    public int GetMaterialIndex(Mesh mesh, int vertex_index, List<int[]> submesh_indices) {
        for (int i=0;i<mesh.subMeshCount;i++) {
            int index = System.Array.IndexOf(submesh_indices[i],vertex_index);
            if (index >= 0) return i;
        }
        return -1;
    }
    
    public bool CanRevert() {
        return original_meshes.Count>0;
    }
    
    public void Revert() {
        //MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();
        for (int i=0;i<filters.Length;i++) {
            filters[i].sharedMesh = original_meshes[i];
            filters[i].GetComponent<MeshRenderer>().sharedMaterials = original_mats[i].mats;
        }
        original_meshes = new List<Mesh>();
    }
    
}
