using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessWater : MonoBehaviour
{
    public static EndlessWater instance;

    public LODInfo[] detailLevels;

    public static float maxViewDst = 100;
    public Transform viewer;

    public static Vector2 viewerPosition;
    int chunkSize;
    int chunksVisibleInViewDst;

    public GameObject obj;

    public Transform empty;

    Dictionary<Vector2, WaterChunk> waterChunkDictionary = new Dictionary<Vector2, WaterChunk>();
    List<WaterChunk> waterChunksVisibleLastUpdate = new List<WaterChunk>();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        chunkSize = Waves.waterChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        //chunkSize = 100;
        //chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {

        for (int i = 0; i < waterChunksVisibleLastUpdate.Count; i++)
        {
            waterChunksVisibleLastUpdate[i].SetVisible(false);
        }
        waterChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (waterChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    waterChunkDictionary[viewedChunkCoord].UpdateWaterChunk();
                    if (waterChunkDictionary[viewedChunkCoord].IsVisible())
                    {
                        waterChunksVisibleLastUpdate.Add(waterChunkDictionary[viewedChunkCoord]);
                    }
                }
                else
                {
                    waterChunkDictionary.Add(viewedChunkCoord, new WaterChunk(viewedChunkCoord, chunkSize, obj));
                }

            }
        }
    }

    public class WaterChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        public WaterChunk(Vector2 coord, int size, GameObject obj)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = Instantiate(obj);
            meshObject.transform.position = positionV3;
            //meshObject.transform.localScale = Vector3.one * size / 10f;
            //meshObject.transform.localScale = Vector3.one * size / 100f;
            SetVisible(false);
        }

        public void UpdateWaterChunk()
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDstFromNearestEdge <= maxViewDst;

            if (visible)
            {
                int lodIndex = 0;
                /*
                for (int i = 0; i < length; i++)
                {

                }*/
            }

            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        void OnMeshDataRecieved()
        {

        }

        public void RequestMesh()
        {
            hasRequestedMesh = true;

        }
    }
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistThreshold;
    }
}
