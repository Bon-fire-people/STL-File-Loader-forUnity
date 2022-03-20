using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class StlLoader : MonoBehaviour
{
    public TextAsset stlFile;
    public string[] files;
    [SerializeField] string findPath = "Assets/SampleModel";    // �ǎ�STL�t�@�C���ۑ���
    [SerializeField] GameObject togglePrefab;                   // STL�t�@�C���I��pPrefab
    [SerializeField] GameObject noDataPrefab;                   // STL���Ȃ������ꍇ�ɕ\������Prefab
    [SerializeField] GameObject listWindow;                     // LayoutGroupObject
    [SerializeField] Text debugMessage;                         // debug���b�Z�[�W
    [SerializeField] GameObject resultPrefab;                   // ���b�V�������pprefab
    [SerializeField] string resultPath = "Assets/ResultMesh";   // �A�E�g�v�b�g���b�V���ۑ���
    [SerializeField] string prefabPath = "Assets/Prefabs/Result";      // �A�E�g�v�b�gPrefab�ۑ���

    /// <summary>
    /// �o�C�i���t�H�[�}�b�g��STL�t�@�C����ǂݍ����Mesh�ŕԂ�
    /// </summary>
    /// <param name="path">STL�t�@�C���̃t���p�X</param>
    /// <returns>���b�V���f�[�^</returns>
    private Mesh [] StlToUnityMesh(string path)
    {
        var stream = File.OpenRead(path);
        var reader = new BinaryReader(stream);

        // "�C�ӂ̕�����"��ǂݔ�΂�
        stream.Position = 80;

        // "�O�p�`�̖���"��ǂݍ���
        uint triangleNum = reader.ReadUInt32();

        // ���_����65535�ȉ��������ꍇ => 21845*3 = 65535
        if (triangleNum <= 21845)
        {
            Vector3[] vert = new Vector3[triangleNum * 3];
            int[] tri = new int[triangleNum * 3];

            int triangleIndex = 0;

            for (int i = 0; i < triangleNum; i++)
            {
                // "�@���x�N�g��"��ǂݔ�΂�
                stream.Position += 12;

                // "���_"�f�[�^X, Y, Z��3��
                vert[triangleIndex + 0] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                vert[triangleIndex + 1] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                vert[triangleIndex + 2] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                // ���_�����ԎO�p�`�́A��قǓǂݍ��񂾒��_�ԍ������̂܂܎g��
                tri[triangleIndex] = triangleIndex++;
                tri[triangleIndex] = triangleIndex++;
                tri[triangleIndex] = triangleIndex++;

                // "���g�p�f�[�^"��ǂݔ�΂�
                stream.Position += 2;
            }

            var mesh = new Mesh();
            mesh.vertices = vert;
            mesh.triangles = tri;
            // �@�����Čv�Z
            mesh.RecalculateNormals();
            // Bounds���Čv�Z
            mesh.RecalculateBounds();
            Mesh[] result = { mesh };
            return result;
        }
        // ���_����65535�𒴂��Ă���
        else 
        {
            // ���b�V�������̃I�u�W�F�N�g�ɕ�������K�v�����邩�v�Z
            int resultCnt = (int) Math.Ceiling((double) triangleNum / 21845);
            Mesh[] result = new Mesh[resultCnt];

            // �����I�u�W�F�N�g�̐��������s
            for (int i = 0; i < resultCnt; i++)
            {
                int maxArray;

                if (i < resultCnt - 1)
                {
                    // �Ō�ɐ������郁�b�V���ȊO�͒��_�������x�ɐݒ� 
                    maxArray = 21845;
                }
                else
                {
                    // �Ō�ɐ������郁�b�V���������ꍇ�͒��_�����v�Z
                    maxArray = (int)triangleNum - (21845 * (resultCnt - 1));
                }
                
                Vector3[] vert = new Vector3[maxArray * 3];
                int[] tri = new int[maxArray * 3];

                int triangleIndex = 0;

                for (int j = 0; j < maxArray; j++)
                {
                    // "�@���x�N�g��"��ǂݔ�΂�
                    stream.Position += 12;

                    // "���_"�f�[�^X, Y, Z��3��
                    vert[triangleIndex + 0] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    vert[triangleIndex + 1] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    vert[triangleIndex + 2] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                    // ���_�����ԎO�p�`�́A��قǓǂݍ��񂾒��_�ԍ������̂܂܎g��
                    tri[triangleIndex] = triangleIndex++;
                    tri[triangleIndex] = triangleIndex++;
                    tri[triangleIndex] = triangleIndex++;

                    // "���g�p�f�[�^"��ǂݔ�΂�
                    stream.Position += 2;
                }

                var mesh = new Mesh();
                mesh.vertices = vert;
                mesh.triangles = tri;
                // �@�����Čv�Z
                mesh.RecalculateNormals();
                // Bounds���Čv�Z
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
            // �t�@�C�������������ꍇ�̕\�����C���X�^���X��
            var child = Instantiate(noDataPrefab, Vector3.zero, Quaternion.identity);
            // �q�K�w�Ɉړ�
            child.transform.SetParent(listWindow.transform);
        }
    }

    /// <summary>
    /// STL�t�@�C���������o���đI�������쐬
    /// </summary>
    /// <param name="files"></param>
    void makeList(string[] files)
    {
        for(int i=0; i < files.Length; i++)
        {
            //�t�@�C������".stl"���܂܂��ꍇ�͑I�����쐬
            if(AssetDatabase.GUIDToAssetPath(files[i]).IndexOf(".stl") + 1 > 0)
            {
                // �C���X�^���X��
                var obj = Instantiate(togglePrefab, Vector3.zero, Quaternion.identity);
                // �t�@�C�������擾���đI�����ɓ���
                obj.transform.Find("Label").gameObject.GetComponent<Text>().text =
                    AssetDatabase.GUIDToAssetPath(files[i]);
                // �q�K�w�Ɉړ�
                obj.transform.SetParent(listWindow.transform);
                
                // 13�ȏ�̃f�[�^���쐬���ꂽ�ꍇ�̓��X�g�̍����𑝂₷
                if(listWindow.transform.childCount > 13)
                {
                    var sizedelta = listWindow.GetComponent<RectTransform>().sizeDelta;
                    sizedelta = new Vector2(sizedelta.x, sizedelta.y + 22);
                }
            }
        }
    }

    /// <summary>
    /// �I������STL�t�@�C������Prefab�𐶐�
    /// </summary>
    public void makeSTL()
    {
        // STL�t�@�C����SampleModel�ɑ��݂��Ă��邩
        if(listWindow.transform.childCount > 0)
        {
            // SampleModel�t�H���_�ɂ���STL�t�@�C���̐��������s
            for (int i = 0; i < listWindow.transform.childCount; i++) 
            {
                // ���X�g�̑I�������擾
                var toggle = listWindow.transform.GetChild(i).gameObject;
                // �`�F�b�N�{�b�N�X��ON�������ꍇ�Ɏ��s
                if (toggle.GetComponent<Toggle>().isOn)
                {
                    // �I��������p�X���擾
                    string stlPath = toggle.transform.Find("Label")
                        .gameObject.GetComponent<Text>().text;
                    // �t�@�C�����𒊏o
                    string stlName = stlPath.Substring(stlPath.LastIndexOf("/") + 1);
                    stlName = stlName.Substring(0,stlName.IndexOf(".stl") );

                    //�@STL�t�@�C������mesh�Q�𐶐�
                    Mesh[] meshes = StlToUnityMesh(stlPath);
                    var obj = new GameObject(stlName);

                    // �������ꂽ���b�V���̐��������s
                    for (int j = 0;j < meshes.Length;j++)
                    {
                        // mesh�pObj���C���X�^���X��
                        var resultObj = Instantiate(resultPrefab, Vector3.zero, Quaternion.identity);
                        var meshFilter = resultObj.GetComponent<MeshFilter>();
                        meshFilter.mesh = meshes[j];
                        
                        // mesh�A�Z�b�g��ۑ�
                        string savePath = resultPath + "/" + stlName + "_" + (j + 1) + ".asset";
                        AssetDatabase.CreateAsset(meshFilter.mesh, savePath);
                        AssetDatabase.SaveAssets();
                        
                        // mesh���q�ɔz�u
                        resultObj.transform.SetParent(obj.transform);
                    }
                    // �I�u�W�F�N�g��Prefab��
                    PrefabUtility.SaveAsPrefabAssetAndConnect(obj, prefabPath + "/" + stlName + ".prefab", InteractionMode.AutomatedAction);
                }
            }
        }
        else
        {
            debugMessage.text = "�w��p�X��STL�t�@�C�������݂��܂���";
        }

    }

}
