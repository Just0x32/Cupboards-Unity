using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameboardController : MonoBehaviour
{
    [SerializeField] private GameObject gameboardPlacePrefab;
    [SerializeField] private GameObject schemePlacePrefab;
    [SerializeField] private Material placeBasicMaterial;
    [SerializeField] private Material placeHighlightedMaterial;
    [SerializeField][Range(0, 100)] private float betweenPlacesMinDistance = 20;
    [SerializeField][Range(0, 50)] private float placeScale = 1;
    [SerializeField][Range(0, 100)] private int placesOrderInLayer = 10;

    [SerializeField] private Material wayMaterial;
    [SerializeField][Range(0, 25)] private float wayWidth = 5;
    [SerializeField][Range(0, 100)] private int waysOrderInLayer = 5;

    [SerializeField] private GameObject gameboardChipPrefab;
    [SerializeField] private GameObject schemeChipPrefab;
    [SerializeField] private Material chipHighlightedMaterial;
    [SerializeField][Range(0, 1)] private float betweenChipsColorHueMinRange = 0.1f;
    [SerializeField][Range(0, 1)] private float chipColorBasicSaturation = 0.75f;
    [SerializeField][Range(0, 1)] private float chipColorBasicBrightness = 1;
    [SerializeField][Range(0, 1)] private float chipColorBrightnessDecrement = 0.35f;
    [SerializeField][Range(0, 1)] private float chipColorMinBrightness = 0.25f;
    [SerializeField][Range(1, 100)] private int numberOfColorGenerationAttempts = 50;
    [SerializeField][Range(0, 50)] private float chipScale = 10;
    [SerializeField][Range(0, 100)] private int chipsOrderInLayer = 20;

    [SerializeField][Range(0, 1)] private float schemeScale = 0.2f;
    [SerializeField][Range(-1f, 1f)] private float schemeLeftOffset = -0.27f;
    [SerializeField][Range(0, 1f)] private float schemeBottomOffset = 0.05f;
    
    [SerializeField] private Camera cameraObject;
    [SerializeField][Range(1, 2)] private float cameraScale = 1.3f;
    [SerializeField][Range(1, 16f / 9f)] private float xToYGameboardRatio = 1.5f;

    [SerializeField] private TextMeshProUGUI messageObject;
    [SerializeField] private TextMeshProUGUI schemeTextObject;

    [SerializeField][Range(1f, 20f)] private float movementSpeed = 2f;

    private List<GameboardPlace> GameboardPlaces { get; set; } = new List<GameboardPlace>();
    private List<GameObject> GameboardWays { get; set; } = new List<GameObject>();
    private List<SchemePlace> SchemePlaces { get; set; } = new List<SchemePlace>();
    private List<GameObject> SchemeWays { get; set; } = new List<GameObject>();
    private List<GameboardChip> GameboardChips { get; set; } = new List<GameboardChip>();
    private List<SchemeChip> SchemeChips { get; set; } = new List<SchemeChip>();
    private int SelectedChipIndex { get; set; }

    public bool arePreparingToMoveOrMoving;
    public bool ArePreparingToMoveOrMoving { get => IsMoving || IsPreparingToMove; }

    private bool isMoving;
    private bool IsMoving
    {
        get => isMoving;
        set
        {
            isMoving = value;

            if (value == false)
            {
                MovingChipIndex = -1;
                ForMovingChipIndexPath = null;
                CheckTheWin();
            }
        }
    }

    private bool IsPreparingToMove { get; set; }

    private int MovingChipIndex { get; set; }

    private List<int> forMovingChipIndexPath;
    private List<int> ForMovingChipIndexPath
    {
        get => forMovingChipIndexPath;
        set
        {
            if (value != null)
            {
                forMovingChipIndexPath = value;
                forMovingChipIndexPath.Reverse();
                forMovingChipIndexPath.RemoveAt(0);
            }
        }
    }

    private int EndOfPathPlaceIndex { get; set; }

    private bool IsWin { get; set; }

    private readonly string winMessage = "Победа!";

    public void Start()
    {
        ChipOrPlace.ParentTransform = transform;
        ChipOrPlaceController.GameboardController = this;
    }

    private void Update()
    {
        Vector2 chipPosition;
        Vector2 targetPlacePosition;

        if (IsMoving)
        {
            if (ForMovingChipIndexPath is null || EndOfPathPlaceIndex == -1)
                IsMoving = false;
            else if (ForMovingChipIndexPath.Count == 0)
                EndMovingAtEndOfPathPlace();
            else
            {
                chipPosition = GameboardChips[MovingChipIndex].Position;
                targetPlacePosition = GameboardPlaces[ForMovingChipIndexPath[0]].Position;

                if (chipPosition == targetPlacePosition)
                    TryToSetNextTargetPosition();
                else
                    MovingChip();
            }
        }

        void EndMovingAtEndOfPathPlace()
        {
            GameboardChips[MovingChipIndex].OnPlaceIndex = EndOfPathPlaceIndex;
            GameboardPlaces[EndOfPathPlaceIndex].IsUnderChip = true;
            IsMoving = false;
        }

        void TryToSetNextTargetPosition()
        {
            ForMovingChipIndexPath.RemoveAt(0);
            if (ForMovingChipIndexPath.Count > 0)
                targetPlacePosition = GameboardPlaces[ForMovingChipIndexPath[0]].Position;
        }

        void MovingChip()
        {
            GameboardChips[MovingChipIndex].Position = Vector2.MoveTowards(chipPosition, targetPlacePosition, movementSpeed * Time.deltaTime * cameraObject.orthographicSize);
        }
    }

    public void Initialize(Vector2[] placeCoordinates, int[][] waysByIncrementedIndexes,
        int[] atStartChipOnPlaceIncrementedIndexes, int[] atWinChipOnPlaceIncrementedIndexes)
    {
        ResetLevel();
        CreatePlacesAndWays(placeCoordinates, waysByIncrementedIndexes);
        CreateChips(atStartChipOnPlaceIncrementedIndexes, atWinChipOnPlaceIncrementedIndexes);
        schemeTextObject.enabled = true;

        void ResetLevel()
        {
            foreach (var place in GameboardPlaces)
                place.DestroyObject();
            GameboardPlaces.Clear();

            foreach (var way in GameboardWays)
                Destroy(way);
            GameboardWays.Clear();

            foreach (var chip in GameboardChips)
                chip.DestroyObject();
            GameboardChips.Clear();

            foreach (var place in SchemePlaces)
                place.DestroyObject();
            SchemePlaces.Clear();

            foreach (var way in SchemeWays)
                Destroy(way);
            SchemeWays.Clear();

            foreach (var chip in SchemeChips)
                chip.DestroyObject();
            SchemeChips.Clear();

            IsWin = false;
            IsPreparingToMove = false;
            IsMoving = false;
            MovingChipIndex = -1;
            EndOfPathPlaceIndex = -1;
            SelectedChipIndex = -1;
        }

        void CreatePlacesAndWays(Vector2[] placeCoordinates, int[][] waysByIncrementedIndexes)
        {
            float minX;
            float minY;
            float maxX;
            float maxY;
            ResetExtremeCoordinates();
            CheckWayJaggedArrayLengths(waysByIncrementedIndexes, 2);

            Vector2 inputDataOffset = GetInputCoordinatesCenterOffset(placeCoordinates);
            Vector2 gameboardScale = GetGameboardScale(placeCoordinates);
            ResetExtremeCoordinates();
            CreateGameboardPlaces();
            CreateGameboardWays(waysByIncrementedIndexes);
            SetCameraSize();
            CreateSchemePlaces();
            CreateSchemeWays(waysByIncrementedIndexes);

            void ResetExtremeCoordinates()
            {
                minX = float.MaxValue;
                minY = float.MaxValue;
                maxX = float.MinValue;
                maxY = float.MinValue;
            }

            void CheckWayJaggedArrayLengths(int[][] array, int rowLength)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] == null || array[i].Length != rowLength)
                        throw new ArgumentException($"Jagged array internal array with not {rowLength}-items length");
                }
            }

            Vector2 GetInputCoordinatesCenterOffset(Vector2[] coordinates)
            {
                foreach (Vector2 coordinate in coordinates)
                    SetExtremeCoordinates(coordinate);

                float xOffset = (minX + maxX) / 2f;
                float yOffset = (minY + maxY) / 2f;
                return new Vector2(xOffset, yOffset);
            }

            Vector2 GetGameboardScale(Vector2[] coordinates)
            {
                float minXPlaceDistance = float.MaxValue;
                float minYPlaceDistance = float.MaxValue;
                FindMinDistances();

                float xScale = betweenPlacesMinDistance / minXPlaceDistance;
                float yScale = betweenPlacesMinDistance / minYPlaceDistance;
                return new Vector2(xScale, yScale);

                void FindMinDistances()
                {
                    for (int i = 0; i < coordinates.Length - 1; i++)
                    {
                        for (int j = i + 1; j < coordinates.Length; j++)
                        {
                            float xDistance = Mathf.Abs(coordinates[i].x - coordinates[j].x);
                            float yDistance = Mathf.Abs(coordinates[i].y - coordinates[j].y);
                            if (xDistance != 0 && xDistance < minXPlaceDistance)
                                minXPlaceDistance = xDistance;
                            if (yDistance != 0 && yDistance < minYPlaceDistance)
                                minYPlaceDistance = yDistance;
                        }
                    }
                }
            }

            void CreateGameboardPlaces()
            {
                for (int i = 0; i < placeCoordinates.Length; i++)
                {
                    Vector2 offsetedAndScaledCoordinates = (placeCoordinates[i] - inputDataOffset) * gameboardScale;
                    GameboardPlaces.Add(new GameboardPlace(gameboardPlacePrefab, offsetedAndScaledCoordinates, placeScale, placesOrderInLayer,
                        placeBasicMaterial, placeHighlightedMaterial, i, GetNearPlaceIndexesForCurrentPlace(i + 1)));
                    SetExtremeCoordinates(offsetedAndScaledCoordinates);
                }

                List<int> GetNearPlaceIndexesForCurrentPlace(int currentPlaceIndex)
                {
                    List<int> nearPlaceIndexes = new List<int>();

                    foreach (int[] way in waysByIncrementedIndexes)
                    {
                        if (way[0] == currentPlaceIndex)
                            nearPlaceIndexes.Add(way[1] - 1);
                        else if (way[1] == currentPlaceIndex)
                            nearPlaceIndexes.Add(way[0] - 1);
                    }

                    return nearPlaceIndexes;
                }
            }

            void SetExtremeCoordinates(Vector2 coordinates)
            {
                if (minX > coordinates.x)
                    minX = coordinates.x;
                if (minY > coordinates.y)
                    minY = coordinates.y;
                if (maxX < coordinates.x)
                    maxX = coordinates.x;
                if (maxY < coordinates.y)
                    maxY = coordinates.y;
            }

            void SetCameraSize()
            {
                Vector2 cameraFullSize = GetGameboardSize() * new Vector2(1f / xToYGameboardRatio, 1f) * cameraScale;

                if (cameraFullSize.x > cameraFullSize.y)
                    cameraObject.orthographicSize = cameraFullSize.x / 2;
                else
                    cameraObject.orthographicSize = cameraFullSize.y / 2;

                Vector2 GetGameboardSize() => new Vector2(maxX - minX + placeScale, maxY - minY + placeScale);
            }

            void CreateGameboardWays(int[][] waysByIndexes)
            {
                foreach (int[] way in waysByIndexes)
                {
                    int firstPlaceIndex = way[0] - 1;
                    int secondPlaceIndex = way[1] - 1;
                    Vector2 firstPoint = GameboardPlaces[firstPlaceIndex].Position;
                    Vector2 secondPoint = GameboardPlaces[secondPlaceIndex].Position;
                    GameboardWays.Add(GetLineObject(firstPoint, secondPoint, wayWidth, wayMaterial));
                }
            }

            GameObject GetLineObject(Vector2 firstPoint, Vector2 secondPoint, float width, Material material)
            {
                GameObject line = new GameObject();
                line.transform.parent = transform;
                LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
                lineRenderer.material = material;
                lineRenderer.positionCount = 2;
                lineRenderer.generateLightingData = true;
                lineRenderer.SetPositions(new Vector3[] { firstPoint, secondPoint });
                lineRenderer.widthMultiplier = width;
                lineRenderer.sortingOrder = waysOrderInLayer;
                return line;
            }

            void CreateSchemePlaces()
            {
                Vector2 schemeCenter = GetSchemeCenter();

                for (int i = 0; i < GameboardPlaces.Count; i++)
                {
                    Vector2 shemeCoordinates = GameboardPlaces[i].Position * schemeScale + schemeCenter;
                    SchemePlaces.Add(new SchemePlace(schemePlacePrefab, shemeCoordinates, schemeScale * placeScale, placesOrderInLayer, placeBasicMaterial));
                }

                Vector2 GetSchemeCenter()
                {
                    float cameraVerticalHalfSize = cameraObject.orthographicSize;
                    float y = cameraVerticalHalfSize * (-1 + schemeScale + schemeBottomOffset);
                    float x = cameraVerticalHalfSize * xToYGameboardRatio * (-1 + schemeScale + schemeLeftOffset / xToYGameboardRatio);
                    return new Vector2(x, y);
                }
            }

            void CreateSchemeWays(int[][] waysByIndexes)
            {
                foreach (int[] way in waysByIndexes)
                {
                    int firstPlaceIndex = way[0] - 1;
                    int secondPlaceIndex = way[1] - 1;
                    Vector2 firstPoint = SchemePlaces[firstPlaceIndex].Position;
                    Vector2 secondPoint = SchemePlaces[secondPlaceIndex].Position;
                    SchemeWays.Add(GetLineObject(firstPoint, secondPoint, wayWidth * schemeScale, wayMaterial));
                }
            }
        }

        void CreateChips(int[] atStartChipOnPlaceIncrementedIndexes, int[] atWinChipOnPlaceIncrementedIndexes)
        {
            CheckNumberOfChips();
            CreateChips();

            void CheckNumberOfChips()
            {
                if (atStartChipOnPlaceIncrementedIndexes.Length != atWinChipOnPlaceIncrementedIndexes.Length)
                    throw new ArgumentException("Numbers of chips at start and at win are not equal.");
            }

            void CreateChips()
            {
                List<Color> usedColors = new List<Color>();
                usedColors.Add(cameraObject.backgroundColor);
                usedColors.Add(placeBasicMaterial.color);
                usedColors.Add(placeHighlightedMaterial.color);
                usedColors.Add(wayMaterial.color);
                usedColors.Add(chipHighlightedMaterial.color);

                for (int i = 0; i < atStartChipOnPlaceIncrementedIndexes.Length; i++)
                {
                    int onGameboardPlaceIndex = GetPlaceNotIncrementedIndex(atStartChipOnPlaceIncrementedIndexes[i]);
                    int onSchemePlaceIndex = GetPlaceNotIncrementedIndex(atWinChipOnPlaceIncrementedIndexes[i]);
                    Material material = new Material(placeBasicMaterial);
                    material.color = GetCurrentItemColor(usedColors, numberOfColorGenerationAttempts,
                        betweenChipsColorHueMinRange, chipColorBasicSaturation, chipColorBasicBrightness, chipColorBrightnessDecrement, chipColorMinBrightness);
                    usedColors.Add(material.color);

                    GameboardChips.Add(new GameboardChip(gameboardChipPrefab, GameboardPlaces[onGameboardPlaceIndex].Position,
                        chipScale, chipsOrderInLayer, material, chipHighlightedMaterial, i, onGameboardPlaceIndex, onSchemePlaceIndex));

                    GameboardPlaces[onGameboardPlaceIndex].IsUnderChip = true;

                    SchemeChips.Add(new SchemeChip(schemeChipPrefab, SchemePlaces[onSchemePlaceIndex].Position,
                        chipScale * schemeScale * 1.4f, chipsOrderInLayer, material));
                }

                Color GetCurrentItemColor(List<Color> usedColors, int numberOfGenerationAttempts,
                    float hueMinRange, float basicSaturation, float basicBrightness, float brightnessDecrement, float minBrightness)
                {
                    float currentBrightness = basicBrightness;
                    Color randomColor = default;
                    bool isUsedColor = true;

                    while (isUsedColor && currentBrightness >= minBrightness)
                    {
                        for (int i = 1; i <= numberOfGenerationAttempts && isUsedColor; i++)
                        {
                            randomColor = UnityEngine.Random.ColorHSV(0, 1, basicSaturation, basicSaturation, currentBrightness, currentBrightness, 1, 1);
                            Color.RGBToHSV(randomColor, out float randomColorHue, out float randomColorSaturation, out float randomColorValue);

                            foreach (var color in usedColors)
                            {
                                Color.RGBToHSV(color, out float usedColorHue, out float usedColorSaturation, out float usedColorBrightness);
                                isUsedColor = Mathf.Abs(usedColorHue - randomColorHue) < hueMinRange && Mathf.Abs(currentBrightness - usedColorBrightness) < brightnessDecrement;

                                if (isUsedColor)
                                    break;
                            }
                        }

                        if (isUsedColor)
                            currentBrightness -= brightnessDecrement;
                    }

                    if (isUsedColor)
                        randomColor = UnityEngine.Random.ColorHSV(0, 1, basicSaturation, basicSaturation, minBrightness, basicBrightness, 1, 1);

                    return randomColor;
                }

                int GetPlaceNotIncrementedIndex(int incrementedIndex) => incrementedIndex - 1;
            }
        }
    }

    public void OnPlaceClick(int index)
    {
        if (!IsWin && !ArePreparingToMoveOrMoving && SelectedChipIndex != -1 && GameboardPlaces[index].IsHighlighted)
        {
            IsPreparingToMove = true;
            UnsetAllPlaces();
            GameboardChips[SelectedChipIndex].UnsetHighlight();
            CreatePathForChipMoving(SelectedChipIndex, index);
            SelectedChipIndex = -1;
            IsPreparingToMove = false;
        }

        void CreatePathForChipMoving(int chipIndex, int targetPlaceIndex)
        {
            int currentPlaceIndex = GameboardChips[chipIndex].OnPlaceIndex;
            GameboardPlaces[currentPlaceIndex].IsUnderChip = false;
            ForMovingChipIndexPath = FindFromCurrentToTargetPlacePath(currentPlaceIndex, targetPlaceIndex);

            if (ForMovingChipIndexPath != null)
            {
                MovingChipIndex = chipIndex;
                EndOfPathPlaceIndex = ForMovingChipIndexPath[ForMovingChipIndexPath.Count - 1];
                IsMoving = true;
            }

            List<int> FindFromCurrentToTargetPlacePath(int currentPlaceIndex, int targetPlaceIndex, List<int> passedPlaceIndexes = null)
            {
                if (passedPlaceIndexes == null)
                    passedPlaceIndexes = new List<int>();
                passedPlaceIndexes.Add(currentPlaceIndex);

                List<int> shortestPathIndexes = null;
                List<int> lastPathIndexes = null;
                List<int> nearPlaceIndexes = GameboardPlaces[currentPlaceIndex].NearPlaceIndexes;

                foreach (var nearPlaceIndex in nearPlaceIndexes)
                {
                    if (!GameboardPlaces[nearPlaceIndex].IsUnderChip && !IsPassedPlaceIndex(nearPlaceIndex, passedPlaceIndexes))
                    {
                        if (nearPlaceIndex == targetPlaceIndex)
                        {
                            shortestPathIndexes = new List<int>();
                            shortestPathIndexes.Add(nearPlaceIndex);
                            shortestPathIndexes.Add(currentPlaceIndex);
                            break;
                        }
                        else
                        {
                            lastPathIndexes = FindFromCurrentToTargetPlacePath(nearPlaceIndex, targetPlaceIndex, passedPlaceIndexes);
                            if (lastPathIndexes != null && (shortestPathIndexes is null || shortestPathIndexes.Count > lastPathIndexes.Count))
                                shortestPathIndexes = lastPathIndexes;
                        }
                    }
                }

                if (shortestPathIndexes != null)
                    shortestPathIndexes.Add(currentPlaceIndex);

                return shortestPathIndexes;

                bool IsPassedPlaceIndex(int placeIndex, List<int> passedPlaceIndexes)
                {
                    bool isPassedPlace = false;

                    foreach (var passedPlaceIndex in passedPlaceIndexes)
                    {
                        isPassedPlace = placeIndex == passedPlaceIndex;
                        if (isPassedPlace)
                            break;
                    }

                    return isPassedPlace;
                }
            }
        }
    }

    public void OnChipClick(int index)
    {
        if (!IsWin & !ArePreparingToMoveOrMoving)
        {
            if (SelectedChipIndex == index)
            {
                GameboardChips[index].UnsetHighlight();
                SelectedChipIndex = -1;
                UnsetAllPlaces();
            }
            else if (SelectedChipIndex != -1)
            {
                GameboardChips[SelectedChipIndex].UnsetHighlight();
                GameboardChips[index].SetHighlight();
                SelectedChipIndex = index;
                UnsetAllPlaces();
                HighlightAvailablePlaces(GameboardChips[index].OnPlaceIndex);
            }
            else if (SelectedChipIndex == -1)
            {
                GameboardChips[index].SetHighlight();
                SelectedChipIndex = index;
                HighlightAvailablePlaces(GameboardChips[index].OnPlaceIndex);
            }
        }

        void HighlightAvailablePlaces(int placeIndex)
        {
            List<int> currentPlaceWays = GameboardPlaces[placeIndex].NearPlaceIndexes;

            foreach (var index in currentPlaceWays)
            {
                if (!GameboardPlaces[index].IsUnderChip && !GameboardPlaces[index].IsHighlighted)
                {
                    GameboardPlaces[index].SetHighlight();
                    HighlightAvailablePlaces(index);
                }
            }
        }
    }

    private void UnsetAllPlaces()
    {
        foreach (var place in GameboardPlaces)
            place.UnsetHighlight();
    }

    private void CheckTheWin()
    {
        bool isWin = false;

        foreach (var chip in GameboardChips)
        {
            isWin = chip.OnWinPlace;

            if (!isWin)
                break;
        }

        if (isWin)
            messageObject.text = winMessage;

        IsWin = isWin;
    }

    private protected abstract class ChipOrPlace
    {
        public static Transform ParentTransform { get; set; }
        protected GameObject Object { get; set; }
        public Vector2 Position { get => (Vector2)Object.transform.position; set => Object.transform.position = value; }

        public float Scale
        {
            get => Object.transform.localScale.x;
            set
            {
                Object.transform.localScale = new Vector3(value, value, 1f);
                Object.transform.GetChild(0).transform.localScale = Vector3.one;
            }
        }

        protected SpriteRenderer shapeSpriteRenderer;
        protected Material basicMaterial;

        public ChipOrPlace(GameObject shapePrefab, Vector2 coordinates, float scale, int orderInLayer, Material basicMaterial)
        {
            Object = Instantiate(shapePrefab, coordinates, Quaternion.identity);
            Object.transform.parent = ParentTransform;
            shapeSpriteRenderer = Object.transform.GetChild(0).GetComponent<SpriteRenderer>();
            shapeSpriteRenderer.sortingOrder = orderInLayer;
            this.basicMaterial = basicMaterial;
            shapeSpriteRenderer.material = basicMaterial;
            Scale = scale;
        }

        public void DestroyObject() => Destroy(Object);
    }

    private protected abstract class ClickableChipOrPlace : ChipOrPlace
    {
        public int Index { get; protected set; }
        public bool IsHighlighted { get; protected set; }

        protected Material highlightedMaterial;

        public ClickableChipOrPlace(GameObject shapePrefab, Vector2 coordinates, float scale, int orderInLayer,
            Material basicMaterial, Material highlightedMaterial, int index)
            : base(shapePrefab, coordinates, scale, orderInLayer, basicMaterial)
        {
            Index = index;
            this.highlightedMaterial = highlightedMaterial;
            IsHighlighted = false;
        }

        public void UnsetHighlight()
        {
            IsHighlighted = false;
            shapeSpriteRenderer.material = basicMaterial;
        }

        public void SetHighlight()
        {
            IsHighlighted = true;
            shapeSpriteRenderer.material = highlightedMaterial;
        }
    }

    private protected abstract class UnClickableChipOrPlace : ChipOrPlace
    {
        public UnClickableChipOrPlace(GameObject shapePrefab, Vector2 coordinates, float scale, int orderInLayer, Material basicMaterial)
            : base(shapePrefab, coordinates, scale, orderInLayer, basicMaterial)
        {
            Destroy(Object.GetComponent<ChipOrPlaceController>());
            Destroy(Object.GetComponent<Collider2D>());
        }
    }

    private protected class GameboardPlace : ClickableChipOrPlace
    {
        public bool IsUnderChip
        {
            get
            {
                if (!Object.GetComponent<Collider2D>().enabled)
                    return true;
                else
                    return false;
            }
            set => Object.GetComponent<Collider2D>().enabled = !value;
        }
        public List<int> NearPlaceIndexes { get; protected set; }

        public GameboardPlace(GameObject shapePrefab, Vector2 coordinates, float scale, int orderInLayer,
            Material basicMaterial, Material highlightedMaterial, int index, List<int> nearPlaceIndexes)
            : base(shapePrefab, coordinates, scale, orderInLayer, basicMaterial, highlightedMaterial, index)
        {
            Object.GetComponent<ChipOrPlaceController>().Initialize(ChipOrPlaceController.ChipOrPlaceType.Place, index);
            NearPlaceIndexes = nearPlaceIndexes;
            IsUnderChip = false;
        }
    }

    private protected class SchemePlace : UnClickableChipOrPlace
    {
        public SchemePlace(GameObject shapePrefab, Vector2 coordinates, float scale, int orderInLayer, Material basicMaterial)
            : base(shapePrefab, coordinates, scale, orderInLayer, basicMaterial)
        {
        }
    }

    private protected class GameboardChip : ClickableChipOrPlace
    {
        public int OnPlaceIndex { get; set; }
        protected int ToWinPlaceIndex { get; set; }
        public bool OnWinPlace { get => OnPlaceIndex == ToWinPlaceIndex; }

        public GameboardChip(GameObject shapePrefab, Vector2 coordinates, float scale, int orderInLayer,
            Material basicMaterial, Material highlightedMaterial, int index, int onPlaceIndex, int toWinPlaceIndex)
            : base(shapePrefab, coordinates, scale, orderInLayer, basicMaterial, highlightedMaterial, index)
        {
            Object.GetComponent<ChipOrPlaceController>().Initialize(ChipOrPlaceController.ChipOrPlaceType.Chip, index);
            OnPlaceIndex = onPlaceIndex;
            ToWinPlaceIndex = toWinPlaceIndex;
        }
    }

    private protected class SchemeChip : UnClickableChipOrPlace
    {
        public SchemeChip(GameObject shapePrefab, Vector2 coordinates, float scale, int orderInLayer, Material basicMaterial)
            : base(shapePrefab, coordinates, scale, orderInLayer, basicMaterial)
        {
        }
    }
}
