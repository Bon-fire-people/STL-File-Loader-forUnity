using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class StlLoader : MonoBehaviour
{
    public TextAsset stlFile;
    public string[] files;
    [SerializeField] string findPath = "Assets/SampleModel";    // 読取STLファイル保存先
    [SerializeField] GameObject togglePrefab;                   // STLファイル選択用Prefab
    [SerializeField] GameObject noDataPrefab;                   // STLがなかった場合に表示するPrefab
    [SerializeField] GameObject listWindow;                     // LayoutGroupObject
    [SerializeField] Text debugMessage;                         // debugメッセージ
    [SerializeField] GameObject resultPrefab;                   // メッシュ生成用prefab
    [SerializeField] string resultPath = "Assets/ResultMesh";   // アウトプットメッシュ保存先
    [SerializeField] string prefabPath = "Assets/Prefabs/Result";      // アウトプットPrefab保存先

    /// <summary>
    /// バイナリフォーマットのSTLファイルを読み込んでMeshで返す
    /// </summary>
    /// <param name="path">STLファイルのフルパス</param>
    /// <returns>メッシュデータ</returns>
    private Mesh [] StlToUnityMesh(string path)
    {
        var stream = File.OpenRead(path);
        var reader = new BinaryReader(stream);

        // "任意の文字列"を読み飛ばし
        stream.Position = 80;

        // "三角形の枚数"を読み込む
        uint triangleNum = reader.ReadUInt32();

        // 頂点数が65535以下だった場合 => 21845*3 = 65535
        if (triangleNum <= 21845)
        {
            Vector3[] vert = new Vector3[triangleNum * 3];
            int[] tri = new int[triangleNum * 3];

            int triangleIndex = 0;

            for (int i = 0; i < triangleNum; i++)
            {
                // "法線ベクトル"を読み飛ばす
                stream.Position += 12;

                // "頂点"データX, Y, Zを3個分
                vert[triangleIndex + 0] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                vert[triangleIndex + 1] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                vert[triangleIndex + 2] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                // 頂点を結ぶ三角形は、先ほど読み込んだ頂点番号をそのまま使う
                tri[triangleIndex] = triangleIndex++;
                tri[triangleIndex] = triangleIndex++;
                tri[triangleIndex] = triangleIndex++;

                // "未使用データ"を読み飛ばし
                stream.Position += 2;
            }

            var mesh = new Mesh();
            mesh.vertices = vert;
            mesh.triangles = tri;
            // 法線を再計算
            mesh.RecalculateNormals();
            // Boundsを再計算
            mesh.RecalculateBounds();
            Mesh[] result = { mesh };
            return result;
        }
        // 頂点数が65535を超えている
        else 
        {
            // メッシュを何個のオブジェクトに分割する必要があるか計算
            int resultCnt = (int) Math.Ceiling((double) triangleNum / 21845);
            Mesh[] result = new Mesh[resultCnt];

            // 分割オブジェクトの数だけ実行
            for (int i = 0; i < resultCnt; i++)
            {
                int maxArray;

                if (i < resultCnt - 1)
                {
                    // 最後に生成するメッシュ以外は頂点数を限度に設定 
                    maxArray = 21845;
                }
                else
                {
                    // 最後に生成するメッシュだった場合は頂点数を計算
                    maxArray = (int)triangleNum - (21845 * (resultCnt - 1));
                }
                
                Vector3[] vert = new Vector3[maxArray * 3];
                int[] tri = new int[maxArray * 3];

                int triangleIndex = 0;

                for (int j = 0; j < maxArray; j++)
                {
                    // "法線ベクトル"を読み飛ばす
                    stream.Position += 12;

                    // "頂点"データX, Y, Zを3個分
                    vert[triangleIndex + 0] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    vert[triangleIndex + 1] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    vert[triangleIndex + 2] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                    // 頂点を結ぶ三角形は、先ほど読み込んだ頂点番号をそのまま使う
                    tri[triangleIndex] = triangleIndex++;
                    tri[triangleIndex] = triangleIndex++;
                    tri[triangleIndex] = triangleIndex++;

                    // "未使用データ"を読み飛ばし
                    stream.Position += 2;
                }

                var mesh = new Mesh();
                mesh.vertices = vert;
                mesh.triangles = tri;
                // 法線を再計算
                mesh.RecalculateNormals();
                // Boundsを再計算
                mesh.RecalculateBounds();
                result[i] = mesh;
            }
            return result;
        }
    }

    void Start() 
    {
        files = AssetDatabase.FindAssets("", new[] { findPath });
        if (files.Length > 0)
        {
            makeList(files);
        }
        else
        {
            // ファイルが無かった場合の表示をインスタンス化
            var child = Instantiate(noDataPrefab, Vector3.zero, Quaternion.identity);
            // 子階層に移動
            child.transform.SetParent(listWindow.transform);
        }
    }

    /// <summary>
    /// STLファイルだけ抽出して選択肢を作成
    /// </summary>
    /// <param name="files"></param>
    void makeList(string[] files)
    {
        for(int i=0; i < files.Length; i++)
        {
            //ファイル名に".stl"が含まれる場合は選択肢作成
            if(AssetDatabase.GUIDToAssetPath(files[i]).IndexOf(".stl") + 1 > 0)
            {
                // インスタンス化
                var obj = Instantiate(togglePrefab, Vector3.zero, Quaternion.identity);
                // ファイル名を取得して選択肢に入力
                obj.transform.Find("Label").gameObject.GetComponent<Text>().text =
                    AssetDatabase.GUIDToAssetPath(files[i]);
                // 子階層に移動
                obj.transform.SetParent(listWindow.transform);
                
                // 13個以上のデータが作成された場合はリストの高さを増やす
                if(listWindow.transform.childCount > 13)
                {
                    var sizedelta = listWindow.GetComponent<RectTransform>().sizeDelta;
                    sizedelta = new Vector2(sizedelta.x, sizedelta.y + 22);
                }
            }
        }
    }

    /// <summary>
    /// 選択したSTLファイルからPrefabを生成
    /// </summary>
    public void makeSTL()
    {
        // STLファイルがSampleModelに存在しているか
        if(listWindow.transform.childCount > 0)
        {
            // SampleModelフォルダにあるSTLファイルの数だけ実行
            for (int i = 0; i < listWindow.transform.childCount; i++) 
            {
                // リストの選択肢を取得
                var toggle = listWindow.transform.GetChild(i).gameObject;
                // チェックボックスがONだった場合に実行
                if (toggle.GetComponent<Toggle>().isOn)
                {
                    // 選択肢からパスを取得
                    string stlPath = toggle.transform.Find("Label")
                        .gameObject.GetComponent<Text>().text;
                    // ファイル名を抽出
                    string stlName = stlPath.Substring(stlPath.LastIndexOf("/") + 1);
                    stlName = stlName.Substring(0,stlName.IndexOf(".stl") );

                    //　STLファイルからmesh群を生成
                    Mesh[] meshes = StlToUnityMesh(stlPath);
                    var obj = new GameObject(stlName);

                    // 生成されたメッシュの数だけ実行
                    for (int j = 0;j < meshes.Length;j++)
                    {
                        // mesh用Objをインスタンス化
                        var resultObj = Instantiate(resultPrefab, Vector3.zero, Quaternion.identity);
                        var meshFilter = resultObj.GetComponent<MeshFilter>();
                        meshFilter.mesh = meshes[j];
                        
                        // meshアセットを保存
                        string savePath = resultPath + "/" + stlName + "_" + (j + 1) + ".asset";
                        AssetDatabase.CreateAsset(meshFilter.mesh, savePath);
                        AssetDatabase.SaveAssets();
                        
                        // meshを子に配置
                        resultObj.transform.SetParent(obj.transform);
                    }
                    // オブジェクトをPrefab化
                    PrefabUtility.SaveAsPrefabAssetAndConnect(obj, prefabPath + "/" + stlName + ".prefab", InteractionMode.AutomatedAction);
                }
            }
        }
        else
        {
            debugMessage.text = "指定パスにSTLファイルが存在しません";
        }

    }

}
