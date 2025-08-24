using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;

namespace RecycleFactory.Buildings
{
    [RequireComponent(typeof(BuildingRenderer))]
    public abstract class Building : MonoBehaviour
    {
        private static int GUID = -1;
        [ShowNativeProperty] public int id { get; protected set; }

        public new string name;
        public string description;
        public int cost;

        [HideInInspector] public bool isAlive = true;

        [HideInInspector] public BuildingExtension_Receiver receiver;
        [HideInInspector] public BuildingExtension_Releaser releaser;
        public BuildingRenderer buildingRenderer;

        public Vector2Int size = Vector2Int.one;
        public Vector2Int shift = Vector2Int.zero;

        [ShowNativeProperty] public int rotation { get; private set; }

        /// <summary>
        /// Position of the building on the map (grid), always from (0,0) to (mapSize.x - 1, mapSize.y - 1)
        /// </summary>
        public Vector2Int mapPosition;

        public Vector2Int worldPosition2DInt { get { return Map.map2world(mapPosition).ProjectTo2D().FloorToInt() ; } }

        public static event System.Action onAnyBuiltEvent;
        public static event System.Action onAnyDemolishedEvent;

        public event System.Action onDemolishedEvent;

        public static void SimulateOnAnyDemolishedEvent()
        {
            onAnyDemolishedEvent?.Invoke();
        }

        public void Init(Vector2Int mapPos, int selectedRotation)
        {
            id = ++GUID;
            gameObject.name += " " + id;
            Rebase(mapPos);
            transform.position = Map.map2world(mapPos);
            isAlive = true;

            Assert.IsTrue(buildingRenderer != null, "[ReFa]: BuildingRenderer must be not null!");
            receiver = GetComponent<BuildingExtension_Receiver>();
            releaser = GetComponent<BuildingExtension_Releaser>();
            buildingRenderer.Init();

            Rotate(selectedRotation);

            if (receiver) receiver.Init(this, selectedRotation);
            if (releaser) releaser.Init(this, selectedRotation);

            Map.RegisterNewBuilding(this);

            PostInit();

            onAnyBuiltEvent?.Invoke();
        }

        /// <summary>
        /// Translates the building seamlessly by changing the matrix position
        /// </summary>
        public virtual void Rebase(Vector2Int newMatrixPosition)
        {
            mapPosition = newMatrixPosition;
        }

        protected virtual void PostInit() { }

        public virtual void Rotate(int delta)
        {
            rotation = (int)Mathf.Repeat(rotation + delta, 4);  
            size = Utils.RotateXY(size, delta);
            shift = Utils.RotateXY(shift, delta);

            transform.Rotate(Vector3.up * delta * 90);
        }

        public void Demolish()
        {
            isAlive = false;
            Map.RemoveBuilding(this);
            OnDemolish();

            onDemolishedEvent?.Invoke();
            onAnyDemolishedEvent?.Invoke();
            Destroy(gameObject);
        }

        protected virtual void OnDemolish() { }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying == false) return;

            Vector2Int pos = mapPosition + shift;
            for (int _x = 0; _x < Mathf.Abs(size.x); _x++)
            {
                for (int _y = 0; _y < Mathf.Abs(size.y); _y++)
                {
                    int ypos = pos.y + _y * (int)Mathf.Sign(size.y);
                    int xpos = pos.x + _x * (int)Mathf.Sign(size.x);

                    bool isFree = Map.buildingsAt
                        [
                            ypos,
                            xpos
                        ] == null;

                    Gizmos.color = isFree ? Color.white : Color.black;

                    Gizmos.DrawCube(Map.map2world(new Vector2Int(xpos, ypos)), Vector3.one / 2f);
                }
            }
        }
    }
}